// Extracts the OFFICIAL Iranian Hijri (qamari) month starts from the Tehran University Calendar Center
// yearly calendars and writes calendar/hijri/ut.json.
//
//   node extract-hijri-anchors.mjs            (reads Calendar-<year>[_n].norm.txt next to this script)
//
// The .norm.txt files are the plain-text version of the official PDFs, one row per day. A year is used ONLY if it
// passes every check below (one row per Jalali day, Hijri day counting 1..29/30 without gaps, month starts that
// follow each other); any other year is skipped and listed, and the app then falls back to UmAlQuraCalendar for it.
// One unreadable row per gap is rebuilt only when its Hijri day is forced by the rows around it (listed as "repairedDays").
//
// Output: "months" = [hijriYear, hijriMonth, "Gregorian date of day 1"] and "ranges" = the Gregorian day ranges
// that are covered by verified data (adjacent years are merged).

import fs from "node:fs"
import path from "node:path"
import { fileURLToPath } from "node:url"

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const outFile = path.join(__dirname, "..", "hijri", "ut.json")

const MONTHS = ["فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"]
const ROW = new RegExp(`\\t(\\d{1,2})\\t(${MONTHS.join("|")}|")\\t(\\d{1,2})\\t`)
const DAY = 86400000

// ---- Jalali -> Gregorian through the runtime's own Persian calendar (ICU), one year at a time ----
const persian = new Intl.DateTimeFormat("en-US-u-ca-persian-nu-latn", { timeZone: "UTC", year: "numeric", month: "numeric", day: "numeric" })
const civil = new Intl.DateTimeFormat("en-US-u-ca-islamic-civil-nu-latn", { timeZone: "UTC", year: "numeric", month: "numeric", day: "numeric" })
const part = (f, d, t) => Number(f.formatToParts(d).find(p => p.type === t).value)

const yearStarts = new Map()
function jalaliYearStart(jy) {
  if (yearStarts.has(jy)) return yearStarts.get(jy)
  let d = new Date(Date.UTC(jy + 621, 2, 23))                       // 1 Farvardin is always 19-22 March; walk back from the 23rd
  for (let i = 0; i < 8; i++, d = new Date(d.getTime() - DAY))
    if (part(persian, d, "month") === 1 && part(persian, d, "day") === 1) { yearStarts.set(jy, d); return d }
  throw new Error("cannot find 1 Farvardin " + jy)
}
const iso = d => d.toISOString().slice(0, 10)
const addDays = (d, n) => new Date(d.getTime() + n * DAY)

function parseYear(file) {
  const rows = []
  for (const line of fs.readFileSync(file, "utf8").split(/\r?\n/)) {
    const m = ROW.exec(line)
    if (!m) continue
    const jd = Number(m[1])
    if (rows.length && rows[rows.length - 1].jd === jd) continue         // a row printed twice
    rows.push({ month: m[2] === '"' ? null : m[2], jd, hd: Number(m[3]) })
  }
  return rows
}

const monthLen = (m, leap) => (m <= 6 ? 31 : m <= 11 ? 30 : leap ? 30 : 29)
// the one value a single missing row can have between two printed Hijri days (0 = not deducible)
const forced = (p, n) => (n === p + 2 && p < 30 ? p + 1 : n === 1 && p === 29 ? 30 : n === 2 && p >= 29 ? 1 : 0)

// returns { days: [{date, hd}], repaired: [..] } or { error }
function verify(jy, rows) {
  const start = jalaliYearStart(jy)
  const expected = Math.round((jalaliYearStart(jy + 1) - start) / DAY)
  const leap = expected === 366
  const seq = []
  const repaired = []
  let curM = 0, curD = 0

  for (let i = 0; i < rows.length; i++) {
    const r = rows[i]
    if (r.month) {
      if (MONTHS.indexOf(r.month) !== curM || r.jd !== 1) return { error: `month header mismatch at row ${i + 1}` }
      curM++; curD = 1
      seq.push({ hd: r.hd })
      continue
    }
    if (curM === 0) return { error: `first row is not 1 Farvardin` }
    if (!(r.jd === curD + 1 && curD < monthLen(curM, leap))) {
      // exactly one unreadable row is repaired, but only when its Hijri day is forced by its neighbours
      let miss = null
      if (r.jd === curD + 2 && curD + 1 <= monthLen(curM, leap)) miss = { m: curM, d: curD + 1 }
      else if (r.jd === 2 && curD === monthLen(curM, leap) && curM < 12) miss = { m: curM + 1, d: 1 }
      const hd = miss ? forced(seq[seq.length - 1].hd, r.hd) : 0
      if (!hd) return { error: `Jalali day ${r.jd} after ${curD} at row ${i + 1}` }
      seq.push({ hd })
      repaired.push(`${jy}-${String(miss.m).padStart(2, "0")}-${String(miss.d).padStart(2, "0")}`)
      curM = miss.m; curD = miss.d
    }
    curD++
    seq.push({ hd: r.hd })
  }
  if (seq.length !== expected) return { error: `${seq.length} days, expected ${expected}` }

  const days = []
  for (let i = 0; i < seq.length; i++) {
    const prevH = i ? seq[i - 1].hd : 0
    const hd = seq[i].hd
    if (prevH && !(hd === prevH + 1 && prevH < 30) && !(hd === 1 && prevH >= 29)) return { error: `Hijri day ${hd} after ${prevH} on day ${i + 1}` }
    days.push({ date: addDays(start, i), hd })
  }
  return { days, repaired }
}

