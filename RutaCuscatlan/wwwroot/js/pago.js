(function () {
  'use strict';
  var $ = function (id) { return document.getElementById(id); };
  var numero = $('Numero'), titular = $('Titular'), venc = $('Vencimiento'), cvc = $('Cvc');
  var pNum = $('ccNumber'), pName = $('ccName'), pExp = $('ccExp'), pBrand = $('ccBrand');
  // Cuenta regresiva del cupo apartado
  var hold = $('hold'), out = $('holdTime');
  if (hold && out) {
    var left = parseInt(hold.getAttribute('data-segundos'), 10) || 0;
    (function tick() {
      var m = Math.floor(left / 60), s = left % 60;
      out.textContent = (m < 10 ? '0' : '') + m + ':' + (s < 10 ? '0' : '') + s;
      if (left <= 0) { hold.classList.add('expired'); out.textContent = 'vencido'; return; }
      if (left <= 60) hold.classList.add('urgent');
      left--; setTimeout(tick, 1000);
    })();
  }


  if (!numero) return;

  function marca(d) {
    if (/^4/.test(d)) return 'Visa';
    if (/^(5[1-5]|2(2[2-9]|[3-6]\d|7[01]|720))/.test(d)) return 'Mastercard';
    if (/^3[47]/.test(d)) return 'American Express';
    return 'Tarjeta';
  }
  function fmtNumero() {
    var d = numero.value.replace(/\D/g, '').slice(0, 19);
    numero.value = d.replace(/(.{4})/g, '$1 ').trim();
    var pad = (d + '################').slice(0, 16).replace(/(.{4})/g, '$1 ').trim().replace(/#/g, '•');
    pNum.textContent = d.length > 16 ? numero.value : pad;
    pBrand.textContent = marca(d);
  }
  function fmtVenc() {
    var d = venc.value.replace(/\D/g, '').slice(0, 4);
    if (d.length >= 3) d = d.slice(0, 2) + '/' + d.slice(2);
    venc.value = d;
    pExp.textContent = d || 'MM/AA';
  }
  function fmtNombre() { pName.textContent = (titular.value || 'NOMBRE APELLIDO').toUpperCase().slice(0, 26); }

  numero.addEventListener('input', fmtNumero);
  venc.addEventListener('input', fmtVenc);
  titular.addEventListener('input', fmtNombre);
  cvc.addEventListener('input', function () { cvc.value = cvc.value.replace(/\D/g, '').slice(0, 4); });

  document.querySelectorAll('.chip[data-numero]').forEach(function (b) {
    b.addEventListener('click', function () {
      numero.value = b.getAttribute('data-numero');
      if (!titular.value) titular.value = 'Cliente Demo';
      if (!venc.value) venc.value = '12/30';
      cvc.value = '123';
      fmtNumero(); fmtVenc(); fmtNombre();
    });
  });

  var form = $('payForm'), btn = $('payBtn');
  if (form && btn) form.addEventListener('submit', function () {
    if (!form.checkValidity()) return;
    btn.classList.add('loading');
    btn.querySelector('.pay-label').textContent = 'Procesando pago…';
    setTimeout(function () { btn.disabled = true; }, 0);
  });

  fmtNumero(); fmtVenc(); fmtNombre();
})();
