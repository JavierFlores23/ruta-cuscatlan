(function () {
  'use strict';
  var reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  var root = document.documentElement;

  // 1) Aparición escalonada al hacer scroll
  var items = Array.prototype.slice.call(document.querySelectorAll('.reveal'));
  var groups = new Map();
  items.forEach(function (el) {
    var list = groups.get(el.parentNode) || [];
    list.push(el);
    groups.set(el.parentNode, list);
  });
  groups.forEach(function (list) {
    list.forEach(function (el, i) { el.style.setProperty('--d', Math.min(i, 6) * 90 + 'ms'); });
  });
  if (!('IntersectionObserver' in window)) {
    items.forEach(function (el) { el.classList.add('in'); });
  } else {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) {
        if (e.isIntersecting) { e.target.classList.add('in'); io.unobserve(e.target); }
      });
    }, { threshold: 0.12, rootMargin: '0px 0px -6% 0px' });
    items.forEach(function (el) { io.observe(el); });
  }

  // 2) Contador animado de las cifras de la portada
  var counters = document.querySelectorAll('[data-count]');
  function runCount(el) {
    var end = parseInt(el.getAttribute('data-count'), 10) || 0;
    if (reduce || end === 0) { el.textContent = end; return; }
    var t0 = null, dur = 1200;
    el.textContent = '0';
    (function tick(t) {
      if (t0 === null) t0 = t;
      var p = Math.min((t - t0) / dur, 1);
      el.textContent = Math.round(end * (1 - Math.pow(1 - p, 3)));
      if (p < 1) requestAnimationFrame(tick);
    })(performance.now());
  }
  setTimeout(function () { counters.forEach(runCount); }, 450);

  // 3) Scroll: barra de progreso, sombra del menú, desvanecido y parallax de la portada
  var nav = document.querySelector('nav');
  var bar = document.getElementById('progress');
  var copy = document.querySelector('[data-hero-copy]');
  var collage = document.querySelector('[data-parallax]');
  var ticking = false;

  function update() {
    var y = window.pageYOffset || root.scrollTop;
    var max = root.scrollHeight - window.innerHeight;
    if (bar) bar.style.setProperty('--p', max > 0 ? Math.min(y / max, 1).toFixed(4) : 0);
    if (nav) nav.classList.toggle('scrolled', y > 8);
    if (!reduce) {
      if (copy) {
        copy.style.setProperty('--hf', Math.max(1 - y / 520, 0).toFixed(3));
        copy.style.setProperty('--hy', Math.min(y * 0.12, 80).toFixed(1) + 'px');
      }
      if (collage && y < 900) collage.style.setProperty('--py', y.toFixed(1) + 'px');
    }
    ticking = false;
  }
  window.addEventListener('scroll', function () {
    if (!ticking) { ticking = true; requestAnimationFrame(update); }
  }, { passive: true });
  window.addEventListener('resize', update);
  update();
})();
