/**
 * H5 演示联调垫片：旧发行包 baseURL 为空时，强制 API 走 5000，并阻止表单 GET 提交到 /api/*
 */
(function () {
  var API = 'http://localhost:5000';

  document.addEventListener(
    'submit',
    function (e) {
      var action = (e.target && e.target.getAttribute && e.target.getAttribute('action')) || '';
      if (String(action).indexOf('/api/') !== -1) {
        e.preventDefault();
        e.stopPropagation();
      }
    },
    true
  );

  function rewriteUrl(url) {
    if (typeof url !== 'string') return url;
    if (url.indexOf('/api/') === 0) return API + url;
    if (url.indexOf('http://localhost:3800/api/') === 0) {
      return API + url.slice('http://localhost:3800'.length);
    }
    return url;
  }

  var origOpen = XMLHttpRequest.prototype.open;
  XMLHttpRequest.prototype.open = function (method, url) {
    var u = rewriteUrl(url);
    var m = String(method || 'GET').toUpperCase();
    if (u.indexOf('/api/oauth/Login') !== -1 && m === 'GET') {
      m = 'POST';
    }
    return origOpen.apply(this, [m, u].concat([].slice.call(arguments, 2)));
  };

  if (typeof window.fetch === 'function') {
    var origFetch = window.fetch;
    window.fetch = function (input, init) {
      if (typeof input === 'string') {
        input = rewriteUrl(input);
      } else if (input && input.url) {
        input = new Request(rewriteUrl(input.url), input);
      }
      return origFetch.call(this, input, init);
    };
  }
})();
