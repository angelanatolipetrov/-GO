(function () {
  function toast(t) {
    var n = document.createElement('div'); n.className = 'toast'; n.textContent = t;
    document.body.appendChild(n); setTimeout(function () { n.remove(); }, 2500);
  }
  document.querySelectorAll('[data-countdown]').forEach(function (el) {
    var end = Date.parse(el.dataset.countdown);
    function tick() {
      var s = Math.max(0, Math.floor((end - Date.now()) / 1000));
      var d = Math.floor(s / 86400), h = Math.floor(s % 86400 / 3600), m = Math.floor(s % 3600 / 60);
      el.textContent = d + el.dataset.d + ' ' + h + el.dataset.h + ' ' + m + el.dataset.m;
    }
    tick(); setInterval(tick, 30000);
  });
  document.querySelectorAll('[data-filter]').forEach(function (input) {
    input.addEventListener('input', function () {
      var q = input.value.trim().toLowerCase();
      document.querySelectorAll(input.dataset.filter + ' > li').forEach(function (li) {
        li.hidden = q && li.textContent.toLowerCase().indexOf(q) < 0;
      });
    });
  });
  document.querySelectorAll('[data-share]').forEach(function (b) {
    b.addEventListener('click', function () {
      if (navigator.share) { navigator.share({ title: b.dataset.title, url: location.href }).catch(function () {}); return; }
      if (navigator.clipboard) navigator.clipboard.writeText(location.href).then(function () { toast(b.dataset.copied); });
    });
  });
  document.querySelectorAll('[data-place-select]').forEach(function (sel) {
    var other = document.querySelector(sel.dataset.otherTarget);
    function sync() { other.hidden = sel.value !== '__other'; }
    sel.addEventListener('change', sync); sync();
  });
})();