const files = fs.readdirSync(__dirname)
  .map(f => /^Calendar-(\d{4})(?:_\d+)?\.norm\.txt$/.exec(f))
  .filter(Boolean)
  .map(m => ({ jy: Number(m[1]), file: path.join(__dirname, m[0]) }))
  .sort((a, b) => a.jy - b.jy)

const months = new Map()        // "hy-hm" -> start date
const ranges = []
const used = [], skipped = [], repairedAll = []

for (const { jy, file } of files) {
  const v = verify(jy, parseYear(file))
  if (v.error) { skipped.push(`${jy}: ${v.error}`); continue }

  // month starts of this year (the first one may begin before the year does)
  const starts = []
  v.days.forEach((d, i) => { if (d.hd === 1) starts.push(d.date) })
  if (v.days[0].hd !== 1) starts.unshift(addDays(v.days[0].date, -(v.days[0].hd - 1)))

  let ok = true, prev = null
  const local = []
  for (const s of starts) {
    const mid = addDays(s, 14)                                           // mid-month is unambiguous in every Hijri calendar
    const hy = part(civil, mid, "year"), hm = part(civil, mid, "month")
    if (prev && !((hy === prev.hy && hm === prev.hm + 1) || (hy === prev.hy + 1 && prev.hm === 12 && hm === 1))) { ok = false; break }
    local.push({ hy, hm, s }); prev = { hy, hm }
  }
  if (!ok) { skipped.push(`${jy}: Hijri month numbering is not continuous`); continue }
  const clash = local.find(a => months.has(`${a.hy}-${a.hm}`) && iso(months.get(`${a.hy}-${a.hm}`)) !== iso(a.s))
  if (clash) { skipped.push(`${jy}: month ${clash.hy}/${clash.hm} starts on a different day than the neighbouring year`); continue }

  for (const a of local) months.set(`${a.hy}-${a.hm}`, a.s)
  const from = v.days[0].date, to = v.days[v.days.length - 1].date
  const last = ranges[ranges.length - 1]
  if (last && iso(addDays(last.to, 1)) === iso(from)) last.to = to
  else ranges.push({ from, to })
  used.push(jy)
  repairedAll.push(...v.repaired)
}

const monthRows = [...months.entries()]
  .map(([k, s]) => [...k.split("-").map(Number), iso(s)])
  .sort((a, b) => a[2].localeCompare(b[2]))

const lines = [
  "{",
  '  "schemaVersion": 1,',
  '  "source": "Tehran University Calendar Center - official yearly calendars (qamari dates as printed by the center)",',
  '  "generatedBy": "calendar/sources/extract-hijri-anchors.mjs",',
  `  "jalaliYears": [${used.join(", ")}],`,
  `  "repairedDays": [${repairedAll.map(x => `"${x}"`).join(", ")}],`,
  '  "ranges": [',
  ranges.map(r => `    ["${iso(r.from)}", "${iso(r.to)}"]`).join(",\n"),
  "  ],",
  '  "months": [',
  monthRows.map(r => `    [${r[0]}, ${r[1]}, "${r[2]}"]`).join(",\n"),
  "  ]",
  "}",
  ""
]
fs.mkdirSync(path.dirname(outFile), { recursive: true })
fs.writeFileSync(outFile, lines.join("\n"))
console.log(`used ${used.length} year(s): ${used.join(", ")}`)
console.log(`${monthRows.length} Hijri month starts, ${ranges.length} range(s)`)
if (repairedAll.length) console.log("single unreadable rows repaired (Hijri day forced by neighbours): " + repairedAll.join(", "))
if (skipped.length) console.log("skipped (fallback to UmAlQuraCalendar):\n  " + skipped.join("\n  "))
