(() => {
  const toggle = document.querySelector('.menu-toggle');
  const nav = document.querySelector('#primary-nav');

  if (toggle && nav) {
    toggle.addEventListener('click', () => {
      const isOpen = toggle.getAttribute('aria-expanded') === 'true';
      toggle.setAttribute('aria-expanded', String(!isOpen));
      toggle.setAttribute('aria-label', isOpen ? 'باز کردن منو' : 'بستن منو');
      nav.classList.toggle('is-open', !isOpen);
    });

    nav.querySelectorAll('a').forEach(link => {
      link.addEventListener('click', () => {
        toggle.setAttribute('aria-expanded', 'false');
        toggle.setAttribute('aria-label', 'باز کردن منو');
        nav.classList.remove('is-open');
      });
    });
  }

  // Accessible screenshot lightbox for feature thumbnails.
  let lightbox = null;
  function closeLightbox() {
    if (!lightbox) return;
    lightbox.classList.remove('is-open');
    lightbox.setAttribute('aria-hidden', 'true');
    document.body.style.overflow = '';
  }
  document.addEventListener('click', event => {
    const trigger = event.target.closest('[data-lightbox-src]');
    if (trigger) {
      if (!lightbox) {
        lightbox = document.createElement('div');
        lightbox.className = 'site-lightbox';
        lightbox.setAttribute('role', 'dialog');
        lightbox.setAttribute('aria-modal', 'true');
        lightbox.setAttribute('aria-label', 'Screenshot preview');
        lightbox.setAttribute('aria-hidden', 'true');
        lightbox.innerHTML = '<button class="site-lightbox-close" type="button" aria-label="Close preview">×</button><img alt="" />';
        document.body.appendChild(lightbox);
        lightbox.addEventListener('click', e => { if (e.target === lightbox || e.target.closest('.site-lightbox-close')) closeLightbox(); });
      }
      const image = lightbox.querySelector('img');
      image.src = trigger.dataset.lightboxSrc;
      image.alt = trigger.dataset.lightboxAlt || 'Rooznama screenshot';
      lightbox.classList.add('is-open');
      lightbox.setAttribute('aria-hidden', 'false');
      document.body.style.overflow = 'hidden';
    }
  });
  document.addEventListener('keydown', event => { if (event.key === 'Escape') closeLightbox(); });

  const items = document.querySelectorAll('.reveal');
  const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  if (!('IntersectionObserver' in window) || reduceMotion) {
    items.forEach(item => item.classList.add('is-visible'));
    return;
  }

  const observer = new IntersectionObserver((entries, currentObserver) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        entry.target.classList.add('is-visible');
        currentObserver.unobserve(entry.target);
      }
    });
  }, { threshold: 0.12, rootMargin: '0px 0px -35px 0px' });

  items.forEach(item => observer.observe(item));
})();
