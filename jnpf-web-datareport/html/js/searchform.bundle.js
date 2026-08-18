/******/ (function(modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if(installedModules[moduleId]) {
/******/ 			return installedModules[moduleId].exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			i: moduleId,
/******/ 			l: false,
/******/ 			exports: {}
/******/ 		};
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);
/******/
/******/ 		// Flag the module as loaded
/******/ 		module.l = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// define getter function for harmony exports
/******/ 	__webpack_require__.d = function(exports, name, getter) {
/******/ 		if(!__webpack_require__.o(exports, name)) {
/******/ 			Object.defineProperty(exports, name, { enumerable: true, get: getter });
/******/ 		}
/******/ 	};
/******/
/******/ 	// define __esModule on exports
/******/ 	__webpack_require__.r = function(exports) {
/******/ 		if(typeof Symbol !== 'undefined' && Symbol.toStringTag) {
/******/ 			Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
/******/ 		}
/******/ 		Object.defineProperty(exports, '__esModule', { value: true });
/******/ 	};
/******/
/******/ 	// create a fake namespace object
/******/ 	// mode & 1: value is a module id, require it
/******/ 	// mode & 2: merge all properties of value into the ns
/******/ 	// mode & 4: return value when already ns object
/******/ 	// mode & 8|1: behave like require
/******/ 	__webpack_require__.t = function(value, mode) {
/******/ 		if(mode & 1) value = __webpack_require__(value);
/******/ 		if(mode & 8) return value;
/******/ 		if((mode & 4) && typeof value === 'object' && value && value.__esModule) return value;
/******/ 		var ns = Object.create(null);
/******/ 		__webpack_require__.r(ns);
/******/ 		Object.defineProperty(ns, 'default', { enumerable: true, value: value });
/******/ 		if(mode & 2 && typeof value != 'string') for(var key in value) __webpack_require__.d(ns, key, function(key) { return value[key]; }.bind(null, key));
/******/ 		return ns;
/******/ 	};
/******/
/******/ 	// getDefaultExport function for compatibility with non-harmony modules
/******/ 	__webpack_require__.n = function(module) {
/******/ 		var getter = module && module.__esModule ?
/******/ 			function getDefault() { return module['default']; } :
/******/ 			function getModuleExports() { return module; };
/******/ 		__webpack_require__.d(getter, 'a', getter);
/******/ 		return getter;
/******/ 	};
/******/
/******/ 	// Object.prototype.hasOwnProperty.call
/******/ 	__webpack_require__.o = function(object, property) { return Object.prototype.hasOwnProperty.call(object, property); };
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";
/******/
/******/
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(__webpack_require__.s = "./src/form/index.js");
/******/ })
/************************************************************************/
/******/ ({

/***/ "./node_modules/bootstrap/dist/js/bootstrap.js":
/*!*****************************************************!*\
  !*** ./node_modules/bootstrap/dist/js/bootstrap.js ***!
  \*****************************************************/
/*! no static exports found */
/***/ (function(module, exports) {

/*!
 * Bootstrap v3.4.1 (https://getbootstrap.com/)
 * Copyright 2011-2019 Twitter, Inc.
 * Licensed under the MIT license
 */

if (typeof jQuery === 'undefined') {
  throw new Error('Bootstrap\'s JavaScript requires jQuery')
}

+function ($) {
  'use strict';
  var version = $.fn.jquery.split(' ')[0].split('.')
  if ((version[0] < 2 && version[1] < 9) || (version[0] == 1 && version[1] == 9 && version[2] < 1) || (version[0] > 3)) {
    throw new Error('Bootstrap\'s JavaScript requires jQuery version 1.9.1 or higher, but lower than version 4')
  }
}(jQuery);

/* ========================================================================
 * Bootstrap: transition.js v3.4.1
 * https://getbootstrap.com/docs/3.4/javascript/#transitions
 * ========================================================================
 * Copyright 2011-2019 Twitter, Inc.
 * Licensed under MIT (https://github.com/twbs/bootstrap/blob/master/LICENSE)
 * ======================================================================== */


+function ($) {
  'use strict';

  // CSS TRANSITION SUPPORT (Shoutout: https://modernizr.com/)
  // ============================================================

  function transitionEnd() {
    var el = document.createElement('bootstrap')

    var transEndEventNames = {
      WebkitTransition : 'webkitTransitionEnd',
      MozTransition    : 'transitionend',
      OTransition      : 'oTransitionEnd otransitionend',
      transition       : 'transitionend'
    }

    for (var name in transEndEventNames) {
      if (el.style[name] !== undefined) {
        return { end: transEndEventNames[name] }
      }
    }

    return false // explicit for ie8 (  ._.)
  }

  // https://blog.alexmaccaw.com/css-transitions
  $.fn.emulateTransitionEnd = function (duration) {
    var called = false
    var $el = this
    $(this).one('bsTransitionEnd', function () { called = true })
    var callback = function () { if (!called) $($el).trigger($.support.transition.end) }
    setTimeout(callback, duration)
    return this
  }

  $(function () {
    $.support.transition = transitionEnd()

    if (!$.support.transition) return

    $.event.special.bsTransitionEnd = {
      bindType: $.support.transition.end,
      delegateType: $.support.transition.end,
      handle: function (e) {
        if ($(e.target).is(this)) return e.handleObj.handler.apply(this, arguments)
      }
    }
  })

}(jQuery);

/* ========================================================================
 * Bootstrap: alert.js v3.4.1
 * https://getbootstrap.com/docs/3.4/javascript/#alerts
 * ========================================================================
 * Copyright 2011-2019 Twitter, Inc.
 * Licensed under MIT (https://github.com/twbs/bootstrap/blob/master/LICENSE)
 * ======================================================================== */


+function ($) {
  'use strict';

  // ALERT CLASS DEFINITION
  // ======================

  var dismiss = '[data-dismiss="alert"]'
  var Alert   = function (el) {
    $(el).on('click', dismiss, this.close)
  }

  Alert.VERSION = '3.4.1'

  Alert.TRANSITION_DURATION = 150

  Alert.prototype.close = function (e) {
    var $this    = $(this)
    var selector = $this.attr('data-target')

    if (!selector) {
      selector = $this.attr('href')
      selector = selector && selector.replace(/.*(?=#[^\s]*$)/, '') // strip for ie7
    }

    selector    = selector === '#' ? [] : selector
    var $parent = $(document).find(selector)

    if (e) e.preventDefault()

    if (!$parent.length) {
      $parent = $this.closest('.alert')
    }

    $parent.trigger(e = $.Event('close.bs.alert'))

    if (e.isDefaultPrevented()) return

    $parent.removeClass('in')

    function removeElement() {
      // detach from parent, fire event then clean up data
      $parent.detach().trigger('closed.bs.alert').remove()
    }

    $.support.transition && $parent.hasClass('fade') ?
      $parent
        .one('bsTransitionEnd', removeElement)
        .emulateTransitionEnd(Alert.TRANSITION_DURATION) :
      removeElement()
  }


  // ALERT PLUGIN DEFINITION
  // =======================

  function Plugin(option) {
    return this.each(function () {
      var $this = $(this)
      var data  = $this.data('bs.alert')

      if (!data) $this.data('bs.alert', (data = new Alert(this)))
      if (typeof option == 'string') data[option].call($this)
    })
  }

  var old = $.fn.alert

  $.fn.alert             = Plugin
  $.fn.alert.Constructor = Alert


  // ALERT NO CONFLICT
  // =================

  $.fn.alert.noConflict = function () {
    $.fn.alert = old
    return this
  }


  // ALERT DATA-API
  // ==============

  $(document).on('click.bs.alert.data-api', dismiss, Alert.prototype.close)

}(jQuery);

/* ========================================================================
 * Bootstrap: button.js v3.4.1
 * https://getbootstrap.com/docs/3.4/javascript/#buttons
 * ========================================================================
 * Copyright 2011-2019 Twitter, Inc.
 * Licensed under MIT (https://github.com/twbs/bootstrap/blob/master/LICENSE)
 * ======================================================================== */


+function ($) {
  'use strict';

  // BUTTON PUBLIC CLASS DEFINITION
  // ==============================

  var Button = function (element, options) {
    this.$element  = $(element)
    this.options   = $.extend({}, Button.DEFAULTS, options)
    this.isLoading = false
  }

  Button.VERSION  = '3.4.1'

  Button.DEFAULTS = {
    loadingText: 'loading...'
  }

  Button.prototype.setState = function (state) {
    var d    = 'disabled'
    var $el  = this.$element
    var val  = $el.is('input') ? 'val' : 'html'
    var data = $el.data()

    state += 'Text'

    if (data.resetText == null) $el.data('resetText', $el[val]())

    // push to event loop to allow forms to submit
    setTimeout($.proxy(function () {
      $el[val](data[state] == null ? this.options[state] : data[state])

      if (state == 'loadingText') {
        this.isLoading = true
        $el.addClass(d).attr(d, d).prop(d, true)
      } else if (this.isLoading) {
        this.isLoading = false
        $el.removeClass(d).removeAttr(d).prop(d, false)
      }
    }, this), 0)
  }

  Button.prototype.toggle = function () {
    var changed = true
    var $parent = this.$element.closest('[data-toggle="buttons"]')

    if ($parent.length) {
      var $input = this.$element.find('input')
      if ($input.prop('type') == 'radio') {
        if ($input.prop('checked')) changed = false
        $parent.find('.active').removeClass('active')
        this.$element.addClass('active')
      } else if ($input.prop('type') == 'checkbox') {
        if (($input.prop('checked')) !== this.$element.hasClass('active')) changed = false
        this.$element.toggleClass('active')
      }
      $input.prop('checked', this.$element.hasClass('active'))
      if (changed) $input.trigger('change')
    } else {
      this.$element.attr('aria-pressed', !this.$element.hasClass('active'))
      this.$element.toggleClass('active')
    }
  }


  // BUTTON PLUGIN DEFINITION
  // ========================

  function Plugin(option) {
    return this.each(function () {
      var $this   = $(this)
      var data    = $this.data('bs.button')
      var options = typeof option == 'object' && option

      if (!data) $this.data('bs.button', (data = new Button(this, options)))

      if (option == 'toggle') data.toggle()
      else if (option) data.setState(option)
    })
  }

  var old = $.fn.button

  $.fn.button             = Plugin
  $.fn.button.Constructor = Button


  // BUTTON NO CONFLICT
  // ==================

  $.fn.button.noConflict = function () {
    $.fn.button = old
    return this
  }


  // BUTTON DATA-API
  // ===============

  $(document)
    .on('click.bs.button.data-api', '[data-toggle^="button"]', function (e) {
      var $btn = $(e.target).closest('.btn')
      Plugin.call($btn, 'toggle')
      if (!($(e.target).is('input[type="radio"], input[type="checkbox"]'))) {
        // Prevent double click on radios, and the double selections (so cancellation) on checkboxes
        e.preventDefault()
        // The target component still receive the focus
        if ($btn.is('input,button')) $btn.trigger('focus')
        else $btn.find('input:visible,button:visible').first().trigger('focus')
      }
    })
    .on('focus.bs.button.data-api blur.bs.button.data-api', '[data-toggle^="button"]', function (e) {
      $(e.target).closest('.btn').toggleClass('focus', /^focus(in)?$/.test(e.type))
    })

}(jQuery);

/* ========================================================================
 * Bootstrap: carousel.js v3.4.1
 * https://getbootstrap.com/docs/3.4/javascript/#carousel
 * ========================================================================
 * Copyright 2011-2019 Twitter, Inc.
 * Licensed under MIT (https://github.com/twbs/bootstrap/blob/master/LICENSE)
 * ======================================================================== */


+function ($) {
  'use strict';

  // CAROUSEL CLASS DEFINITION
  // =========================

  var Carousel = function (element, options) {
    this.$element    = $(element)
    this.$indicators = this.$element.find('.carousel-indicators')
    this.options     = options
    this.paused      = null
    this.sliding     = null
    this.interval    = null
    this.$active     = null
    this.$items      = null

    this.options.keyboard && this.$element.on('keydown.bs.carousel', $.proxy(this.keydown, this))

    this.options.pause == 'hover' && !('ontouchstart' in document.documentElement) && this.$element
      .on('mouseenter.bs.carousel', $.proxy(this.pause, this))
      .on('mouseleave.bs.carousel', $.proxy(this.cycle, this))
  }

  Carousel.VERSION  = '3.4.1'

  Carousel.TRANSITION_DURATION = 600

  Carousel.DEFAULTS = {
    interval: 5000,
    pause: 'hover',
    wrap: true,
    keyboard: true
  }

  Carousel.prototype.keydown = function (e) {
    if (/input|textarea/i.test(e.target.tagName)) return
    switch (e.which) {
      case 37: this.prev(); break
      case 39: this.next(); break
      default: return
    }

    e.preventDefault()
  }

  Carousel.prototype.cycle = function (e) {
    e || (this.paused = false)

    this.interval && clearInterval(this.interval)

    this.options.interval
      && !this.paused
      && (this.interval = setInterval($.proxy(this.next, this), this.options.interval))

    return this
  }

  Carousel.prototype.getItemIndex = function (item) {
    this.$items = item.parent().children('.item')
    return this.$items.index(item || this.$active)
  }

  Carousel.prototype.getItemForDirection = function (direction, active) {
    var activeIndex = this.getItemIndex(active)
    var willWrap = (direction == 'prev' && activeIndex === 0)
                || (direction == 'next' && activeIndex == (this.$items.length - 1))
    if (willWrap && !this.options.wrap) return active
    var delta = direction == 'prev' ? -1 : 1
    var itemIndex = (activeIndex + delta) % this.$items.length
    return this.$items.eq(itemIndex)
  }

  Carousel.prototype.to = function (pos) {
    var that        = this
    var activeIndex = this.getItemIndex(this.$active = this.$element.find('.item.active'))

    if (pos > (this.$items.length - 1) || pos < 0) return

    if (this.sliding)       return this.$element.one('slid.bs.carousel', function () { that.to(pos) }) // yes, "slid"
    if (activeIndex == pos) return this.pause().cycle()

    return this.slide(pos > activeIndex ? 'next' : 'prev', this.$items.eq(pos))
  }

  Carousel.prototype.pause = function (e) {
    e || (this.paused = true)

    if (this.$element.find('.next, .prev').length && $.support.transition) {
      this.$element.trigger($.support.transition.end)
      this.cycle(true)
    }

    this.interval = clearInterval(this.interval)

    return this
  }

  Carousel.prototype.next = function () {
    if (this.sliding) return
    return this.slide('next')
  }

  Carousel.prototype.prev = function () {
    if (this.sliding) return
    return this.slide('prev')
  }

  Carousel.prototype.slide = function (type, next) {
    var $active   = this.$element.find('.item.active')
    var $next     = next || this.getItemForDirection(type, $active)
    var isCycling = this.interval
    var direction = type == 'next' ? 'left' : 'right'
    var that      = this

    if ($next.hasClass('active')) return (this.sliding = false)

    var relatedTarget = $next[0]
    var slideEvent = $.Event('slide.bs.carousel', {
      relatedTarget: relatedTarget,
      direction: direction
    })
    this.$element.trigger(slideEvent)
    if (slideEvent.isDefaultPrevented()) return

    this.sliding = true

    isCycling && this.pause()

    if (this.$indicators.length) {
      this.$indicators.find('.active').removeClass('active')
      var $nextIndicator = $(this.$indicators.children()[this.getItemIndex($next)])
      $nextIndicator && $nextIndicator.addClass('active')
    }

    var slidEvent = $.Event('slid.bs.carousel', { relatedTarget: relatedTarget, direction: direction }) // yes, "slid"
    if ($.support.transition && this.$element.hasClass('slide')) {
      $next.addClass(type)
      if (typeof $next === 'object' && $next.length) {
        $next[0].offsetWidth // force reflow
      }
      $active.addClass(direction)
      $next.addClass(direction)
      $active
        .one('bsTransitionEnd', function () {
          $next.removeClass([type, direction].join(' ')).addClass('active')
          $active.removeClass(['active', direction].join(' '))
          that.sliding = false
          setTimeout(function () {
            that.$element.trigger(slidEvent)
          }, 0)
        })
        .emulateTransitionEnd(Carousel.TRANSITION_DURATION)
    } else {
      $active.removeClass('active')
      $next.addClass('active')
      this.sliding = false
      this.$element.trigger(slidEvent)
    }

    isCycling && this.cycle()

    return this
  }


  // CAROUSEL PLUGIN DEFINITION
  // ==========================

  function Plugin(option) {
    return this.each(function () {
      var $this   = $(this)
      var data    = $this.data('bs.carousel')
      var options = $.extend({}, Carousel.DEFAULTS, $this.data(), typeof option == 'object' && option)
      var action  = typeof option == 'string' ? option : options.slide

      if (!data) $this.data('bs.carousel', (data = new Carousel(this, options)))
      if (typeof option == 'number') data.to(option)
      else if (action) data[action]()
      else if (options.interval) data.pause().cycle()
    })
  }

  var old = $.fn.carousel

  $.fn.carousel             = Plugin
  $.fn.carousel.Constructor = Carousel


  // CAROUSEL NO CONFLICT
  // ====================

  $.fn.carousel.noConflict = function () {
    $.fn.carousel = old
    return this
  }


  // CAROUSEL DATA-API
  // =================

  var clickHandler = function (e) {
    var $this   = $(this)
    var href    = $this.attr('href')
    if (href) {
      href = href.replace(/.*(?=#[^\s]+$)/, '') // strip for ie7
    }

    var target  = $this.attr('data-target') || href
    var $target = $(document).find(target)

    if (!$target.hasClass('carousel')) return

    var options = $.extend({}, $target.data(), $this.data())
    var slideIndex = $this.attr('data-slide-to')
    if (slideIndex) options.interval = false

    Plugin.call($target, options)

    if (slideIndex) {
      $target.data('bs.carousel').to(slideIndex)
    }

    e.preventDefault()
  }

  $(document)
    .on('click.bs.carousel.data-api', '[data-slide]', clickHandler)
    .on('click.bs.carousel.data-api', '[data-slide-to]', clickHandler)

  $(window).on('load', function () {
    $('[data-ride="carousel"]').each(function () {
      var $carousel = $(this)
      Plugin.call($carousel, $carousel.data())
    })
  })

}(jQuery);

/* ========================================================================
 * Bootstrap: collapse.js v3.4.1
 * https://getbootstrap.com/docs/3.4/javascript/#collapse
 * ========================================================================
 * Copyright 2011-2019 Twitter, Inc.
 * Licensed under MIT (https://github.com/twbs/bootstrap/blob/master/LICENSE)
 * ======================================================================== */

/* jshint latedef: false */

+function ($) {
  'use strict';

  // COLLAPSE PUBLIC CLASS DEFINITION
  // ================================

  var Collapse = function (element, options) {
    this.$element      = $(element)
    this.options       = $.extend({}, Collapse.DEFAULTS, options)
    this.$trigger      = $('[data-toggle="collapse"][href="#' + element.id + '"],' +
                           '[data-toggle="collapse"][data-target="#' + element.id + '"]')
    this.transitioning = null

    if (this.options.parent) {
      this.$parent = this.getParent()
    } else {
      this.addAriaAndCollapsedClass(this.$element, this.$trigger)
    }

    if (this.options.toggle) this.toggle()
  }

  Collapse.VERSION  = '3.4.1'

  Collapse.TRANSITION_DURATION = 350

  Collapse.DEFAULTS = {
    toggle: true
  }

  Collapse.prototype.dimension = function () {
    var hasWidth = this.$element.hasClass('width')
    return hasWidth ? 'width' : 'height'
  }

  Collapse.prototype.show = function () {
    if (this.transitioning || this.$element.hasClass('in')) return

    var activesData
    var actives = this.$parent && this.$parent.children('.panel').children('.in, .collapsing')

    if (actives && actives.length) {
      activesData = actives.data('bs.collapse')
      if (activesData && activesData.transitioning) return
    }

    var startEvent = $.Event('show.bs.collapse')
    this.$element.trigger(startEvent)
    if (startEvent.isDefaultPrevented()) return

    if (actives && actives.length) {
      Plugin.call(actives, 'hide')
      activesData || actives.data('bs.collapse', null)
    }

    var dimension = this.dimension()

    this.$element
      .removeClass('collapse')
      .addClass('collapsing')[dimension](0)
      .attr('aria-expanded', true)

    this.$trigger
      .removeClass('collapsed')
      .attr('aria-expanded', true)

    this.transitioning = 1

    var complete = function () {
      this.$element
        .removeClass('collapsing')
        .addClass('collapse in')[dimension]('')
      this.transitioning = 0
      this.$element
        .trigger('shown.bs.collapse')
    }

    if (!$.support.transition) return complete.call(this)

    var scrollSize = $.camelCase(['scroll', dimension].join('-'))

    this.$element
      .one('bsTransitionEnd', $.proxy(complete, this))
      .emulateTransitionEnd(Collapse.TRANSITION_DURATION)[dimension](this.$element[0][scrollSize])
  }

  Collapse.prototype.hide = function () {
    if (this.transitioning || !this.$element.hasClass('in')) return

    var startEvent = $.Event('hide.bs.collapse')
    this.$element.trigger(startEvent)
    if (startEvent.isDefaultPrevented()) return

    var dimension = this.dimension()

    this.$element[dimension](this.$element[dimension]())[0].offsetHeight

    this.$element
      .addClass('collapsing')
      .removeClass('collapse in')
      .attr('aria-expanded', false)

    this.$trigger
      .addClass('collapsed')
      .attr('aria-expanded', false)

    this.transitioning = 1

    var complete = function () {
      this.transitioning = 0
      this.$element
        .removeClass('collapsing')
        .addClass('collapse')
        .trigger('hidden.bs.collapse')
    }

    if (!$.support.transition) return complete.call(this)

    this.$element
      [dimension](0)
      .one('bsTransitionEnd', $.proxy(complete, this))
      .emulateTransitionEnd(Collapse.TRANSITION_DURATION)
  }

  Collapse.prototype.toggle = function () {
    this[this.$element.hasClass('in') ? 'hide' : 'show']()
  }

  Collapse.prototype.getParent = function () {
    return $(document).find(this.options.parent)
      .find('[data-toggle="collapse"][data-parent="' + this.options.parent + '"]')
      .each($.proxy(function (i, element) {
        var $element = $(element)
        this.addAriaAndCollapsedClass(getTargetFromTrigger($element), $element)
      }, this))
      .end()
  }

  Collapse.prototype.addAriaAndCollapsedClass = function ($element, $trigger) {
    var isOpen = $element.hasClass('in')

    $element.attr('aria-expanded', isOpen)
    $trigger
      .toggleClass('collapsed', !isOpen)
      .attr('aria-expanded', isOpen)
  }

  function getTargetFromTrigger($trigger) {
    var href
    var target = $trigger.attr('data-target')
      || (href = $trigger.attr('href')) && href.replace(/.*(?=#[^\s]+$)/, '') // strip for ie7

    return $(document).find(target)
  }


  // COLLAPSE PLUGIN DEFINITION
  // ==========================

  function Plugin(option) {
    return this.each(function () {
      var $this   = $(this)
      var data    = $this.data('bs.collapse')
      var options = $.extend({}, Collapse.DEFAULTS, $this.data(), typeof option == 'object' && option)

      if (!data && options.toggle && /show|hide/.test(option)) options.toggle = false
      if (!data) $this.data('bs.collapse', (data = new Collapse(this, options)))
      if (typeof option == 'string') data[option]()
    })
  }

  var old = $.fn.collapse

  $.fn.collapse             = Plugin
  $.fn.collapse.Constructor = Collapse


  // COLLAPSE NO CONFLICT
  // ====================

  $.fn.collapse.noConflict = function () {
    $.fn.collapse = old
    return this
  }


  // COLLAPSE DATA-API
  // =================

  $(document).on('click.bs.collapse.data-api', '[data-toggle="collapse"]', function (e) {
    var $this   = $(this)

    if (!$this.attr('data-target')) e.preventDefault()

    var $target = getTargetFromTrigger($this)
    var data    = $target.data('bs.collapse')
    var option  = data ? 'toggle' : $this.data()

    Plugin.call($target, option)
  })

}(jQuery);

/* ========================================================================
 * Bootstrap: dropdown.js v3.4.1
 * https://getbootstrap.com/docs/3.4/javascript/#dropdowns
 * ========================================================================
 * Copyright 2011-2019 Twitter, Inc.
 * Licensed under MIT (https://github.com/twbs/bootstrap/blob/master/LICENSE)
 * ======================================================================== */


+function ($) {
  'use strict';

  // DROPDOWN CLASS DEFINITION
  // =========================

  var backdrop = '.dropdown-backdrop'
  var toggle   = '[data-toggle="dropdown"]'
  var Dropdown = function (element) {
    $(element).on('click.bs.dropdown', this.toggle)
  }

  Dropdown.VERSION = '3.4.1'

  function getParent($this) {
    var selector = $this.attr('data-target')

    if (!selector) {
      selector = $this.attr('href')
      selector = selector && /#[A-Za-z]/.test(selector) && selector.replace(/.*(?=#[^\s]*$)/, '') // strip for ie7
    }

    var $parent = selector !== '#' ? $(document).find(selector) : null

    return $parent && $parent.length ? $parent : $this.parent()
  }

  function clearMenus(e) {
    if (e && e.which === 3) return
    $(backdrop).remove()
    $(toggle).each(function () {
      var $this         = $(this)
      var $parent       = getParent($this)
      var relatedTarget = { relatedTarget: this }

      if (!$parent.hasClass('open')) return

      if (e && e.type == 'click' && /input|textarea/i.test(e.target.tagName) && $.contains($parent[0], e.target)) return

      $parent.trigger(e = $.Event('hide.bs.dropdown', relatedTarget))

      if (e.isDefaultPrevented()) return

      $this.attr('aria-expanded', 'false')
      $parent.removeClass('open').trigger($.Event('hidden.bs.dropdown', relatedTarget))
    })
  }

  Dropdown.prototype.toggle = function (e) {
    var $this = $(this)

    if ($this.is('.disabled, :disabled')) return

    var $parent  = getParent($this)
    var isActive = $parent.hasClass('open')

    clearMenus()

    if (!isActive) {
      if ('ontouchstart' in document.documentElement && !$parent.closest('.navbar-nav').length) {
        // if mobile we use a backdrop because click events don't delegate
        $(document.createElement('div'))
          .addClass('dropdown-backdrop')
          .insertAfter($(this))
          .on('click', clearMenus)
      }

      var relatedTarget = { relatedTarget: this }
      $parent.trigger(e = $.Event('show.bs.dropdown', relatedTarget))

      if (e.isDefaultPrevented()) return

      $this
        .trigger('focus')
        .attr('aria-expanded', 'true')

      $parent
        .toggleClass('open')
        .trigger($.Event('shown.bs.dropdown', relatedTarget))
    }

    return false
  }

  Dropdown.prototype.keydown = function (e) {
    if (!/(38|40|27|32)/.test(e.which) || /input|textarea/i.test(e.target.tagName)) return

    var $this = $(this)

    e.preventDefault()
    e.stopPropagation()

    if ($this.is('.disabled, :disabled')) return

    var $parent  = getParent($this)
    var isActive = $parent.hasClass('open')

    if (!isActive && e.which != 27 || isActive && e.which == 27) {
      if (e.which == 27) $parent.find(toggle).trigger('focus')
      return $this.trigger('click')
    }

    var desc = ' li:not(.disabled):visible a'
    var $items = $parent.find('.dropdown-menu' + desc)

    if (!$items.length) return

    var index = $items.index(e.target)

    if (e.which == 38 && index > 0)                 index--         // up
    if (e.which == 40 && index < $items.length - 1) index++         // down
    if (!~index)                                    index = 0

    $items.eq(index).trigger('focus')
  }


  // DROPDOWN PLUGIN DEFINITION
  // ==========================

  function Plugin(option) {
    return this.each(function () {
      var $this = $(this)
      var data  = $this.data('bs.dropdown')

      if (!data) $this.data('bs.dropdown', (data = new Dropdown(this)))
      if (typeof option == 'string') data[option].call($this)
    })
  }

  var old = $.fn.dropdown

  $.fn.dropdown             = Plugin
  $.fn.dropdown.Constructor = Dropdown


  // DROPDOWN NO CONFLICT
  // ====================

  $.fn.dropdown.noConflict = function () {
    $.fn.dropdown = old
    return this
  }


  // APPLY TO STANDARD DROPDOWN ELEMENTS
  // ===================================

  $(document)
    .on('click.bs.dropdown.data-api', clearMenus)
    .on('click.bs.dropdown.data-api', '.dropdown form', function (e) { e.stopPropagation() })
    .on('click.bs.dropdown.data-api', toggle, Dropdown.prototype.toggle)
    .on('keydown.bs.dropdown.data-api', toggle, Dropdown.prototype.keydown)
    .on('keydown.bs.dropdown.data-api', '.dropdown-menu', Dropdown.prototype.keydown)

}(jQuery);

/* ========================================================================
 * Bootstrap: modal.js v3.4.1
 * https://getbootstrap.com/docs/3.4/javascript/#modals
 * ========================================================================
 * Copyright 2011-2019 Twitter, Inc.
 * Licensed under MIT (https://github.com/twbs/bootstrap/blob/master/LICENSE)
 * ======================================================================== */


+function ($) {
  'use strict';

  // MODAL CLASS DEFINITION
  // ======================

  var Modal = function (element, options) {
    this.options = options
    this.$body = $(document.body)
    this.$element = $(element)
    this.$dialog = this.$element.find('.modal-dialog')
    this.$backdrop = null
    this.isShown = null
    this.originalBodyPad = null
    this.scrollbarWidth = 0
    this.ignoreBackdropClick = false
    this.fixedContent = '.navbar-fixed-top, .navbar-fixed-bottom'

    if (this.options.remote) {
      this.$element
        .find('.modal-content')
        .load(this.options.remote, $.proxy(function () {
          this.$element.trigger('loaded.bs.modal')
        }, this))
    }
  }

  Modal.VERSION = '3.4.1'

  Modal.TRANSITION_DURATION = 300
  Modal.BACKDROP_TRANSITION_DURATION = 150

  Modal.DEFAULTS = {
    backdrop: true,
    keyboard: true,
    show: true
  }

  Modal.prototype.toggle = function (_relatedTarget) {
    return this.isShown ? this.hide() : this.show(_relatedTarget)
  }

  Modal.prototype.show = function (_relatedTarget) {
    var that = this
    var e = $.Event('show.bs.modal', { relatedTarget: _relatedTarget })

    this.$element.trigger(e)

    if (this.isShown || e.isDefaultPrevented()) return

    this.isShown = true

    this.checkScrollbar()
    this.setScrollbar()
    this.$body.addClass('modal-open')

    this.escape()
    this.resize()

    this.$element.on('click.dismiss.bs.modal', '[data-dismiss="modal"]', $.proxy(this.hide, this))

    this.$dialog.on('mousedown.dismiss.bs.modal', function () {
      that.$element.one('mouseup.dismiss.bs.modal', function (e) {
        if ($(e.target).is(that.$element)) that.ignoreBackdropClick = true
      })
    })

    this.backdrop(function () {
      var transition = $.support.transition && that.$element.hasClass('fade')

      if (!that.$element.parent().length) {
        that.$element.appendTo(that.$body) // don't move modals dom position
      }

      that.$element
        .show()
        .scrollTop(0)

      that.adjustDialog()

      if (transition) {
        that.$element[0].offsetWidth // force reflow
      }

      that.$element.addClass('in')

      that.enforceFocus()

      var e = $.Event('shown.bs.modal', { relatedTarget: _relatedTarget })

      transition ?
        that.$dialog // wait for modal to slide in
          .one('bsTransitionEnd', function () {
            that.$element.trigger('focus').trigger(e)
          })
          .emulateTransitionEnd(Modal.TRANSITION_DURATION) :
        that.$element.trigger('focus').trigger(e)
    })
  }

  Modal.prototype.hide = function (e) {
    if (e) e.preventDefault()

    e = $.Event('hide.bs.modal')

    this.$element.trigger(e)

    if (!this.isShown || e.isDefaultPrevented()) return

    this.isShown = false

    this.escape()
    this.resize()

    $(document).off('focusin.bs.modal')

    this.$element
      .removeClass('in')
      .off('click.dismiss.bs.modal')
      .off('mouseup.dismiss.bs.modal')

    this.$dialog.off('mousedown.dismiss.bs.modal')

    $.support.transition && this.$element.hasClass('fade') ?
      this.$element
        .one('bsTransitionEnd', $.proxy(this.hideModal, this))
        .emulateTransitionEnd(Modal.TRANSITION_DURATION) :
      this.hideModal()
  }

  Modal.prototype.enforceFocus = function () {
    $(document)
      .off('focusin.bs.modal') // guard against infinite focus loop
      .on('focusin.bs.modal', $.proxy(function (e) {
        if (document !== e.target &&
          this.$element[0] !== e.target &&
          !this.$element.has(e.target).length) {
          this.$element.trigger('focus')
        }
      }, this))
  }

  Modal.prototype.escape = function () {
    if (this.isShown && this.options.keyboard) {
      this.$element.on('keydown.dismiss.bs.modal', $.proxy(function (e) {
        e.which == 27 && this.hide()
      }, this))
    } else if (!this.isShown) {
      this.$element.off('keydown.dismiss.bs.modal')
    }
  }

  Modal.prototype.resize = function () {
    if (this.isShown) {
      $(window).on('resize.bs.modal', $.proxy(this.handleUpdate, this))
    } else {
      $(window).off('resize.bs.modal')
    }
  }

  Modal.prototype.hideModal = function () {
    var that = this
    this.$element.hide()
    this.backdrop(function () {
      that.$body.removeClass('modal-open')
      that.resetAdjustments()
      that.resetScrollbar()
      that.$element.trigger('hidden.bs.modal')
    })
  }

  Modal.prototype.removeBackdrop = function () {
    this.$backdrop && this.$backdrop.remove()
    this.$backdrop = null
  }

  Modal.prototype.backdrop = function (callback) {
    var that = this
    var animate = this.$element.hasClass('fade') ? 'fade' : ''

    if (this.isShown && this.options.backdrop) {
      var doAnimate = $.support.transition && animate

      this.$backdrop = $(document.createElement('div'))
        .addClass('modal-backdrop ' + animate)
        .appendTo(this.$body)

      this.$element.on('click.dismiss.bs.modal', $.proxy(function (e) {
        if (this.ignoreBackdropClick) {
          this.ignoreBackdropClick = false
          return
        }
        if (e.target !== e.currentTarget) return
        this.options.backdrop == 'static'
          ? this.$element[0].focus()
          : this.hide()
      }, this))

      if (doAnimate) this.$backdrop[0].offsetWidth // force reflow

      this.$backdrop.addClass('in')

      if (!callback) return

      doAnimate ?
        this.$backdrop
          .one('bsTransitionEnd', callback)
          .emulateTransitionEnd(Modal.BACKDROP_TRANSITION_DURATION) :
        callback()

    } else if (!this.isShown && this.$backdrop) {
      this.$backdrop.removeClass('in')

      var callbackRemove = function () {
        that.removeBackdrop()
        callback && callback()
      }
      $.support.transition && this.$element.hasClass('fade') ?
        this.$backdrop
          .one('bsTransitionEnd', callbackRemove)
          .emulateTransitionEnd(Modal.BACKDROP_TRANSITION_DURATION) :
        callbackRemove()

    } else if (callback) {
      callback()
    }
  }

  // these following methods are used to handle overflowing modals

  Modal.prototype.handleUpdate = function () {
    this.adjustDialog()
  }

  Modal.prototype.adjustDialog = function () {
    var modalIsOverflowing = this.$element[0].scrollHeight > document.documentElement.clientHeight

    this.$element.css({
      paddingLeft: !this.bodyIsOverflowing && modalIsOverflowing ? this.scrollbarWidth : '',
      paddingRight: this.bodyIsOverflowing && !modalIsOverflowing ? this.scrollbarWidth : ''
    })
  }

  Modal.prototype.resetAdjustments = function () {
    this.$element.css({
      paddingLeft: '',
      paddingRight: ''
    })
  }

  Modal.prototype.checkScrollbar = function () {
    var fullWindowWidth = window.innerWidth
    if (!fullWindowWidth) { // workaround for missing window.innerWidth in IE8
      var documentElementRect = document.documentElement.getBoundingClientRect()
      fullWindowWidth = documentElementRect.right - Math.abs(documentElementRect.left)
    }
    this.bodyIsOverflowing = document.body.clientWidth < fullWindowWidth
    this.scrollbarWidth = this.measureScrollbar()
  }

  Modal.prototype.setScrollbar = function () {
    var bodyPad = parseInt((this.$body.css('padding-right') || 0), 10)
    this.originalBodyPad = document.body.style.paddingRight || ''
    var scrollbarWidth = this.scrollbarWidth
    if (this.bodyIsOverflowing) {
      this.$body.css('padding-right', bodyPad + scrollbarWidth)
      $(this.fixedContent).each(function (index, element) {
        var actualPadding = element.style.paddingRight
        var calculatedPadding = $(element).css('padding-right')
        $(element)
          .data('padding-right', actualPadding)
          .css('padding-right', parseFloat(calculatedPadding) + scrollbarWidth + 'px')
      })
    }
  }

  Modal.prototype.resetScrollbar = function () {
    this.$body.css('padding-right', this.originalBodyPad)
    $(this.fixedContent).each(function (index, element) {
      var padding = $(element).data('padding-right')
      $(element).removeData('padding-right')
      element.style.paddingRight = padding ? padding : ''
    })
  }

  Modal.prototype.measureScrollbar = function () { // thx walsh
    var scrollDiv = document.createElement('div')
    scrollDiv.className = 'modal-scrollbar-measure'
    this.$body.append(scrollDiv)
    var scrollbarWidth = scrollDiv.offsetWidth - scrollDiv.clientWidth
    this.$body[0].removeChild(scrollDiv)
    return scrollbarWidth
  }


  // MODAL PLUGIN DEFINITION
  // =======================

  function Plugin(option, _relatedTarget) {
    return this.each(function () {
      var $this = $(this)
      var data = $this.data('bs.modal')
      var options = $.extend({}, Modal.DEFAULTS, $this.data(), typeof option == 'object' && option)

      if (!data) $this.data('bs.modal', (data = new Modal(this, options)))
      if (typeof option == 'string') data[option](_relatedTarget)
      else if (options.show) data.show(_relatedTarget)
    })
  }

  var old = $.fn.modal

  $.fn.modal = Plugin
  $.fn.modal.Constructor = Modal


  // MODAL NO CONFLICT
  // =================

  $.fn.modal.noConflict = function () {
    $.fn.modal = old
    return this
  }


  // MODAL DATA-API
  // ==============

  $(document).on('click.bs.modal.data-api', '[data-toggle="modal"]', function (e) {
    var $this = $(this)
    var href = $this.attr('href')
    var target = $this.attr('data-target') ||
      (href && href.replace(/.*(?=#[^\s]+$)/, '')) // strip for ie7

    var $target = $(document).find(target)
    var option = $target.data('bs.modal') ? 'toggle' : $.extend({ remote: !/#/.test(href) && href }, $target.data(), $this.data())

    if ($this.is('a')) e.preventDefault()

    $target.one('show.bs.modal', function (showEvent) {
      if (showEvent.isDefaultPrevented()) return // only register focus restorer if modal will actually get shown
      $target.one('hidden.bs.modal', function () {
        $this.is(':visible') && $this.trigger('focus')
      })
    })
    Plugin.call($target, option, this)
  })

}(jQuery);

/* ========================================================================
 * Bootstrap: tooltip.js v3.4.1
 * https://getbootstrap.com/docs/3.4/javascript/#tooltip
 * Inspired by the original jQuery.tipsy by Jason Frame
 * ========================================================================
 * Copyright 2011-2019 Twitter, Inc.
 * Licensed under MIT (https://github.com/twbs/bootstrap/blob/master/LICENSE)
 * ======================================================================== */

+function ($) {
  'use strict';

  var DISALLOWED_ATTRIBUTES = ['sanitize', 'whiteList', 'sanitizeFn']

  var uriAttrs = [
    'background',
    'cite',
    'href',
    'itemtype',
    'longdesc',
    'poster',
    'src',
    'xlink:href'
  ]

  var ARIA_ATTRIBUTE_PATTERN = /^aria-[\w-]*$/i

  var DefaultWhitelist = {
    // Global attributes allowed on any supplied element below.
    '*': ['class', 'dir', 'id', 'lang', 'role', ARIA_ATTRIBUTE_PATTERN],
    a: ['target', 'href', 'title', 'rel'],
    area: [],
    b: [],
    br: [],
    col: [],
    code: [],
    div: [],
    em: [],
    hr: [],
    h1: [],
    h2: [],
    h3: [],
    h4: [],
    h5: [],
    h6: [],
    i: [],
    img: ['src', 'alt', 'title', 'width', 'height'],
    li: [],
    ol: [],
    p: [],
    pre: [],
    s: [],
    small: [],
    span: [],
    sub: [],
    sup: [],
    strong: [],
    u: [],
    ul: []
  }

  /**
   * A pattern that recognizes a commonly useful subset of URLs that are safe.
   *
   * Shoutout to Angular 7 https://github.com/angular/angular/blob/7.2.4/packages/core/src/sanitization/url_sanitizer.ts
   */
  var SAFE_URL_PATTERN = /^(?:(?:https?|mailto|ftp|tel|file):|[^&:/?#]*(?:[/?#]|$))/gi

  /**
   * A pattern that matches safe data URLs. Only matches image, video and audio types.
   *
   * Shoutout to Angular 7 https://github.com/angular/angular/blob/7.2.4/packages/core/src/sanitization/url_sanitizer.ts
   */
  var DATA_URL_PATTERN = /^data:(?:image\/(?:bmp|gif|jpeg|jpg|png|tiff|webp)|video\/(?:mpeg|mp4|ogg|webm)|audio\/(?:mp3|oga|ogg|opus));base64,[a-z0-9+/]+=*$/i

  function allowedAttribute(attr, allowedAttributeList) {
    var attrName = attr.nodeName.toLowerCase()

    if ($.inArray(attrName, allowedAttributeList) !== -1) {
      if ($.inArray(attrName, uriAttrs) !== -1) {
        return Boolean(attr.nodeValue.match(SAFE_URL_PATTERN) || attr.nodeValue.match(DATA_URL_PATTERN))
      }

      return true
    }

    var regExp = $(allowedAttributeList).filter(function (index, value) {
      return value instanceof RegExp
    })

    // Check if a regular expression validates the attribute.
    for (var i = 0, l = regExp.length; i < l; i++) {
      if (attrName.match(regExp[i])) {
        return true
      }
    }

    return false
  }

  function sanitizeHtml(unsafeHtml, whiteList, sanitizeFn) {
    if (unsafeHtml.length === 0) {
      return unsafeHtml
    }

    if (sanitizeFn && typeof sanitizeFn === 'function') {
      return sanitizeFn(unsafeHtml)
    }

    // IE 8 and below don't support createHTMLDocument
    if (!document.implementation || !document.implementation.createHTMLDocument) {
      return unsafeHtml
    }

    var createdDocument = document.implementation.createHTMLDocument('sanitization')
    createdDocument.body.innerHTML = unsafeHtml

    var whitelistKeys = $.map(whiteList, function (el, i) { return i })
    var elements = $(createdDocument.body).find('*')

    for (var i = 0, len = elements.length; i < len; i++) {
      var el = elements[i]
      var elName = el.nodeName.toLowerCase()

      if ($.inArray(elName, whitelistKeys) === -1) {
        el.parentNode.removeChild(el)

        continue
      }

      var attributeList = $.map(el.attributes, function (el) { return el })
      var whitelistedAttributes = [].concat(whiteList['*'] || [], whiteList[elName] || [])

      for (var j = 0, len2 = attributeList.length; j < len2; j++) {
        if (!allowedAttribute(attributeList[j], whitelistedAttributes)) {
          el.removeAttribute(attributeList[j].nodeName)
        }
      }
    }

    return createdDocument.body.innerHTML
  }

  // TOOLTIP PUBLIC CLASS DEFINITION
  // ===============================

  var Tooltip = function (element, options) {
    this.type       = null
    this.options    = null
    this.enabled    = null
    this.timeout    = null
    this.hoverState = null
    this.$element   = null
    this.inState    = null

    this.init('tooltip', element, options)
  }

  Tooltip.VERSION  = '3.4.1'

  Tooltip.TRANSITION_DURATION = 150

  Tooltip.DEFAULTS = {
    animation: true,
    placement: 'top',
    selector: false,
    template: '<div class="tooltip" role="tooltip"><div class="tooltip-arrow"></div><div class="tooltip-inner"></div></div>',
    trigger: 'hover focus',
    title: '',
    delay: 0,
    html: false,
    container: false,
    viewport: {
      selector: 'body',
      padding: 0
    },
    sanitize : true,
    sanitizeFn : null,
    whiteList : DefaultWhitelist
  }

  Tooltip.prototype.init = function (type, element, options) {
    this.enabled   = true
    this.type      = type
    this.$element  = $(element)
    this.options   = this.getOptions(options)
    this.$viewport = this.options.viewport && $(document).find($.isFunction(this.options.viewport) ? this.options.viewport.call(this, this.$element) : (this.options.viewport.selector || this.options.viewport))
    this.inState   = { click: false, hover: false, focus: false }

    if (this.$element[0] instanceof document.constructor && !this.options.selector) {
      throw new Error('`selector` option must be specified when initializing ' + this.type + ' on the window.document object!')
    }

    var triggers = this.options.trigger.split(' ')

    for (var i = triggers.length; i--;) {
      var trigger = triggers[i]

      if (trigger == 'click') {
        this.$element.on('click.' + this.type, this.options.selector, $.proxy(this.toggle, this))
      } else if (trigger != 'manual') {
        var eventIn  = trigger == 'hover' ? 'mouseenter' : 'focusin'
        var eventOut = trigger == 'hover' ? 'mouseleave' : 'focusout'

        this.$element.on(eventIn  + '.' + this.type, this.options.selector, $.proxy(this.enter, this))
        this.$element.on(eventOut + '.' + this.type, this.options.selector, $.proxy(this.leave, this))
      }
    }

    this.options.selector ?
      (this._options = $.extend({}, this.options, { trigger: 'manual', selector: '' })) :
      this.fixTitle()
  }

  Tooltip.prototype.getDefaults = function () {
    return Tooltip.DEFAULTS
  }

  Tooltip.prototype.getOptions = function (options) {
    var dataAttributes = this.$element.data()

    for (var dataAttr in dataAttributes) {
      if (dataAttributes.hasOwnProperty(dataAttr) && $.inArray(dataAttr, DISALLOWED_ATTRIBUTES) !== -1) {
        delete dataAttributes[dataAttr]
      }
    }

    options = $.extend({}, this.getDefaults(), dataAttributes, options)

    if (options.delay && typeof options.delay == 'number') {
      options.delay = {
        show: options.delay,
        hide: options.delay
      }
    }

    if (options.sanitize) {
      options.template = sanitizeHtml(options.template, options.whiteList, options.sanitizeFn)
    }

    return options
  }

  Tooltip.prototype.getDelegateOptions = function () {
    var options  = {}
    var defaults = this.getDefaults()

    this._options && $.each(this._options, function (key, value) {
      if (defaults[key] != value) options[key] = value
    })

    return options
  }

  Tooltip.prototype.enter = function (obj) {
    var self = obj instanceof this.constructor ?
      obj : $(obj.currentTarget).data('bs.' + this.type)

    if (!self) {
      self = new this.constructor(obj.currentTarget, this.getDelegateOptions())
      $(obj.currentTarget).data('bs.' + this.type, self)
    }

    if (obj instanceof $.Event) {
      self.inState[obj.type == 'focusin' ? 'focus' : 'hover'] = true
    }

    if (self.tip().hasClass('in') || self.hoverState == 'in') {
      self.hoverState = 'in'
      return
    }

    clearTimeout(self.timeout)

    self.hoverState = 'in'

    if (!self.options.delay || !self.options.delay.show) return self.show()

    self.timeout = setTimeout(function () {
      if (self.hoverState == 'in') self.show()
    }, self.options.delay.show)
  }

  Tooltip.prototype.isInStateTrue = function () {
    for (var key in this.inState) {
      if (this.inState[key]) return true
    }

    return false
  }

  Tooltip.prototype.leave = function (obj) {
    var self = obj instanceof this.constructor ?
      obj : $(obj.currentTarget).data('bs.' + this.type)

    if (!self) {
      self = new this.constructor(obj.currentTarget, this.getDelegateOptions())
      $(obj.currentTarget).data('bs.' + this.type, self)
    }

    if (obj instanceof $.Event) {
      self.inState[obj.type == 'focusout' ? 'focus' : 'hover'] = false
    }

    if (self.isInStateTrue()) return

    clearTimeout(self.timeout)

    self.hoverState = 'out'

    if (!self.options.delay || !self.options.delay.hide) return self.hide()

    self.timeout = setTimeout(function () {
      if (self.hoverState == 'out') self.hide()
    }, self.options.delay.hide)
  }

  Tooltip.prototype.show = function () {
    var e = $.Event('show.bs.' + this.type)

    if (this.hasContent() && this.enabled) {
      this.$element.trigger(e)

      var inDom = $.contains(this.$element[0].ownerDocument.documentElement, this.$element[0])
      if (e.isDefaultPrevented() || !inDom) return
      var that = this

      var $tip = this.tip()

      var tipId = this.getUID(this.type)

      this.setContent()
      $tip.attr('id', tipId)
      this.$element.attr('aria-describedby', tipId)

      if (this.options.animation) $tip.addClass('fade')

      var placement = typeof this.options.placement == 'function' ?
        this.options.placement.call(this, $tip[0], this.$element[0]) :
        this.options.placement

      var autoToken = /\s?auto?\s?/i
      var autoPlace = autoToken.test(placement)
      if (autoPlace) placement = placement.replace(autoToken, '') || 'top'

      $tip
        .detach()
        .css({ top: 0, left: 0, display: 'block' })
        .addClass(placement)
        .data('bs.' + this.type, this)

      this.options.container ? $tip.appendTo($(document).find(this.options.container)) : $tip.insertAfter(this.$element)
      this.$element.trigger('inserted.bs.' + this.type)

      var pos          = this.getPosition()
      var actualWidth  = $tip[0].offsetWidth
      var actualHeight = $tip[0].offsetHeight

      if (autoPlace) {
        var orgPlacement = placement
        var viewportDim = this.getPosition(this.$viewport)

        placement = placement == 'bottom' && pos.bottom + actualHeight > viewportDim.bottom ? 'top'    :
                    placement == 'top'    && pos.top    - actualHeight < viewportDim.top    ? 'bottom' :
                    placement == 'right'  && pos.right  + actualWidth  > viewportDim.width  ? 'left'   :
                    placement == 'left'   && pos.left   - actualWidth  < viewportDim.left   ? 'right'  :
                    placement

        $tip
          .removeClass(orgPlacement)
          .addClass(placement)
      }

      var calculatedOffset = this.getCalculatedOffset(placement, pos, actualWidth, actualHeight)

      this.applyPlacement(calculatedOffset, placement)

      var complete = function () {
        var prevHoverState = that.hoverState
        that.$element.trigger('shown.bs.' + that.type)
        that.hoverState = null

        if (prevHoverState == 'out') that.leave(that)
      }

      $.support.transition && this.$tip.hasClass('fade') ?
        $tip
          .one('bsTransitionEnd', complete)
          .emulateTransitionEnd(Tooltip.TRANSITION_DURATION) :
        complete()
    }
  }

  Tooltip.prototype.applyPlacement = function (offset, placement) {
    var $tip   = this.tip()
    var width  = $tip[0].offsetWidth
    var height = $tip[0].offsetHeight

    // manually read margins because getBoundingClientRect includes difference
    var marginTop = parseInt($tip.css('margin-top'), 10)
    var marginLeft = parseInt($tip.css('margin-left'), 10)

    // we must check for NaN for ie 8/9
    if (isNaN(marginTop))  marginTop  = 0
    if (isNaN(marginLeft)) marginLeft = 0

    offset.top  += marginTop
    offset.left += marginLeft

    // $.fn.offset doesn't round pixel values
    // so we use setOffset directly with our own function B-0
    $.offset.setOffset($tip[0], $.extend({
      using: function (props) {
        $tip.css({
          top: Math.round(props.top),
          left: Math.round(props.left)
        })
      }
    }, offset), 0)

    $tip.addClass('in')

    // check to see if placing tip in new offset caused the tip to resize itself
    var actualWidth  = $tip[0].offsetWidth
    var actualHeight = $tip[0].offsetHeight

    if (placement == 'top' && actualHeight != height) {
      offset.top = offset.top + height - actualHeight
    }

    var delta = this.getViewportAdjustedDelta(placement, offset, actualWidth, actualHeight)

    if (delta.left) offset.left += delta.left
    else offset.top += delta.top

    var isVertical          = /top|bottom/.test(placement)
    var arrowDelta          = isVertical ? delta.left * 2 - width + actualWidth : delta.top * 2 - height + actualHeight
    var arrowOffsetPosition = isVertical ? 'offsetWidth' : 'offsetHeight'

    $tip.offset(offset)
    this.replaceArrow(arrowDelta, $tip[0][arrowOffsetPosition], isVertical)
  }

  Tooltip.prototype.replaceArrow = function (delta, dimension, isVertical) {
    this.arrow()
      .css(isVertical ? 'left' : 'top', 50 * (1 - delta / dimension) + '%')
      .css(isVertical ? 'top' : 'left', '')
  }

  Tooltip.prototype.setContent = function () {
    var $tip  = this.tip()
    var title = this.getTitle()

    if (this.options.html) {
      if (this.options.sanitize) {
        title = sanitizeHtml(title, this.options.whiteList, this.options.sanitizeFn)
      }

      $tip.find('.tooltip-inner').html(title)
    } else {
      $tip.find('.tooltip-inner').text(title)
    }

    $tip.removeClass('fade in top bottom left right')
  }

  Tooltip.prototype.hide = function (callback) {
    var that = this
    var $tip = $(this.$tip)
    var e    = $.Event('hide.bs.' + this.type)

    function complete() {
      if (that.hoverState != 'in') $tip.detach()
      if (that.$element) { // TODO: Check whether guarding this code with this `if` is really necessary.
        that.$element
          .removeAttr('aria-describedby')
          .trigger('hidden.bs.' + that.type)
      }
      callback && callback()
    }

    this.$element.trigger(e)

    if (e.isDefaultPrevented()) return

    $tip.removeClass('in')

    $.support.transition && $tip.hasClass('fade') ?
      $tip
        .one('bsTransitionEnd', complete)
        .emulateTransitionEnd(Tooltip.TRANSITION_DURATION) :
      complete()

    this.hoverState = null

    return this
  }

  Tooltip.prototype.fixTitle = function () {
    var $e = this.$element
    if ($e.attr('title') || typeof $e.attr('data-original-title') != 'string') {
      $e.attr('data-original-title', $e.attr('title') || '').attr('title', '')
    }
  }

  Tooltip.prototype.hasContent = function () {
    return this.getTitle()
  }

  Tooltip.prototype.getPosition = function ($element) {
    $element   = $element || this.$element

    var el     = $element[0]
    var isBody = el.tagName == 'BODY'

    var elRect    = el.getBoundingClientRect()
    if (elRect.width == null) {
      // width and height are missing in IE8, so compute them manually; see https://github.com/twbs/bootstrap/issues/14093
      elRect = $.extend({}, elRect, { width: elRect.right - elRect.left, height: elRect.bottom - elRect.top })
    }
    var isSvg = window.SVGElement && el instanceof window.SVGElement
    // Avoid using $.offset() on SVGs since it gives incorrect results in jQuery 3.
    // See https://github.com/twbs/bootstrap/issues/20280
    var elOffset  = isBody ? { top: 0, left: 0 } : (isSvg ? null : $element.offset())
    var scroll    = { scroll: isBody ? document.documentElement.scrollTop || document.body.scrollTop : $element.scrollTop() }
    var outerDims = isBody ? { width: $(window).width(), height: $(window).height() } : null

    return $.extend({}, elRect, scroll, outerDims, elOffset)
  }

  Tooltip.prototype.getCalculatedOffset = function (placement, pos, actualWidth, actualHeight) {
    return placement == 'bottom' ? { top: pos.top + pos.height,   left: pos.left + pos.width / 2 - actualWidth / 2 } :
           placement == 'top'    ? { top: pos.top - actualHeight, left: pos.left + pos.width / 2 - actualWidth / 2 } :
           placement == 'left'   ? { top: pos.top + pos.height / 2 - actualHeight / 2, left: pos.left - actualWidth } :
        /* placement == 'right' */ { top: pos.top + pos.height / 2 - actualHeight / 2, left: pos.left + pos.width }

  }

  Tooltip.prototype.getViewportAdjustedDelta = function (placement, pos, actualWidth, actualHeight) {
    var delta = { top: 0, left: 0 }
    if (!this.$viewport) return delta

    var viewportPadding = this.options.viewport && this.options.viewport.padding || 0
    var viewportDimensions = this.getPosition(this.$viewport)

    if (/right|left/.test(placement)) {
      var topEdgeOffset    = pos.top - viewportPadding - viewportDimensions.scroll
      var bottomEdgeOffset = pos.top + viewportPadding - viewportDimensions.scroll + actualHeight
      if (topEdgeOffset < viewportDimensions.top) { // top overflow
        delta.top = viewportDimensions.top - topEdgeOffset
      } else if (bottomEdgeOffset > viewportDimensions.top + viewportDimensions.height) { // bottom overflow
        delta.top = viewportDimensions.top + viewportDimensions.height - bottomEdgeOffset
      }
    } else {
      var leftEdgeOffset  = pos.left - viewportPadding
      var rightEdgeOffset = pos.left + viewportPadding + actualWidth
      if (leftEdgeOffset < viewportDimensions.left) { // left overflow
        delta.left = viewportDimensions.left - leftEdgeOffset
      } else if (rightEdgeOffset > viewportDimensions.right) { // right overflow
        delta.left = viewportDimensions.left + viewportDimensions.width - rightEdgeOffset
      }
    }

    return delta
  }

  Tooltip.prototype.getTitle = function () {
    var title
    var $e = this.$element
    var o  = this.options

    title = $e.attr('data-original-title')
      || (typeof o.title == 'function' ? o.title.call($e[0]) :  o.title)

    return title
  }

  Tooltip.prototype.getUID = function (prefix) {
    do prefix += ~~(Math.random() * 1000000)
    while (document.getElementById(prefix))
    return prefix
  }

  Tooltip.prototype.tip = function () {
    if (!this.$tip) {
      this.$tip = $(this.options.template)
      if (this.$tip.length != 1) {
        throw new Error(this.type + ' `template` option must consist of exactly 1 top-level element!')
      }
    }
    return this.$tip
  }

  Tooltip.prototype.arrow = function () {
    return (this.$arrow = this.$arrow || this.tip().find('.tooltip-arrow'))
  }

  Tooltip.prototype.enable = function () {
    this.enabled = true
  }

  Tooltip.prototype.disable = function () {
    this.enabled = false
  }

  Tooltip.prototype.toggleEnabled = function () {
    this.enabled = !this.enabled
  }

  Tooltip.prototype.toggle = function (e) {
    var self = this
    if (e) {
      self = $(e.currentTarget).data('bs.' + this.type)
      if (!self) {
        self = new this.constructor(e.currentTarget, this.getDelegateOptions())
        $(e.currentTarget).data('bs.' + this.type, self)
      }
    }

    if (e) {
      self.inState.click = !self.inState.click
      if (self.isInStateTrue()) self.enter(self)
      else self.leave(self)
    } else {
      self.tip().hasClass('in') ? self.leave(self) : self.enter(self)
    }
  }

  Tooltip.prototype.destroy = function () {
    var that = this
    clearTimeout(this.timeout)
    this.hide(function () {
      that.$element.off('.' + that.type).removeData('bs.' + that.type)
      if (that.$tip) {
        that.$tip.detach()
      }
      that.$tip = null
      that.$arrow = null
      that.$viewport = null
      that.$element = null
    })
  }

  Tooltip.prototype.sanitizeHtml = function (unsafeHtml) {
    return sanitizeHtml(unsafeHtml, this.options.whiteList, this.options.sanitizeFn)
  }

  // TOOLTIP PLUGIN DEFINITION
  // =========================

  function Plugin(option) {
    return this.each(function () {
      var $this   = $(this)
      var data    = $this.data('bs.tooltip')
      var options = typeof option == 'object' && option

      if (!data && /destroy|hide/.test(option)) return
      if (!data) $this.data('bs.tooltip', (data = new Tooltip(this, options)))
      if (typeof option == 'string') data[option]()
    })
  }

  var old = $.fn.tooltip

  $.fn.tooltip             = Plugin
  $.fn.tooltip.Constructor = Tooltip


  // TOOLTIP NO CONFLICT
  // ===================

  $.fn.tooltip.noConflict = function () {
    $.fn.tooltip = old
    return this
  }

}(jQuery);

/* ========================================================================
 * Bootstrap: popover.js v3.4.1
 * https://getbootstrap.com/docs/3.4/javascript/#popovers
 * ========================================================================
 * Copyright 2011-2019 Twitter, Inc.
 * Licensed under MIT (https://github.com/twbs/bootstrap/blob/master/LICENSE)
 * ======================================================================== */


+function ($) {
  'use strict';

  // POPOVER PUBLIC CLASS DEFINITION
  // ===============================

  var Popover = function (element, options) {
    this.init('popover', element, options)
  }

  if (!$.fn.tooltip) throw new Error('Popover requires tooltip.js')

  Popover.VERSION  = '3.4.1'

  Popover.DEFAULTS = $.extend({}, $.fn.tooltip.Constructor.DEFAULTS, {
    placement: 'right',
    trigger: 'click',
    content: '',
    template: '<div class="popover" role="tooltip"><div class="arrow"></div><h3 class="popover-title"></h3><div class="popover-content"></div></div>'
  })


  // NOTE: POPOVER EXTENDS tooltip.js
  // ================================

  Popover.prototype = $.extend({}, $.fn.tooltip.Constructor.prototype)

  Popover.prototype.constructor = Popover

  Popover.prototype.getDefaults = function () {
    return Popover.DEFAULTS
  }

  Popover.prototype.setContent = function () {
    var $tip    = this.tip()
    var title   = this.getTitle()
    var content = this.getContent()

    if (this.options.html) {
      var typeContent = typeof content

      if (this.options.sanitize) {
        title = this.sanitizeHtml(title)

        if (typeContent === 'string') {
          content = this.sanitizeHtml(content)
        }
      }

      $tip.find('.popover-title').html(title)
      $tip.find('.popover-content').children().detach().end()[
        typeContent === 'string' ? 'html' : 'append'
      ](content)
    } else {
      $tip.find('.popover-title').text(title)
      $tip.find('.popover-content').children().detach().end().text(content)
    }

    $tip.removeClass('fade top bottom left right in')

    // IE8 doesn't accept hiding via the `:empty` pseudo selector, we have to do
    // this manually by checking the contents.
    if (!$tip.find('.popover-title').html()) $tip.find('.popover-title').hide()
  }

  Popover.prototype.hasContent = function () {
    return this.getTitle() || this.getContent()
  }

  Popover.prototype.getContent = function () {
    var $e = this.$element
    var o  = this.options

    return $e.attr('data-content')
      || (typeof o.content == 'function' ?
        o.content.call($e[0]) :
        o.content)
  }

  Popover.prototype.arrow = function () {
    return (this.$arrow = this.$arrow || this.tip().find('.arrow'))
  }


  // POPOVER PLUGIN DEFINITION
  // =========================

  function Plugin(option) {
    return this.each(function () {
      var $this   = $(this)
      var data    = $this.data('bs.popover')
      var options = typeof option == 'object' && option

      if (!data && /destroy|hide/.test(option)) return
      if (!data) $this.data('bs.popover', (data = new Popover(this, options)))
      if (typeof option == 'string') data[option]()
    })
  }

  var old = $.fn.popover

  $.fn.popover             = Plugin
  $.fn.popover.Constructor = Popover


  // POPOVER NO CONFLICT
  // ===================

  $.fn.popover.noConflict = function () {
    $.fn.popover = old
    return this
  }

}(jQuery);

/* ========================================================================
 * Bootstrap: scrollspy.js v3.4.1
 * https://getbootstrap.com/docs/3.4/javascript/#scrollspy
 * ========================================================================
 * Copyright 2011-2019 Twitter, Inc.
 * Licensed under MIT (https://github.com/twbs/bootstrap/blob/master/LICENSE)
 * ======================================================================== */


+function ($) {
  'use strict';

  // SCROLLSPY CLASS DEFINITION
  // ==========================

  function ScrollSpy(element, options) {
    this.$body          = $(document.body)
    this.$scrollElement = $(element).is(document.body) ? $(window) : $(element)
    this.options        = $.extend({}, ScrollSpy.DEFAULTS, options)
    this.selector       = (this.options.target || '') + ' .nav li > a'
    this.offsets        = []
    this.targets        = []
    this.activeTarget   = null
    this.scrollHeight   = 0

    this.$scrollElement.on('scroll.bs.scrollspy', $.proxy(this.process, this))
    this.refresh()
    this.process()
  }

  ScrollSpy.VERSION  = '3.4.1'

  ScrollSpy.DEFAULTS = {
    offset: 10
  }

  ScrollSpy.prototype.getScrollHeight = function () {
    return this.$scrollElement[0].scrollHeight || Math.max(this.$body[0].scrollHeight, document.documentElement.scrollHeight)
  }

  ScrollSpy.prototype.refresh = function () {
    var that          = this
    var offsetMethod  = 'offset'
    var offsetBase    = 0

    this.offsets      = []
    this.targets      = []
    this.scrollHeight = this.getScrollHeight()

    if (!$.isWindow(this.$scrollElement[0])) {
      offsetMethod = 'position'
      offsetBase   = this.$scrollElement.scrollTop()
    }

    this.$body
      .find(this.selector)
      .map(function () {
        var $el   = $(this)
        var href  = $el.data('target') || $el.attr('href')
        var $href = /^#./.test(href) && $(href)

        return ($href
          && $href.length
          && $href.is(':visible')
          && [[$href[offsetMethod]().top + offsetBase, href]]) || null
      })
      .sort(function (a, b) { return a[0] - b[0] })
      .each(function () {
        that.offsets.push(this[0])
        that.targets.push(this[1])
      })
  }

  ScrollSpy.prototype.process = function () {
    var scrollTop    = this.$scrollElement.scrollTop() + this.options.offset
    var scrollHeight = this.getScrollHeight()
    var maxScroll    = this.options.offset + scrollHeight - this.$scrollElement.height()
    var offsets      = this.offsets
    var targets      = this.targets
    var activeTarget = this.activeTarget
    var i

    if (this.scrollHeight != scrollHeight) {
      this.refresh()
    }

    if (scrollTop >= maxScroll) {
      return activeTarget != (i = targets[targets.length - 1]) && this.activate(i)
    }

    if (activeTarget && scrollTop < offsets[0]) {
      this.activeTarget = null
      return this.clear()
    }

    for (i = offsets.length; i--;) {
      activeTarget != targets[i]
        && scrollTop >= offsets[i]
        && (offsets[i + 1] === undefined || scrollTop < offsets[i + 1])
        && this.activate(targets[i])
    }
  }

  ScrollSpy.prototype.activate = function (target) {
    this.activeTarget = target

    this.clear()

    var selector = this.selector +
      '[data-target="' + target + '"],' +
      this.selector + '[href="' + target + '"]'

    var active = $(selector)
      .parents('li')
      .addClass('active')

    if (active.parent('.dropdown-menu').length) {
      active = active
        .closest('li.dropdown')
        .addClass('active')
    }

    active.trigger('activate.bs.scrollspy')
  }

  ScrollSpy.prototype.clear = function () {
    $(this.selector)
      .parentsUntil(this.options.target, '.active')
      .removeClass('active')
  }


  // SCROLLSPY PLUGIN DEFINITION
  // ===========================

  function Plugin(option) {
    return this.each(function () {
      var $this   = $(this)
      var data    = $this.data('bs.scrollspy')
      var options = typeof option == 'object' && option

      if (!data) $this.data('bs.scrollspy', (data = new ScrollSpy(this, options)))
      if (typeof option == 'string') data[option]()
    })
  }

  var old = $.fn.scrollspy

  $.fn.scrollspy             = Plugin
  $.fn.scrollspy.Constructor = ScrollSpy


  // SCROLLSPY NO CONFLICT
  // =====================

  $.fn.scrollspy.noConflict = function () {
    $.fn.scrollspy = old
    return this
  }


  // SCROLLSPY DATA-API
  // ==================

  $(window).on('load.bs.scrollspy.data-api', function () {
    $('[data-spy="scroll"]').each(function () {
      var $spy = $(this)
      Plugin.call($spy, $spy.data())
    })
  })

}(jQuery);

/* ========================================================================
 * Bootstrap: tab.js v3.4.1
 * https://getbootstrap.com/docs/3.4/javascript/#tabs
 * ========================================================================
 * Copyright 2011-2019 Twitter, Inc.
 * Licensed under MIT (https://github.com/twbs/bootstrap/blob/master/LICENSE)
 * ======================================================================== */


+function ($) {
  'use strict';

  // TAB CLASS DEFINITION
  // ====================

  var Tab = function (element) {
    // jscs:disable requireDollarBeforejQueryAssignment
    this.element = $(element)
    // jscs:enable requireDollarBeforejQueryAssignment
  }

  Tab.VERSION = '3.4.1'

  Tab.TRANSITION_DURATION = 150

  Tab.prototype.show = function () {
    var $this    = this.element
    var $ul      = $this.closest('ul:not(.dropdown-menu)')
    var selector = $this.data('target')

    if (!selector) {
      selector = $this.attr('href')
      selector = selector && selector.replace(/.*(?=#[^\s]*$)/, '') // strip for ie7
    }

    if ($this.parent('li').hasClass('active')) return

    var $previous = $ul.find('.active:last a')
    var hideEvent = $.Event('hide.bs.tab', {
      relatedTarget: $this[0]
    })
    var showEvent = $.Event('show.bs.tab', {
      relatedTarget: $previous[0]
    })

    $previous.trigger(hideEvent)
    $this.trigger(showEvent)

    if (showEvent.isDefaultPrevented() || hideEvent.isDefaultPrevented()) return

    var $target = $(document).find(selector)

    this.activate($this.closest('li'), $ul)
    this.activate($target, $target.parent(), function () {
      $previous.trigger({
        type: 'hidden.bs.tab',
        relatedTarget: $this[0]
      })
      $this.trigger({
        type: 'shown.bs.tab',
        relatedTarget: $previous[0]
      })
    })
  }

  Tab.prototype.activate = function (element, container, callback) {
    var $active    = container.find('> .active')
    var transition = callback
      && $.support.transition
      && ($active.length && $active.hasClass('fade') || !!container.find('> .fade').length)

    function next() {
      $active
        .removeClass('active')
        .find('> .dropdown-menu > .active')
        .removeClass('active')
        .end()
        .find('[data-toggle="tab"]')
        .attr('aria-expanded', false)

      element
        .addClass('active')
        .find('[data-toggle="tab"]')
        .attr('aria-expanded', true)

      if (transition) {
        element[0].offsetWidth // reflow for transition
        element.addClass('in')
      } else {
        element.removeClass('fade')
      }

      if (element.parent('.dropdown-menu').length) {
        element
          .closest('li.dropdown')
          .addClass('active')
          .end()
          .find('[data-toggle="tab"]')
          .attr('aria-expanded', true)
      }

      callback && callback()
    }

    $active.length && transition ?
      $active
        .one('bsTransitionEnd', next)
        .emulateTransitionEnd(Tab.TRANSITION_DURATION) :
      next()

    $active.removeClass('in')
  }


  // TAB PLUGIN DEFINITION
  // =====================

  function Plugin(option) {
    return this.each(function () {
      var $this = $(this)
      var data  = $this.data('bs.tab')

      if (!data) $this.data('bs.tab', (data = new Tab(this)))
      if (typeof option == 'string') data[option]()
    })
  }

  var old = $.fn.tab

  $.fn.tab             = Plugin
  $.fn.tab.Constructor = Tab


  // TAB NO CONFLICT
  // ===============

  $.fn.tab.noConflict = function () {
    $.fn.tab = old
    return this
  }


  // TAB DATA-API
  // ============

  var clickHandler = function (e) {
    e.preventDefault()
    Plugin.call($(this), 'show')
  }

  $(document)
    .on('click.bs.tab.data-api', '[data-toggle="tab"]', clickHandler)
    .on('click.bs.tab.data-api', '[data-toggle="pill"]', clickHandler)

}(jQuery);

/* ========================================================================
 * Bootstrap: affix.js v3.4.1
 * https://getbootstrap.com/docs/3.4/javascript/#affix
 * ========================================================================
 * Copyright 2011-2019 Twitter, Inc.
 * Licensed under MIT (https://github.com/twbs/bootstrap/blob/master/LICENSE)
 * ======================================================================== */


+function ($) {
  'use strict';

  // AFFIX CLASS DEFINITION
  // ======================

  var Affix = function (element, options) {
    this.options = $.extend({}, Affix.DEFAULTS, options)

    var target = this.options.target === Affix.DEFAULTS.target ? $(this.options.target) : $(document).find(this.options.target)

    this.$target = target
      .on('scroll.bs.affix.data-api', $.proxy(this.checkPosition, this))
      .on('click.bs.affix.data-api',  $.proxy(this.checkPositionWithEventLoop, this))

    this.$element     = $(element)
    this.affixed      = null
    this.unpin        = null
    this.pinnedOffset = null

    this.checkPosition()
  }

  Affix.VERSION  = '3.4.1'

  Affix.RESET    = 'affix affix-top affix-bottom'

  Affix.DEFAULTS = {
    offset: 0,
    target: window
  }

  Affix.prototype.getState = function (scrollHeight, height, offsetTop, offsetBottom) {
    var scrollTop    = this.$target.scrollTop()
    var position     = this.$element.offset()
    var targetHeight = this.$target.height()

    if (offsetTop != null && this.affixed == 'top') return scrollTop < offsetTop ? 'top' : false

    if (this.affixed == 'bottom') {
      if (offsetTop != null) return (scrollTop + this.unpin <= position.top) ? false : 'bottom'
      return (scrollTop + targetHeight <= scrollHeight - offsetBottom) ? false : 'bottom'
    }

    var initializing   = this.affixed == null
    var colliderTop    = initializing ? scrollTop : position.top
    var colliderHeight = initializing ? targetHeight : height

    if (offsetTop != null && scrollTop <= offsetTop) return 'top'
    if (offsetBottom != null && (colliderTop + colliderHeight >= scrollHeight - offsetBottom)) return 'bottom'

    return false
  }

  Affix.prototype.getPinnedOffset = function () {
    if (this.pinnedOffset) return this.pinnedOffset
    this.$element.removeClass(Affix.RESET).addClass('affix')
    var scrollTop = this.$target.scrollTop()
    var position  = this.$element.offset()
    return (this.pinnedOffset = position.top - scrollTop)
  }

  Affix.prototype.checkPositionWithEventLoop = function () {
    setTimeout($.proxy(this.checkPosition, this), 1)
  }

  Affix.prototype.checkPosition = function () {
    if (!this.$element.is(':visible')) return

    var height       = this.$element.height()
    var offset       = this.options.offset
    var offsetTop    = offset.top
    var offsetBottom = offset.bottom
    var scrollHeight = Math.max($(document).height(), $(document.body).height())

    if (typeof offset != 'object')         offsetBottom = offsetTop = offset
    if (typeof offsetTop == 'function')    offsetTop    = offset.top(this.$element)
    if (typeof offsetBottom == 'function') offsetBottom = offset.bottom(this.$element)

    var affix = this.getState(scrollHeight, height, offsetTop, offsetBottom)

    if (this.affixed != affix) {
      if (this.unpin != null) this.$element.css('top', '')

      var affixType = 'affix' + (affix ? '-' + affix : '')
      var e         = $.Event(affixType + '.bs.affix')

      this.$element.trigger(e)

      if (e.isDefaultPrevented()) return

      this.affixed = affix
      this.unpin = affix == 'bottom' ? this.getPinnedOffset() : null

      this.$element
        .removeClass(Affix.RESET)
        .addClass(affixType)
        .trigger(affixType.replace('affix', 'affixed') + '.bs.affix')
    }

    if (affix == 'bottom') {
      this.$element.offset({
        top: scrollHeight - height - offsetBottom
      })
    }
  }


  // AFFIX PLUGIN DEFINITION
  // =======================

  function Plugin(option) {
    return this.each(function () {
      var $this   = $(this)
      var data    = $this.data('bs.affix')
      var options = typeof option == 'object' && option

      if (!data) $this.data('bs.affix', (data = new Affix(this, options)))
      if (typeof option == 'string') data[option]()
    })
  }

  var old = $.fn.affix

  $.fn.affix             = Plugin
  $.fn.affix.Constructor = Affix


  // AFFIX NO CONFLICT
  // =================

  $.fn.affix.noConflict = function () {
    $.fn.affix = old
    return this
  }


  // AFFIX DATA-API
  // ==============

  $(window).on('load', function () {
    $('[data-spy="affix"]').each(function () {
      var $spy = $(this)
      var data = $spy.data()

      data.offset = data.offset || {}

      if (data.offsetBottom != null) data.offset.bottom = data.offsetBottom
      if (data.offsetTop    != null) data.offset.top    = data.offsetTop

      Plugin.call($spy, data)
    })
  })

}(jQuery);


/***/ }),

/***/ "./node_modules/css-loader/index.js!./src/form/css/form.css":
/*!*********************************************************!*\
  !*** ./node_modules/css-loader!./src/form/css/form.css ***!
  \*********************************************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

exports = module.exports = __webpack_require__(/*! ../../../node_modules/css-loader/lib/css-base.js */ "./node_modules/css-loader/lib/css-base.js")(false);
// imports


// module
exports.push([module.i, ".form-horizontal .form-group {\n    margin-right: auto;\n    margin-left: auto;\n}\n.pb-palette{\n    width:295px;\n    float:left;\n    min-height: 300px;\n    border:solid 1px #dddddd;\n    background: #ffffff;\n    margin-left:10px;\n    position: absolute;\n    padding-bottom: 20px;\n}\n.pb-hasFocus{\n    border:1px solid #9BBDD8 !important;\n}\n.pb-component{\n    background: transparent;\n    font-size: 12px;\n    padding: 5px;\n    cursor: move;\n    border: 1px solid transparent;\n    border-radius: 2.5px 2.5px 2.5px 2.5px;\n    color: #525C66;\n    transition-duration: 150ms;\n    transition-property: background-color, border-color, box-shadow;\n    white-space: normal;\n    min-width: 100px;\n}\n.pb-component:hover{\n    border: 1px solid #ddd !important;\n    background-color: rgba(3, 14, 27, 0.03);\n}\n.pb-element{\n    border: 1px solid transparent;\n    background: transparent;\n}\n.pb-element-hover{\n    border: 1px solid #9BBDD8 !important;\n}\n.pb-shadow{\n    border: #ddd solid 1px;\n    margin: 20px;\n    background-color: #ffffff;\n    padding-left:20px;\n    padding-right:20px;\n}\n.pb-dropable-grid{\n    padding: 4px;\n    min-height: 80px;\n    height: auto !important;\n    background-color: #fff;\n    border: 1px dotted #dddddd;\n}\n.pb-tab-grid{\n    padding: 4px;\n    min-height: 80px;\n    height: auto !important;\n    background-color: #fff;\n}\n.pb-carousel-container{\n    min-height: 200px;\n}\n.pb-sortable-placeholder {\n    display: block;\n    border: 1px solid #ddd;\n    min-height: 60px;\n    background: #fdfdfd;\n    height: 60px;\n    width: 100%;\n}\n.pb-canvas-container{\n    min-height: 100px;\n    height: auto !important;\n    background-color: #fff;\n    background: #fff;\n    border: 1px solid #fff;\n    padding: 2px;\n}\n.pb-tab-icon {\n    position: relative;\n    top: 1px;\n    display: inline-block;\n    font-family: 'Glyphicons Halflings';\n    font-style: normal;\n    font-weight: normal;\n    line-height: 1;\n    -webkit-font-smoothing: antialiased;\n}\n.pb-tab-toolbar {\n     float:right;\n     margin-right: 3px;\n     top: 5px;\n     right: 5px;\n     margin-top: 0px;\n     cursor: pointer;\n     color:#007fff;\n }\n.pb-icon-add {\n    cursor: pointer;\n    color: #007fff;\n}\n.pb-icon-delete {\n    cursor: pointer;\n    color: red;\n}\n.pb-toolbar{\n    background-color: #ffffff;\n    margin-left: 10px;\n    margin-right: 30px;\n    margin-top: 5px;\n}\n.pd-datalabel{\n    border-bottom: solid 1px #adadad;\n    min-width: 120px;\n    min-height: 26px;\n    display: inline-block;\n    text-align: center;\n}\n.slider-bar-left{\n    width: 310px;\n    top: 0;\n    bottom: 0;\n    /* height: auto; */\n    margin-left: 0px;\n    border-color: #f5f5f5;\n    border-right: 1px solid #ddd !important;\n    background-color: #ffffff;\n}", ""]);

// exports


/***/ }),

/***/ "./node_modules/css-loader/index.js!./src/form/css/iconfont.css":
/*!*************************************************************!*\
  !*** ./node_modules/css-loader!./src/form/css/iconfont.css ***!
  \*************************************************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

var escape = __webpack_require__(/*! ../../../node_modules/css-loader/lib/url/escape.js */ "./node_modules/css-loader/lib/url/escape.js");
exports = module.exports = __webpack_require__(/*! ../../../node_modules/css-loader/lib/css-base.js */ "./node_modules/css-loader/lib/css-base.js")(false);
// imports


// module
exports.push([module.i, "\n@font-face {font-family: \"form\";\n  src: url(" + escape(__webpack_require__(/*! ./iconfont.eot */ "./src/form/css/iconfont.eot")) + "); /* IE9*/\n  src: url(" + escape(__webpack_require__(/*! ./iconfont.ttf */ "./src/form/css/iconfont.ttf")) + ") format('truetype');\n}\n\n.form {\n  font-family:\"form\" !important;\n  font-size:13px;\n  font-style:normal;\n  -webkit-font-smoothing: antialiased;\n  -moz-osx-font-smoothing: grayscale;\n}\n\n.form-3col:before { content: \"\\E6E7\"; }\n\n.form-custom-col:before { content: \"\\E614\"; }\n\n.form-dropdown:before { content: \"\\E606\"; }\n\n.form-checkbox:before { content: \"\\E60D\"; }\n\n.form-datetime:before { content: \"\\E6CC\"; }\n\n.form-radio:before { content: \"\\E612\"; }\n\n.form-tab:before { content: \"\\E61F\"; }\n\n.form-danye-:before { content: \"\\E603\"; }\n\n.form-submit:before { content: \"\\E670\"; }\n\n.form-textarea:before { content: \"\\E6EA\"; }\n\n.form-textbox:before { content: \"\\E6EB\"; }\n\n.form-2col:before { content: \"\\E64B\"; }\n\n.form-4col:before { content: \"\\E602\"; }\n\n.form-reset:before { content: \"\\E6E8\"; }\n\n.form-1col:before { content: \"\\E649\"; }\n\n", ""]);

// exports


/***/ }),

/***/ "./node_modules/css-loader/index.js!./src/form/external/bootstrap-datetimepicker.css":
/*!**********************************************************************************!*\
  !*** ./node_modules/css-loader!./src/form/external/bootstrap-datetimepicker.css ***!
  \**********************************************************************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

exports = module.exports = __webpack_require__(/*! ../../../node_modules/css-loader/lib/css-base.js */ "./node_modules/css-loader/lib/css-base.js")(false);
// imports


// module
exports.push([module.i, "/*!\n * Datetimepicker for Bootstrap\n *\n * Copyright 2012 Stefan Petre\n * Improvements by Andrew Rowls\n * Licensed under the Apache License v2.0\n * http://www.apache.org/licenses/LICENSE-2.0\n *\n */\n.datetimepicker {\n\tpadding: 4px;\n\tmargin-top: 1px;\n\t-webkit-border-radius: 4px;\n\t-moz-border-radius: 4px;\n\tborder-radius: 4px;\n\tdirection: ltr;\n}\n\n.datetimepicker-inline {\n\twidth: 220px;\n}\n\n.datetimepicker.datetimepicker-rtl {\n\tdirection: rtl;\n}\n\n.datetimepicker.datetimepicker-rtl table tr td span {\n\tfloat: right;\n}\n\n.datetimepicker-dropdown, .datetimepicker-dropdown-left {\n\ttop: 0;\n\tleft: 0;\n}\n\n[class*=\" datetimepicker-dropdown\"]:before {\n\tcontent: '';\n\tdisplay: inline-block;\n\tborder-left: 7px solid transparent;\n\tborder-right: 7px solid transparent;\n\tborder-bottom: 7px solid #cccccc;\n\tborder-bottom-color: rgba(0, 0, 0, 0.2);\n\tposition: absolute;\n}\n\n[class*=\" datetimepicker-dropdown\"]:after {\n\tcontent: '';\n\tdisplay: inline-block;\n\tborder-left: 6px solid transparent;\n\tborder-right: 6px solid transparent;\n\tborder-bottom: 6px solid #ffffff;\n\tposition: absolute;\n}\n\n[class*=\" datetimepicker-dropdown-top\"]:before {\n\tcontent: '';\n\tdisplay: inline-block;\n\tborder-left: 7px solid transparent;\n\tborder-right: 7px solid transparent;\n\tborder-top: 7px solid #cccccc;\n\tborder-top-color: rgba(0, 0, 0, 0.2);\n\tborder-bottom: 0;\n}\n\n[class*=\" datetimepicker-dropdown-top\"]:after {\n\tcontent: '';\n\tdisplay: inline-block;\n\tborder-left: 6px solid transparent;\n\tborder-right: 6px solid transparent;\n\tborder-top: 6px solid #ffffff;\n\tborder-bottom: 0;\n}\n\n.datetimepicker-dropdown-bottom-left:before {\n\ttop: -7px;\n\tright: 6px;\n}\n\n.datetimepicker-dropdown-bottom-left:after {\n\ttop: -6px;\n\tright: 7px;\n}\n\n.datetimepicker-dropdown-bottom-right:before {\n\ttop: -7px;\n\tleft: 6px;\n}\n\n.datetimepicker-dropdown-bottom-right:after {\n\ttop: -6px;\n\tleft: 7px;\n}\n\n.datetimepicker-dropdown-top-left:before {\n\tbottom: -7px;\n\tright: 6px;\n}\n\n.datetimepicker-dropdown-top-left:after {\n\tbottom: -6px;\n\tright: 7px;\n}\n\n.datetimepicker-dropdown-top-right:before {\n\tbottom: -7px;\n\tleft: 6px;\n}\n\n.datetimepicker-dropdown-top-right:after {\n\tbottom: -6px;\n\tleft: 7px;\n}\n\n.datetimepicker > div {\n\tdisplay: none;\n}\n\n.datetimepicker.minutes div.datetimepicker-minutes {\n\tdisplay: block;\n}\n\n.datetimepicker.hours div.datetimepicker-hours {\n\tdisplay: block;\n}\n\n.datetimepicker.days div.datetimepicker-days {\n\tdisplay: block;\n}\n\n.datetimepicker.months div.datetimepicker-months {\n\tdisplay: block;\n}\n\n.datetimepicker.years div.datetimepicker-years {\n\tdisplay: block;\n}\n\n.datetimepicker table {\n\tmargin: 0;\n}\n\n.datetimepicker  td,\n.datetimepicker th {\n\ttext-align: center;\n\twidth: 20px;\n\theight: 20px;\n\t-webkit-border-radius: 4px;\n\t-moz-border-radius: 4px;\n\tborder-radius: 4px;\n\tborder: none;\n}\n\n.table-striped .datetimepicker table tr td,\n.table-striped .datetimepicker table tr th {\n\tbackground-color: transparent;\n}\n\n.datetimepicker table tr td.minute:hover {\n\tbackground: #eeeeee;\n\tcursor: pointer;\n}\n\n.datetimepicker table tr td.hour:hover {\n\tbackground: #eeeeee;\n\tcursor: pointer;\n}\n\n.datetimepicker table tr td.day:hover {\n\tbackground: #eeeeee;\n\tcursor: pointer;\n}\n\n.datetimepicker table tr td.old,\n.datetimepicker table tr td.new {\n\tcolor: #999999;\n}\n\n.datetimepicker table tr td.disabled,\n.datetimepicker table tr td.disabled:hover {\n\tbackground: none;\n\tcolor: #999999;\n\tcursor: default;\n}\n\n.datetimepicker table tr td.today,\n.datetimepicker table tr td.today:hover,\n.datetimepicker table tr td.today.disabled,\n.datetimepicker table tr td.today.disabled:hover {\n\tbackground-color: #fde19a;\n\tbackground-image: -moz-linear-gradient(top, #fdd49a, #fdf59a);\n\tbackground-image: -ms-linear-gradient(top, #fdd49a, #fdf59a);\n\tbackground-image: -webkit-gradient(linear, 0 0, 0 100%, from(#fdd49a), to(#fdf59a));\n\tbackground-image: -webkit-linear-gradient(top, #fdd49a, #fdf59a);\n\tbackground-image: -o-linear-gradient(top, #fdd49a, #fdf59a);\n\tbackground-image: linear-gradient(to bottom, #fdd49a, #fdf59a);\n\tbackground-repeat: repeat-x;\n\tfilter: progid:DXImageTransform.Microsoft.gradient(startColorstr='#fdd49a', endColorstr='#fdf59a', GradientType=0);\n\tborder-color: #fdf59a #fdf59a #fbed50;\n\tborder-color: rgba(0, 0, 0, 0.1) rgba(0, 0, 0, 0.1) rgba(0, 0, 0, 0.25);\n\tfilter: progid:DXImageTransform.Microsoft.gradient(enabled=false);\n}\n\n.datetimepicker table tr td.today:hover,\n.datetimepicker table tr td.today:hover:hover,\n.datetimepicker table tr td.today.disabled:hover,\n.datetimepicker table tr td.today.disabled:hover:hover,\n.datetimepicker table tr td.today:active,\n.datetimepicker table tr td.today:hover:active,\n.datetimepicker table tr td.today.disabled:active,\n.datetimepicker table tr td.today.disabled:hover:active,\n.datetimepicker table tr td.today.active,\n.datetimepicker table tr td.today:hover.active,\n.datetimepicker table tr td.today.disabled.active,\n.datetimepicker table tr td.today.disabled:hover.active,\n.datetimepicker table tr td.today.disabled,\n.datetimepicker table tr td.today:hover.disabled,\n.datetimepicker table tr td.today.disabled.disabled,\n.datetimepicker table tr td.today.disabled:hover.disabled,\n.datetimepicker table tr td.today[disabled],\n.datetimepicker table tr td.today:hover[disabled],\n.datetimepicker table tr td.today.disabled[disabled],\n.datetimepicker table tr td.today.disabled:hover[disabled] {\n\tbackground-color: #fdf59a;\n}\n\n.datetimepicker table tr td.today:active,\n.datetimepicker table tr td.today:hover:active,\n.datetimepicker table tr td.today.disabled:active,\n.datetimepicker table tr td.today.disabled:hover:active,\n.datetimepicker table tr td.today.active,\n.datetimepicker table tr td.today:hover.active,\n.datetimepicker table tr td.today.disabled.active,\n.datetimepicker table tr td.today.disabled:hover.active {\n\tbackground-color: #fbf069;\n}\n\n.datetimepicker table tr td.active,\n.datetimepicker table tr td.active:hover,\n.datetimepicker table tr td.active.disabled,\n.datetimepicker table tr td.active.disabled:hover {\n\tbackground-color: #006dcc;\n\tbackground-image: -moz-linear-gradient(top, #0088cc, #0044cc);\n\tbackground-image: -ms-linear-gradient(top, #0088cc, #0044cc);\n\tbackground-image: -webkit-gradient(linear, 0 0, 0 100%, from(#0088cc), to(#0044cc));\n\tbackground-image: -webkit-linear-gradient(top, #0088cc, #0044cc);\n\tbackground-image: -o-linear-gradient(top, #0088cc, #0044cc);\n\tbackground-image: linear-gradient(to bottom, #0088cc, #0044cc);\n\tbackground-repeat: repeat-x;\n\tfilter: progid:DXImageTransform.Microsoft.gradient(startColorstr='#0088cc', endColorstr='#0044cc', GradientType=0);\n\tborder-color: #0044cc #0044cc #002a80;\n\tborder-color: rgba(0, 0, 0, 0.1) rgba(0, 0, 0, 0.1) rgba(0, 0, 0, 0.25);\n\tfilter: progid:DXImageTransform.Microsoft.gradient(enabled=false);\n\tcolor: #ffffff;\n\ttext-shadow: 0 -1px 0 rgba(0, 0, 0, 0.25);\n}\n\n.datetimepicker table tr td.active:hover,\n.datetimepicker table tr td.active:hover:hover,\n.datetimepicker table tr td.active.disabled:hover,\n.datetimepicker table tr td.active.disabled:hover:hover,\n.datetimepicker table tr td.active:active,\n.datetimepicker table tr td.active:hover:active,\n.datetimepicker table tr td.active.disabled:active,\n.datetimepicker table tr td.active.disabled:hover:active,\n.datetimepicker table tr td.active.active,\n.datetimepicker table tr td.active:hover.active,\n.datetimepicker table tr td.active.disabled.active,\n.datetimepicker table tr td.active.disabled:hover.active,\n.datetimepicker table tr td.active.disabled,\n.datetimepicker table tr td.active:hover.disabled,\n.datetimepicker table tr td.active.disabled.disabled,\n.datetimepicker table tr td.active.disabled:hover.disabled,\n.datetimepicker table tr td.active[disabled],\n.datetimepicker table tr td.active:hover[disabled],\n.datetimepicker table tr td.active.disabled[disabled],\n.datetimepicker table tr td.active.disabled:hover[disabled] {\n\tbackground-color: #0044cc;\n}\n\n.datetimepicker table tr td.active:active,\n.datetimepicker table tr td.active:hover:active,\n.datetimepicker table tr td.active.disabled:active,\n.datetimepicker table tr td.active.disabled:hover:active,\n.datetimepicker table tr td.active.active,\n.datetimepicker table tr td.active:hover.active,\n.datetimepicker table tr td.active.disabled.active,\n.datetimepicker table tr td.active.disabled:hover.active {\n\tbackground-color: #003399;\n}\n\n.datetimepicker table tr td span {\n\tdisplay: block;\n\twidth: 23%;\n\theight: 54px;\n\tline-height: 54px;\n\tfloat: left;\n\tmargin: 1%;\n\tcursor: pointer;\n\t-webkit-border-radius: 4px;\n\t-moz-border-radius: 4px;\n\tborder-radius: 4px;\n}\n\n.datetimepicker .datetimepicker-hours span {\n\theight: 26px;\n\tline-height: 26px;\n}\n\n.datetimepicker .datetimepicker-hours table tr td span.hour_am,\n.datetimepicker .datetimepicker-hours table tr td span.hour_pm {\n\twidth: 14.6%;\n}\n\n.datetimepicker .datetimepicker-hours fieldset legend,\n.datetimepicker .datetimepicker-minutes fieldset legend {\n\tmargin-bottom: inherit;\n\tline-height: 30px;\n}\n\n.datetimepicker .datetimepicker-minutes span {\n\theight: 26px;\n\tline-height: 26px;\n}\n\n.datetimepicker table tr td span:hover {\n\tbackground: #eeeeee;\n}\n\n.datetimepicker table tr td span.disabled,\n.datetimepicker table tr td span.disabled:hover {\n\tbackground: none;\n\tcolor: #999999;\n\tcursor: default;\n}\n\n.datetimepicker table tr td span.active,\n.datetimepicker table tr td span.active:hover,\n.datetimepicker table tr td span.active.disabled,\n.datetimepicker table tr td span.active.disabled:hover {\n\tbackground-color: #006dcc;\n\tbackground-image: -moz-linear-gradient(top, #0088cc, #0044cc);\n\tbackground-image: -ms-linear-gradient(top, #0088cc, #0044cc);\n\tbackground-image: -webkit-gradient(linear, 0 0, 0 100%, from(#0088cc), to(#0044cc));\n\tbackground-image: -webkit-linear-gradient(top, #0088cc, #0044cc);\n\tbackground-image: -o-linear-gradient(top, #0088cc, #0044cc);\n\tbackground-image: linear-gradient(to bottom, #0088cc, #0044cc);\n\tbackground-repeat: repeat-x;\n\tfilter: progid:DXImageTransform.Microsoft.gradient(startColorstr='#0088cc', endColorstr='#0044cc', GradientType=0);\n\tborder-color: #0044cc #0044cc #002a80;\n\tborder-color: rgba(0, 0, 0, 0.1) rgba(0, 0, 0, 0.1) rgba(0, 0, 0, 0.25);\n\tfilter: progid:DXImageTransform.Microsoft.gradient(enabled=false);\n\tcolor: #ffffff;\n\ttext-shadow: 0 -1px 0 rgba(0, 0, 0, 0.25);\n}\n\n.datetimepicker table tr td span.active:hover,\n.datetimepicker table tr td span.active:hover:hover,\n.datetimepicker table tr td span.active.disabled:hover,\n.datetimepicker table tr td span.active.disabled:hover:hover,\n.datetimepicker table tr td span.active:active,\n.datetimepicker table tr td span.active:hover:active,\n.datetimepicker table tr td span.active.disabled:active,\n.datetimepicker table tr td span.active.disabled:hover:active,\n.datetimepicker table tr td span.active.active,\n.datetimepicker table tr td span.active:hover.active,\n.datetimepicker table tr td span.active.disabled.active,\n.datetimepicker table tr td span.active.disabled:hover.active,\n.datetimepicker table tr td span.active.disabled,\n.datetimepicker table tr td span.active:hover.disabled,\n.datetimepicker table tr td span.active.disabled.disabled,\n.datetimepicker table tr td span.active.disabled:hover.disabled,\n.datetimepicker table tr td span.active[disabled],\n.datetimepicker table tr td span.active:hover[disabled],\n.datetimepicker table tr td span.active.disabled[disabled],\n.datetimepicker table tr td span.active.disabled:hover[disabled] {\n\tbackground-color: #0044cc;\n}\n\n.datetimepicker table tr td span.active:active,\n.datetimepicker table tr td span.active:hover:active,\n.datetimepicker table tr td span.active.disabled:active,\n.datetimepicker table tr td span.active.disabled:hover:active,\n.datetimepicker table tr td span.active.active,\n.datetimepicker table tr td span.active:hover.active,\n.datetimepicker table tr td span.active.disabled.active,\n.datetimepicker table tr td span.active.disabled:hover.active {\n\tbackground-color: #003399;\n}\n\n.datetimepicker table tr td span.old {\n\tcolor: #999999;\n}\n\n.datetimepicker th.switch {\n\twidth: 145px;\n}\n\n.datetimepicker th span.glyphicon {\n\tpointer-events: none;\n}\n\n.datetimepicker thead tr:first-child th,\n.datetimepicker tfoot th {\n\tcursor: pointer;\n}\n\n.datetimepicker thead tr:first-child th:hover,\n.datetimepicker tfoot th:hover {\n\tbackground: #eeeeee;\n}\n\n.input-append.date .add-on i,\n.input-prepend.date .add-on i,\n.input-group.date .input-group-addon span {\n\tcursor: pointer;\n\twidth: 14px;\n\theight: 14px;\n}\n", ""]);

// exports


/***/ }),

/***/ "./node_modules/css-loader/index.js!./src/form/external/jquery-ui.css":
/*!*******************************************************************!*\
  !*** ./node_modules/css-loader!./src/form/external/jquery-ui.css ***!
  \*******************************************************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

var escape = __webpack_require__(/*! ../../../node_modules/css-loader/lib/url/escape.js */ "./node_modules/css-loader/lib/url/escape.js");
exports = module.exports = __webpack_require__(/*! ../../../node_modules/css-loader/lib/css-base.js */ "./node_modules/css-loader/lib/css-base.js")(false);
// imports


// module
exports.push([module.i, "/*! jQuery UI - v1.12.1 - 2017-10-13\n* http://jqueryui.com\n* Includes: draggable.css, core.css, resizable.css, selectable.css, sortable.css, theme.css\n* To view and modify this theme, visit http://jqueryui.com/themeroller/?scope=&folderName=base&cornerRadiusShadow=8px&offsetLeftShadow=0px&offsetTopShadow=0px&thicknessShadow=5px&opacityShadow=30&bgImgOpacityShadow=0&bgTextureShadow=flat&bgColorShadow=666666&opacityOverlay=30&bgImgOpacityOverlay=0&bgTextureOverlay=flat&bgColorOverlay=aaaaaa&iconColorError=cc0000&fcError=5f3f3f&borderColorError=f1a899&bgTextureError=flat&bgColorError=fddfdf&iconColorHighlight=777620&fcHighlight=777620&borderColorHighlight=dad55e&bgTextureHighlight=flat&bgColorHighlight=fffa90&iconColorActive=ffffff&fcActive=ffffff&borderColorActive=003eff&bgTextureActive=flat&bgColorActive=007fff&iconColorHover=555555&fcHover=2b2b2b&borderColorHover=cccccc&bgTextureHover=flat&bgColorHover=ededed&iconColorDefault=777777&fcDefault=454545&borderColorDefault=c5c5c5&bgTextureDefault=flat&bgColorDefault=f6f6f6&iconColorContent=444444&fcContent=333333&borderColorContent=dddddd&bgTextureContent=flat&bgColorContent=ffffff&iconColorHeader=444444&fcHeader=333333&borderColorHeader=dddddd&bgTextureHeader=flat&bgColorHeader=e9e9e9&cornerRadius=3px&fwDefault=normal&fsDefault=1em&ffDefault=Arial%2CHelvetica%2Csans-serif\n* Copyright jQuery Foundation and other contributors; Licensed MIT */\n\n.ui-draggable-handle {\n\t-ms-touch-action: none;\n\ttouch-action: none;\n}\n/* Layout helpers\n----------------------------------*/\n.ui-helper-hidden {\n\tdisplay: none;\n}\n.ui-helper-hidden-accessible {\n\tborder: 0;\n\tclip: rect(0 0 0 0);\n\theight: 1px;\n\tmargin: -1px;\n\toverflow: hidden;\n\tpadding: 0;\n\tposition: absolute;\n\twidth: 1px;\n}\n.ui-helper-reset {\n\tmargin: 0;\n\tpadding: 0;\n\tborder: 0;\n\toutline: 0;\n\tline-height: 1.3;\n\ttext-decoration: none;\n\tfont-size: 100%;\n\tlist-style: none;\n}\n.ui-helper-clearfix:before,\n.ui-helper-clearfix:after {\n\tcontent: \"\";\n\tdisplay: table;\n\tborder-collapse: collapse;\n}\n.ui-helper-clearfix:after {\n\tclear: both;\n}\n.ui-helper-zfix {\n\twidth: 100%;\n\theight: 100%;\n\ttop: 0;\n\tleft: 0;\n\tposition: absolute;\n\topacity: 0;\n\tfilter:Alpha(Opacity=0); /* support: IE8 */\n}\n\n.ui-front {\n\tz-index: 100;\n}\n\n\n/* Interaction Cues\n----------------------------------*/\n.ui-state-disabled {\n\tcursor: default !important;\n\tpointer-events: none;\n}\n\n\n/* Icons\n----------------------------------*/\n.ui-icon {\n\tdisplay: inline-block;\n\tvertical-align: middle;\n\tmargin-top: -.25em;\n\tposition: relative;\n\ttext-indent: -99999px;\n\toverflow: hidden;\n\tbackground-repeat: no-repeat;\n}\n\n.ui-widget-icon-block {\n\tleft: 50%;\n\tmargin-left: -8px;\n\tdisplay: block;\n}\n\n/* Misc visuals\n----------------------------------*/\n\n/* Overlays */\n.ui-widget-overlay {\n\tposition: fixed;\n\ttop: 0;\n\tleft: 0;\n\twidth: 100%;\n\theight: 100%;\n}\n.ui-resizable {\n\tposition: relative;\n}\n.ui-resizable-handle {\n\tposition: absolute;\n\tfont-size: 0.1px;\n\tdisplay: block;\n\t-ms-touch-action: none;\n\ttouch-action: none;\n}\n.ui-resizable-disabled .ui-resizable-handle,\n.ui-resizable-autohide .ui-resizable-handle {\n\tdisplay: none;\n}\n.ui-resizable-n {\n\tcursor: n-resize;\n\theight: 7px;\n\twidth: 100%;\n\ttop: -5px;\n\tleft: 0;\n}\n.ui-resizable-s {\n\tcursor: s-resize;\n\theight: 7px;\n\twidth: 100%;\n\tbottom: -5px;\n\tleft: 0;\n}\n.ui-resizable-e {\n\tcursor: e-resize;\n\twidth: 7px;\n\tright: -5px;\n\ttop: 0;\n\theight: 100%;\n}\n.ui-resizable-w {\n\tcursor: w-resize;\n\twidth: 7px;\n\tleft: -5px;\n\ttop: 0;\n\theight: 100%;\n}\n.ui-resizable-se {\n\tcursor: se-resize;\n\twidth: 12px;\n\theight: 12px;\n\tright: 1px;\n\tbottom: 1px;\n}\n.ui-resizable-sw {\n\tcursor: sw-resize;\n\twidth: 9px;\n\theight: 9px;\n\tleft: -5px;\n\tbottom: -5px;\n}\n.ui-resizable-nw {\n\tcursor: nw-resize;\n\twidth: 9px;\n\theight: 9px;\n\tleft: -5px;\n\ttop: -5px;\n}\n.ui-resizable-ne {\n\tcursor: ne-resize;\n\twidth: 9px;\n\theight: 9px;\n\tright: -5px;\n\ttop: -5px;\n}\n.ui-selectable {\n\t-ms-touch-action: none;\n\ttouch-action: none;\n}\n.ui-selectable-helper {\n\tposition: absolute;\n\tz-index: 100;\n\tborder: 1px dotted black;\n}\n.ui-sortable-handle {\n\t-ms-touch-action: none;\n\ttouch-action: none;\n}\n\n/* Component containers\n----------------------------------*/\n.ui-widget {\n\tfont-family: Arial,Helvetica,sans-serif;\n\tfont-size: 1em;\n}\n.ui-widget .ui-widget {\n\tfont-size: 1em;\n}\n.ui-widget input,\n.ui-widget select,\n.ui-widget textarea,\n.ui-widget button {\n\tfont-family: Arial,Helvetica,sans-serif;\n\tfont-size: 1em;\n}\n.ui-widget.ui-widget-content {\n\tborder: 1px solid #c5c5c5;\n}\n.ui-widget-content {\n\tborder: 1px solid #dddddd;\n\tbackground: #ffffff;\n\tcolor: #333333;\n}\n.ui-widget-content a {\n\tcolor: #333333;\n}\n.ui-widget-header {\n\tborder: 1px solid #dddddd;\n\tbackground: #e9e9e9;\n\tcolor: #333333;\n\tfont-weight: bold;\n}\n.ui-widget-header a {\n\tcolor: #333333;\n}\n\n/* Interaction states\n----------------------------------*/\n.ui-state-default,\n.ui-widget-content .ui-state-default,\n.ui-widget-header .ui-state-default,\n.ui-button,\n\n/* We use html here because we need a greater specificity to make sure disabled\nworks properly when clicked or hovered */\nhtml .ui-button.ui-state-disabled:hover,\nhtml .ui-button.ui-state-disabled:active {\n\tborder: 1px solid #c5c5c5;\n\tbackground: #f6f6f6;\n\tfont-weight: normal;\n\tcolor: #454545;\n}\n.ui-state-default a,\n.ui-state-default a:link,\n.ui-state-default a:visited,\na.ui-button,\na:link.ui-button,\na:visited.ui-button,\n.ui-button {\n\tcolor: #454545;\n\ttext-decoration: none;\n}\n.ui-state-hover,\n.ui-widget-content .ui-state-hover,\n.ui-widget-header .ui-state-hover,\n.ui-state-focus,\n.ui-widget-content .ui-state-focus,\n.ui-widget-header .ui-state-focus,\n.ui-button:hover,\n.ui-button:focus {\n\tborder: 1px solid #cccccc;\n\tbackground: #ededed;\n\tfont-weight: normal;\n\tcolor: #2b2b2b;\n}\n.ui-state-hover a,\n.ui-state-hover a:hover,\n.ui-state-hover a:link,\n.ui-state-hover a:visited,\n.ui-state-focus a,\n.ui-state-focus a:hover,\n.ui-state-focus a:link,\n.ui-state-focus a:visited,\na.ui-button:hover,\na.ui-button:focus {\n\tcolor: #2b2b2b;\n\ttext-decoration: none;\n}\n\n.ui-visual-focus {\n\tbox-shadow: 0 0 3px 1px rgb(94, 158, 214);\n}\n.ui-state-active,\n.ui-widget-content .ui-state-active,\n.ui-widget-header .ui-state-active,\na.ui-button:active,\n.ui-button:active,\n.ui-button.ui-state-active:hover {\n\tborder: 1px solid #003eff;\n\tbackground: #007fff;\n\tfont-weight: normal;\n\tcolor: #ffffff;\n}\n.ui-icon-background,\n.ui-state-active .ui-icon-background {\n\tborder: #003eff;\n\tbackground-color: #ffffff;\n}\n.ui-state-active a,\n.ui-state-active a:link,\n.ui-state-active a:visited {\n\tcolor: #ffffff;\n\ttext-decoration: none;\n}\n\n/* Interaction Cues\n----------------------------------*/\n.ui-state-highlight,\n.ui-widget-content .ui-state-highlight,\n.ui-widget-header .ui-state-highlight {\n\tborder: 1px solid #dad55e;\n\tbackground: #fffa90;\n\tcolor: #777620;\n}\n.ui-state-checked {\n\tborder: 1px solid #dad55e;\n\tbackground: #fffa90;\n}\n.ui-state-highlight a,\n.ui-widget-content .ui-state-highlight a,\n.ui-widget-header .ui-state-highlight a {\n\tcolor: #777620;\n}\n.ui-state-error,\n.ui-widget-content .ui-state-error,\n.ui-widget-header .ui-state-error {\n\tborder: 1px solid #f1a899;\n\tbackground: #fddfdf;\n\tcolor: #5f3f3f;\n}\n.ui-state-error a,\n.ui-widget-content .ui-state-error a,\n.ui-widget-header .ui-state-error a {\n\tcolor: #5f3f3f;\n}\n.ui-state-error-text,\n.ui-widget-content .ui-state-error-text,\n.ui-widget-header .ui-state-error-text {\n\tcolor: #5f3f3f;\n}\n.ui-priority-primary,\n.ui-widget-content .ui-priority-primary,\n.ui-widget-header .ui-priority-primary {\n\tfont-weight: bold;\n}\n.ui-priority-secondary,\n.ui-widget-content .ui-priority-secondary,\n.ui-widget-header .ui-priority-secondary {\n\topacity: .7;\n\tfilter:Alpha(Opacity=70); /* support: IE8 */\n\tfont-weight: normal;\n}\n.ui-state-disabled,\n.ui-widget-content .ui-state-disabled,\n.ui-widget-header .ui-state-disabled {\n\topacity: .35;\n\tfilter:Alpha(Opacity=35); /* support: IE8 */\n\tbackground-image: none;\n}\n.ui-state-disabled .ui-icon {\n\tfilter:Alpha(Opacity=35); /* support: IE8 - See #6059 */\n}\n\n/* Icons\n----------------------------------*/\n\n/* states and images */\n.ui-icon {\n\twidth: 16px;\n\theight: 16px;\n}\n.ui-icon,\n.ui-widget-content .ui-icon {\n\tbackground-image: url(" + escape(__webpack_require__(/*! ./images/ui-icons_444444_256x240.png */ "./src/form/external/images/ui-icons_444444_256x240.png")) + ");\n}\n.ui-widget-header .ui-icon {\n\tbackground-image: url(" + escape(__webpack_require__(/*! ./images/ui-icons_444444_256x240.png */ "./src/form/external/images/ui-icons_444444_256x240.png")) + ");\n}\n.ui-state-hover .ui-icon,\n.ui-state-focus .ui-icon,\n.ui-button:hover .ui-icon,\n.ui-button:focus .ui-icon {\n\tbackground-image: url(" + escape(__webpack_require__(/*! ./images/ui-icons_555555_256x240.png */ "./src/form/external/images/ui-icons_555555_256x240.png")) + ");\n}\n.ui-state-active .ui-icon,\n.ui-button:active .ui-icon {\n\tbackground-image: url(" + escape(__webpack_require__(/*! ./images/ui-icons_ffffff_256x240.png */ "./src/form/external/images/ui-icons_ffffff_256x240.png")) + ");\n}\n.ui-state-highlight .ui-icon,\n.ui-button .ui-state-highlight.ui-icon {\n\tbackground-image: url(" + escape(__webpack_require__(/*! ./images/ui-icons_777620_256x240.png */ "./src/form/external/images/ui-icons_777620_256x240.png")) + ");\n}\n.ui-state-error .ui-icon,\n.ui-state-error-text .ui-icon {\n\tbackground-image: url(" + escape(__webpack_require__(/*! ./images/ui-icons_cc0000_256x240.png */ "./src/form/external/images/ui-icons_cc0000_256x240.png")) + ");\n}\n.ui-button .ui-icon {\n\tbackground-image: url(" + escape(__webpack_require__(/*! ./images/ui-icons_777777_256x240.png */ "./src/form/external/images/ui-icons_777777_256x240.png")) + ");\n}\n\n/* positioning */\n.ui-icon-blank { background-position: 16px 16px; }\n.ui-icon-caret-1-n { background-position: 0 0; }\n.ui-icon-caret-1-ne { background-position: -16px 0; }\n.ui-icon-caret-1-e { background-position: -32px 0; }\n.ui-icon-caret-1-se { background-position: -48px 0; }\n.ui-icon-caret-1-s { background-position: -65px 0; }\n.ui-icon-caret-1-sw { background-position: -80px 0; }\n.ui-icon-caret-1-w { background-position: -96px 0; }\n.ui-icon-caret-1-nw { background-position: -112px 0; }\n.ui-icon-caret-2-n-s { background-position: -128px 0; }\n.ui-icon-caret-2-e-w { background-position: -144px 0; }\n.ui-icon-triangle-1-n { background-position: 0 -16px; }\n.ui-icon-triangle-1-ne { background-position: -16px -16px; }\n.ui-icon-triangle-1-e { background-position: -32px -16px; }\n.ui-icon-triangle-1-se { background-position: -48px -16px; }\n.ui-icon-triangle-1-s { background-position: -65px -16px; }\n.ui-icon-triangle-1-sw { background-position: -80px -16px; }\n.ui-icon-triangle-1-w { background-position: -96px -16px; }\n.ui-icon-triangle-1-nw { background-position: -112px -16px; }\n.ui-icon-triangle-2-n-s { background-position: -128px -16px; }\n.ui-icon-triangle-2-e-w { background-position: -144px -16px; }\n.ui-icon-arrow-1-n { background-position: 0 -32px; }\n.ui-icon-arrow-1-ne { background-position: -16px -32px; }\n.ui-icon-arrow-1-e { background-position: -32px -32px; }\n.ui-icon-arrow-1-se { background-position: -48px -32px; }\n.ui-icon-arrow-1-s { background-position: -65px -32px; }\n.ui-icon-arrow-1-sw { background-position: -80px -32px; }\n.ui-icon-arrow-1-w { background-position: -96px -32px; }\n.ui-icon-arrow-1-nw { background-position: -112px -32px; }\n.ui-icon-arrow-2-n-s { background-position: -128px -32px; }\n.ui-icon-arrow-2-ne-sw { background-position: -144px -32px; }\n.ui-icon-arrow-2-e-w { background-position: -160px -32px; }\n.ui-icon-arrow-2-se-nw { background-position: -176px -32px; }\n.ui-icon-arrowstop-1-n { background-position: -192px -32px; }\n.ui-icon-arrowstop-1-e { background-position: -208px -32px; }\n.ui-icon-arrowstop-1-s { background-position: -224px -32px; }\n.ui-icon-arrowstop-1-w { background-position: -240px -32px; }\n.ui-icon-arrowthick-1-n { background-position: 1px -48px; }\n.ui-icon-arrowthick-1-ne { background-position: -16px -48px; }\n.ui-icon-arrowthick-1-e { background-position: -32px -48px; }\n.ui-icon-arrowthick-1-se { background-position: -48px -48px; }\n.ui-icon-arrowthick-1-s { background-position: -64px -48px; }\n.ui-icon-arrowthick-1-sw { background-position: -80px -48px; }\n.ui-icon-arrowthick-1-w { background-position: -96px -48px; }\n.ui-icon-arrowthick-1-nw { background-position: -112px -48px; }\n.ui-icon-arrowthick-2-n-s { background-position: -128px -48px; }\n.ui-icon-arrowthick-2-ne-sw { background-position: -144px -48px; }\n.ui-icon-arrowthick-2-e-w { background-position: -160px -48px; }\n.ui-icon-arrowthick-2-se-nw { background-position: -176px -48px; }\n.ui-icon-arrowthickstop-1-n { background-position: -192px -48px; }\n.ui-icon-arrowthickstop-1-e { background-position: -208px -48px; }\n.ui-icon-arrowthickstop-1-s { background-position: -224px -48px; }\n.ui-icon-arrowthickstop-1-w { background-position: -240px -48px; }\n.ui-icon-arrowreturnthick-1-w { background-position: 0 -64px; }\n.ui-icon-arrowreturnthick-1-n { background-position: -16px -64px; }\n.ui-icon-arrowreturnthick-1-e { background-position: -32px -64px; }\n.ui-icon-arrowreturnthick-1-s { background-position: -48px -64px; }\n.ui-icon-arrowreturn-1-w { background-position: -64px -64px; }\n.ui-icon-arrowreturn-1-n { background-position: -80px -64px; }\n.ui-icon-arrowreturn-1-e { background-position: -96px -64px; }\n.ui-icon-arrowreturn-1-s { background-position: -112px -64px; }\n.ui-icon-arrowrefresh-1-w { background-position: -128px -64px; }\n.ui-icon-arrowrefresh-1-n { background-position: -144px -64px; }\n.ui-icon-arrowrefresh-1-e { background-position: -160px -64px; }\n.ui-icon-arrowrefresh-1-s { background-position: -176px -64px; }\n.ui-icon-arrow-4 { background-position: 0 -80px; }\n.ui-icon-arrow-4-diag { background-position: -16px -80px; }\n.ui-icon-extlink { background-position: -32px -80px; }\n.ui-icon-newwin { background-position: -48px -80px; }\n.ui-icon-refresh { background-position: -64px -80px; }\n.ui-icon-shuffle { background-position: -80px -80px; }\n.ui-icon-transfer-e-w { background-position: -96px -80px; }\n.ui-icon-transferthick-e-w { background-position: -112px -80px; }\n.ui-icon-folder-collapsed { background-position: 0 -96px; }\n.ui-icon-folder-open { background-position: -16px -96px; }\n.ui-icon-document { background-position: -32px -96px; }\n.ui-icon-document-b { background-position: -48px -96px; }\n.ui-icon-note { background-position: -64px -96px; }\n.ui-icon-mail-closed { background-position: -80px -96px; }\n.ui-icon-mail-open { background-position: -96px -96px; }\n.ui-icon-suitcase { background-position: -112px -96px; }\n.ui-icon-comment { background-position: -128px -96px; }\n.ui-icon-person { background-position: -144px -96px; }\n.ui-icon-print { background-position: -160px -96px; }\n.ui-icon-trash { background-position: -176px -96px; }\n.ui-icon-locked { background-position: -192px -96px; }\n.ui-icon-unlocked { background-position: -208px -96px; }\n.ui-icon-bookmark { background-position: -224px -96px; }\n.ui-icon-tag { background-position: -240px -96px; }\n.ui-icon-home { background-position: 0 -112px; }\n.ui-icon-flag { background-position: -16px -112px; }\n.ui-icon-calendar { background-position: -32px -112px; }\n.ui-icon-cart { background-position: -48px -112px; }\n.ui-icon-pencil { background-position: -64px -112px; }\n.ui-icon-clock { background-position: -80px -112px; }\n.ui-icon-disk { background-position: -96px -112px; }\n.ui-icon-calculator { background-position: -112px -112px; }\n.ui-icon-zoomin { background-position: -128px -112px; }\n.ui-icon-zoomout { background-position: -144px -112px; }\n.ui-icon-search { background-position: -160px -112px; }\n.ui-icon-wrench { background-position: -176px -112px; }\n.ui-icon-gear { background-position: -192px -112px; }\n.ui-icon-heart { background-position: -208px -112px; }\n.ui-icon-star { background-position: -224px -112px; }\n.ui-icon-link { background-position: -240px -112px; }\n.ui-icon-cancel { background-position: 0 -128px; }\n.ui-icon-plus { background-position: -16px -128px; }\n.ui-icon-plusthick { background-position: -32px -128px; }\n.ui-icon-minus { background-position: -48px -128px; }\n.ui-icon-minusthick { background-position: -64px -128px; }\n.ui-icon-close { background-position: -80px -128px; }\n.ui-icon-closethick { background-position: -96px -128px; }\n.ui-icon-key { background-position: -112px -128px; }\n.ui-icon-lightbulb { background-position: -128px -128px; }\n.ui-icon-scissors { background-position: -144px -128px; }\n.ui-icon-clipboard { background-position: -160px -128px; }\n.ui-icon-copy { background-position: -176px -128px; }\n.ui-icon-contact { background-position: -192px -128px; }\n.ui-icon-image { background-position: -208px -128px; }\n.ui-icon-video { background-position: -224px -128px; }\n.ui-icon-script { background-position: -240px -128px; }\n.ui-icon-alert { background-position: 0 -144px; }\n.ui-icon-info { background-position: -16px -144px; }\n.ui-icon-notice { background-position: -32px -144px; }\n.ui-icon-help { background-position: -48px -144px; }\n.ui-icon-check { background-position: -64px -144px; }\n.ui-icon-bullet { background-position: -80px -144px; }\n.ui-icon-radio-on { background-position: -96px -144px; }\n.ui-icon-radio-off { background-position: -112px -144px; }\n.ui-icon-pin-w { background-position: -128px -144px; }\n.ui-icon-pin-s { background-position: -144px -144px; }\n.ui-icon-play { background-position: 0 -160px; }\n.ui-icon-pause { background-position: -16px -160px; }\n.ui-icon-seek-next { background-position: -32px -160px; }\n.ui-icon-seek-prev { background-position: -48px -160px; }\n.ui-icon-seek-end { background-position: -64px -160px; }\n.ui-icon-seek-start { background-position: -80px -160px; }\n/* ui-icon-seek-first is deprecated, use ui-icon-seek-start instead */\n.ui-icon-seek-first { background-position: -80px -160px; }\n.ui-icon-stop { background-position: -96px -160px; }\n.ui-icon-eject { background-position: -112px -160px; }\n.ui-icon-volume-off { background-position: -128px -160px; }\n.ui-icon-volume-on { background-position: -144px -160px; }\n.ui-icon-power { background-position: 0 -176px; }\n.ui-icon-signal-diag { background-position: -16px -176px; }\n.ui-icon-signal { background-position: -32px -176px; }\n.ui-icon-battery-0 { background-position: -48px -176px; }\n.ui-icon-battery-1 { background-position: -64px -176px; }\n.ui-icon-battery-2 { background-position: -80px -176px; }\n.ui-icon-battery-3 { background-position: -96px -176px; }\n.ui-icon-circle-plus { background-position: 0 -192px; }\n.ui-icon-circle-minus { background-position: -16px -192px; }\n.ui-icon-circle-close { background-position: -32px -192px; }\n.ui-icon-circle-triangle-e { background-position: -48px -192px; }\n.ui-icon-circle-triangle-s { background-position: -64px -192px; }\n.ui-icon-circle-triangle-w { background-position: -80px -192px; }\n.ui-icon-circle-triangle-n { background-position: -96px -192px; }\n.ui-icon-circle-arrow-e { background-position: -112px -192px; }\n.ui-icon-circle-arrow-s { background-position: -128px -192px; }\n.ui-icon-circle-arrow-w { background-position: -144px -192px; }\n.ui-icon-circle-arrow-n { background-position: -160px -192px; }\n.ui-icon-circle-zoomin { background-position: -176px -192px; }\n.ui-icon-circle-zoomout { background-position: -192px -192px; }\n.ui-icon-circle-check { background-position: -208px -192px; }\n.ui-icon-circlesmall-plus { background-position: 0 -208px; }\n.ui-icon-circlesmall-minus { background-position: -16px -208px; }\n.ui-icon-circlesmall-close { background-position: -32px -208px; }\n.ui-icon-squaresmall-plus { background-position: -48px -208px; }\n.ui-icon-squaresmall-minus { background-position: -64px -208px; }\n.ui-icon-squaresmall-close { background-position: -80px -208px; }\n.ui-icon-grip-dotted-vertical { background-position: 0 -224px; }\n.ui-icon-grip-dotted-horizontal { background-position: -16px -224px; }\n.ui-icon-grip-solid-vertical { background-position: -32px -224px; }\n.ui-icon-grip-solid-horizontal { background-position: -48px -224px; }\n.ui-icon-gripsmall-diagonal-se { background-position: -64px -224px; }\n.ui-icon-grip-diagonal-se { background-position: -80px -224px; }\n\n\n/* Misc visuals\n----------------------------------*/\n\n/* Corner radius */\n.ui-corner-all,\n.ui-corner-top,\n.ui-corner-left,\n.ui-corner-tl {\n\tborder-top-left-radius: 3px;\n}\n.ui-corner-all,\n.ui-corner-top,\n.ui-corner-right,\n.ui-corner-tr {\n\tborder-top-right-radius: 3px;\n}\n.ui-corner-all,\n.ui-corner-bottom,\n.ui-corner-left,\n.ui-corner-bl {\n\tborder-bottom-left-radius: 3px;\n}\n.ui-corner-all,\n.ui-corner-bottom,\n.ui-corner-right,\n.ui-corner-br {\n\tborder-bottom-right-radius: 3px;\n}\n\n/* Overlays */\n.ui-widget-overlay {\n\tbackground: #aaaaaa;\n\topacity: .3;\n\tfilter: Alpha(Opacity=30); /* support: IE8 */\n}\n.ui-widget-shadow {\n\t-webkit-box-shadow: 0px 0px 5px #666666;\n\tbox-shadow: 0px 0px 5px #666666;\n}\n", ""]);

// exports


/***/ }),

/***/ "./node_modules/css-loader/lib/css-base.js":
/*!*************************************************!*\
  !*** ./node_modules/css-loader/lib/css-base.js ***!
  \*************************************************/
/*! no static exports found */
/***/ (function(module, exports) {

/*
	MIT License http://www.opensource.org/licenses/mit-license.php
	Author Tobias Koppers @sokra
*/
// css base code, injected by the css-loader
module.exports = function(useSourceMap) {
	var list = [];

	// return the list of modules as css string
	list.toString = function toString() {
		return this.map(function (item) {
			var content = cssWithMappingToString(item, useSourceMap);
			if(item[2]) {
				return "@media " + item[2] + "{" + content + "}";
			} else {
				return content;
			}
		}).join("");
	};

	// import a list of modules into the list
	list.i = function(modules, mediaQuery) {
		if(typeof modules === "string")
			modules = [[null, modules, ""]];
		var alreadyImportedModules = {};
		for(var i = 0; i < this.length; i++) {
			var id = this[i][0];
			if(typeof id === "number")
				alreadyImportedModules[id] = true;
		}
		for(i = 0; i < modules.length; i++) {
			var item = modules[i];
			// skip already imported module
			// this implementation is not 100% perfect for weird media query combinations
			//  when a module is imported multiple times with different media queries.
			//  I hope this will never occur (Hey this way we have smaller bundles)
			if(typeof item[0] !== "number" || !alreadyImportedModules[item[0]]) {
				if(mediaQuery && !item[2]) {
					item[2] = mediaQuery;
				} else if(mediaQuery) {
					item[2] = "(" + item[2] + ") and (" + mediaQuery + ")";
				}
				list.push(item);
			}
		}
	};
	return list;
};

function cssWithMappingToString(item, useSourceMap) {
	var content = item[1] || '';
	var cssMapping = item[3];
	if (!cssMapping) {
		return content;
	}

	if (useSourceMap && typeof btoa === 'function') {
		var sourceMapping = toComment(cssMapping);
		var sourceURLs = cssMapping.sources.map(function (source) {
			return '/*# sourceURL=' + cssMapping.sourceRoot + source + ' */'
		});

		return [content].concat(sourceURLs).concat([sourceMapping]).join('\n');
	}

	return [content].join('\n');
}

// Adapted from convert-source-map (MIT)
function toComment(sourceMap) {
	// eslint-disable-next-line no-undef
	var base64 = btoa(unescape(encodeURIComponent(JSON.stringify(sourceMap))));
	var data = 'sourceMappingURL=data:application/json;charset=utf-8;base64,' + base64;

	return '/*# ' + data + ' */';
}


/***/ }),

/***/ "./node_modules/css-loader/lib/url/escape.js":
/*!***************************************************!*\
  !*** ./node_modules/css-loader/lib/url/escape.js ***!
  \***************************************************/
/*! no static exports found */
/***/ (function(module, exports) {

module.exports = function escape(url) {
    if (typeof url !== 'string') {
        return url
    }
    // If url is already wrapped in quotes, remove them
    if (/^['"].*['"]$/.test(url)) {
        url = url.slice(1, -1);
    }
    // Should url be wrapped?
    // See https://drafts.csswg.org/css-values-3/#urls
    if (/["'() \t\n]/.test(url)) {
        return '"' + url.replace(/"/g, '\\"').replace(/\n/g, '\\n') + '"'
    }

    return url
}


/***/ }),

/***/ "./node_modules/style-loader/addStyles.js":
/*!************************************************!*\
  !*** ./node_modules/style-loader/addStyles.js ***!
  \************************************************/
/*! no static exports found */
/***/ (function(module, exports) {

/*
	MIT License http://www.opensource.org/licenses/mit-license.php
	Author Tobias Koppers @sokra
*/
var stylesInDom = {},
	memoize = function(fn) {
		var memo;
		return function () {
			if (typeof memo === "undefined") memo = fn.apply(this, arguments);
			return memo;
		};
	},
	isOldIE = memoize(function() {
		return /msie [6-9]\b/.test(self.navigator.userAgent.toLowerCase());
	}),
	getHeadElement = memoize(function () {
		return document.head || document.getElementsByTagName("head")[0];
	}),
	singletonElement = null,
	singletonCounter = 0,
	styleElementsInsertedAtTop = [];

module.exports = function(list, options) {
	if(typeof DEBUG !== "undefined" && DEBUG) {
		if(typeof document !== "object") throw new Error("The style-loader cannot be used in a non-browser environment");
	}

	options = options || {};
	// Force single-tag solution on IE6-9, which has a hard limit on the # of <style>
	// tags it will allow on a page
	if (typeof options.singleton === "undefined") options.singleton = isOldIE();

	// By default, add <style> tags to the bottom of <head>.
	if (typeof options.insertAt === "undefined") options.insertAt = "bottom";

	var styles = listToStyles(list);
	addStylesToDom(styles, options);

	return function update(newList) {
		var mayRemove = [];
		for(var i = 0; i < styles.length; i++) {
			var item = styles[i];
			var domStyle = stylesInDom[item.id];
			domStyle.refs--;
			mayRemove.push(domStyle);
		}
		if(newList) {
			var newStyles = listToStyles(newList);
			addStylesToDom(newStyles, options);
		}
		for(var i = 0; i < mayRemove.length; i++) {
			var domStyle = mayRemove[i];
			if(domStyle.refs === 0) {
				for(var j = 0; j < domStyle.parts.length; j++)
					domStyle.parts[j]();
				delete stylesInDom[domStyle.id];
			}
		}
	};
}

function addStylesToDom(styles, options) {
	for(var i = 0; i < styles.length; i++) {
		var item = styles[i];
		var domStyle = stylesInDom[item.id];
		if(domStyle) {
			domStyle.refs++;
			for(var j = 0; j < domStyle.parts.length; j++) {
				domStyle.parts[j](item.parts[j]);
			}
			for(; j < item.parts.length; j++) {
				domStyle.parts.push(addStyle(item.parts[j], options));
			}
		} else {
			var parts = [];
			for(var j = 0; j < item.parts.length; j++) {
				parts.push(addStyle(item.parts[j], options));
			}
			stylesInDom[item.id] = {id: item.id, refs: 1, parts: parts};
		}
	}
}

function listToStyles(list) {
	var styles = [];
	var newStyles = {};
	for(var i = 0; i < list.length; i++) {
		var item = list[i];
		var id = item[0];
		var css = item[1];
		var media = item[2];
		var sourceMap = item[3];
		var part = {css: css, media: media, sourceMap: sourceMap};
		if(!newStyles[id])
			styles.push(newStyles[id] = {id: id, parts: [part]});
		else
			newStyles[id].parts.push(part);
	}
	return styles;
}

function insertStyleElement(options, styleElement) {
	var head = getHeadElement();
	var lastStyleElementInsertedAtTop = styleElementsInsertedAtTop[styleElementsInsertedAtTop.length - 1];
	if (options.insertAt === "top") {
		if(!lastStyleElementInsertedAtTop) {
			head.insertBefore(styleElement, head.firstChild);
		} else if(lastStyleElementInsertedAtTop.nextSibling) {
			head.insertBefore(styleElement, lastStyleElementInsertedAtTop.nextSibling);
		} else {
			head.appendChild(styleElement);
		}
		styleElementsInsertedAtTop.push(styleElement);
	} else if (options.insertAt === "bottom") {
		head.appendChild(styleElement);
	} else {
		throw new Error("Invalid value for parameter 'insertAt'. Must be 'top' or 'bottom'.");
	}
}

function removeStyleElement(styleElement) {
	styleElement.parentNode.removeChild(styleElement);
	var idx = styleElementsInsertedAtTop.indexOf(styleElement);
	if(idx >= 0) {
		styleElementsInsertedAtTop.splice(idx, 1);
	}
}

function createStyleElement(options) {
	var styleElement = document.createElement("style");
	styleElement.type = "text/css";
	insertStyleElement(options, styleElement);
	return styleElement;
}

function createLinkElement(options) {
	var linkElement = document.createElement("link");
	linkElement.rel = "stylesheet";
	insertStyleElement(options, linkElement);
	return linkElement;
}

function addStyle(obj, options) {
	var styleElement, update, remove;

	if (options.singleton) {
		var styleIndex = singletonCounter++;
		styleElement = singletonElement || (singletonElement = createStyleElement(options));
		update = applyToSingletonTag.bind(null, styleElement, styleIndex, false);
		remove = applyToSingletonTag.bind(null, styleElement, styleIndex, true);
	} else if(obj.sourceMap &&
		typeof URL === "function" &&
		typeof URL.createObjectURL === "function" &&
		typeof URL.revokeObjectURL === "function" &&
		typeof Blob === "function" &&
		typeof btoa === "function") {
		styleElement = createLinkElement(options);
		update = updateLink.bind(null, styleElement);
		remove = function() {
			removeStyleElement(styleElement);
			if(styleElement.href)
				URL.revokeObjectURL(styleElement.href);
		};
	} else {
		styleElement = createStyleElement(options);
		update = applyToTag.bind(null, styleElement);
		remove = function() {
			removeStyleElement(styleElement);
		};
	}

	update(obj);

	return function updateStyle(newObj) {
		if(newObj) {
			if(newObj.css === obj.css && newObj.media === obj.media && newObj.sourceMap === obj.sourceMap)
				return;
			update(obj = newObj);
		} else {
			remove();
		}
	};
}

var replaceText = (function () {
	var textStore = [];

	return function (index, replacement) {
		textStore[index] = replacement;
		return textStore.filter(Boolean).join('\n');
	};
})();

function applyToSingletonTag(styleElement, index, remove, obj) {
	var css = remove ? "" : obj.css;

	if (styleElement.styleSheet) {
		styleElement.styleSheet.cssText = replaceText(index, css);
	} else {
		var cssNode = document.createTextNode(css);
		var childNodes = styleElement.childNodes;
		if (childNodes[index]) styleElement.removeChild(childNodes[index]);
		if (childNodes.length) {
			styleElement.insertBefore(cssNode, childNodes[index]);
		} else {
			styleElement.appendChild(cssNode);
		}
	}
}

function applyToTag(styleElement, obj) {
	var css = obj.css;
	var media = obj.media;

	if(media) {
		styleElement.setAttribute("media", media)
	}

	if(styleElement.styleSheet) {
		styleElement.styleSheet.cssText = css;
	} else {
		while(styleElement.firstChild) {
			styleElement.removeChild(styleElement.firstChild);
		}
		styleElement.appendChild(document.createTextNode(css));
	}
}

function updateLink(linkElement, obj) {
	var css = obj.css;
	var sourceMap = obj.sourceMap;

	if(sourceMap) {
		// http://stackoverflow.com/a/26603875
		css += "\n/*# sourceMappingURL=data:application/json;base64," + btoa(unescape(encodeURIComponent(JSON.stringify(sourceMap)))) + " */";
	}

	var blob = new Blob([css], { type: "text/css" });

	var oldSrc = linkElement.href;

	linkElement.href = URL.createObjectURL(blob);

	if(oldSrc)
		URL.revokeObjectURL(oldSrc);
}


/***/ }),

/***/ "./src/form/FormBuilder.js":
/*!*********************************!*\
  !*** ./src/form/FormBuilder.js ***!
  \*********************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return FormBuilder; });
/* harmony import */ var _css_iconfont_css__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./css/iconfont.css */ "./src/form/css/iconfont.css");
/* harmony import */ var _css_iconfont_css__WEBPACK_IMPORTED_MODULE_0___default = /*#__PURE__*/__webpack_require__.n(_css_iconfont_css__WEBPACK_IMPORTED_MODULE_0__);
/* harmony import */ var _css_form_css__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./css/form.css */ "./src/form/css/form.css");
/* harmony import */ var _css_form_css__WEBPACK_IMPORTED_MODULE_1___default = /*#__PURE__*/__webpack_require__.n(_css_form_css__WEBPACK_IMPORTED_MODULE_1__);
/* harmony import */ var _external_jquery_ui_css__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./external/jquery-ui.css */ "./src/form/external/jquery-ui.css");
/* harmony import */ var _external_jquery_ui_css__WEBPACK_IMPORTED_MODULE_2___default = /*#__PURE__*/__webpack_require__.n(_external_jquery_ui_css__WEBPACK_IMPORTED_MODULE_2__);
/* harmony import */ var _external_bootstrap_datetimepicker_css__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ./external/bootstrap-datetimepicker.css */ "./src/form/external/bootstrap-datetimepicker.css");
/* harmony import */ var _external_bootstrap_datetimepicker_css__WEBPACK_IMPORTED_MODULE_3___default = /*#__PURE__*/__webpack_require__.n(_external_bootstrap_datetimepicker_css__WEBPACK_IMPORTED_MODULE_3__);
/* harmony import */ var _node_modules_bootstrap_dist_js_bootstrap_js__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! ../../node_modules/bootstrap/dist/js/bootstrap.js */ "./node_modules/bootstrap/dist/js/bootstrap.js");
/* harmony import */ var _node_modules_bootstrap_dist_js_bootstrap_js__WEBPACK_IMPORTED_MODULE_4___default = /*#__PURE__*/__webpack_require__.n(_node_modules_bootstrap_dist_js_bootstrap_js__WEBPACK_IMPORTED_MODULE_4__);
/* harmony import */ var _Utils_js__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! ./Utils.js */ "./src/form/Utils.js");
/* harmony import */ var _container_CanvasContainer_js__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! ./container/CanvasContainer.js */ "./src/form/container/CanvasContainer.js");
/* harmony import */ var _Toolbar_js__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(/*! ./Toolbar.js */ "./src/form/Toolbar.js");
/* harmony import */ var _Palette_js__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(/*! ./Palette.js */ "./src/form/Palette.js");
/* harmony import */ var _property_PageProperty_js__WEBPACK_IMPORTED_MODULE_9__ = __webpack_require__(/*! ./property/PageProperty.js */ "./src/form/property/PageProperty.js");
/* harmony import */ var _component_Component_js__WEBPACK_IMPORTED_MODULE_10__ = __webpack_require__(/*! ./component/Component.js */ "./src/form/component/Component.js");
/**
 * Created by Jacky.Gao on 2017-10-12.
 */












class FormBuilder {
    constructor(container) {
        window.formBuilder = this;
        this.container = container;
        this.formPosition = "up";
        this.toolbar = new _Toolbar_js__WEBPACK_IMPORTED_MODULE_7__["default"]();
        this.container.append(this.toolbar.toolbar);

        var palette = new _Palette_js__WEBPACK_IMPORTED_MODULE_8__["default"]();
        this.propertyPalette = palette.propertyPalette;
        this.components = palette.components;
        this.pageProperty = new _property_PageProperty_js__WEBPACK_IMPORTED_MODULE_9__["default"]();
        this.propertyPalette.append(this.pageProperty.propertyContainer);
        this.pageProperty.propertyContainer.show();

        this.container.append(palette.tabControl);
        this.containers = [];
        this.instances = [];
        this.initRootContainer();
    }
    initRootContainer() {
        const body = $("<div style='width:auto;margin-left:300px;margin-right:10px'>");
        this.container.append(body);
        const shadowContainer = $("<div class='pb-shadow'>");
        body.append(shadowContainer);
        const container = $("<div class='container pb-canvas-container form-horizontal' style='width: auto;padding: 0;'>");
        shadowContainer.append(container);
        const row = $("<div class='row'>");
        const canvas = $("<div class='col-md-12 pb-dropable-grid' style='min-height: 100px;border: none;padding: 0;;'>");
        row.append(canvas);
        container.append(row);
        this.rootContainer = new _container_CanvasContainer_js__WEBPACK_IMPORTED_MODULE_6__["default"](canvas);
        this.containers.push(this.rootContainer);
        _Utils_js__WEBPACK_IMPORTED_MODULE_5__["default"].attachSortable(canvas);
    }
    initData(reportDef) {
        this.reportDef = reportDef;
        reportDef._formBuilder = this;
        let datasources = reportDef.datasources;
        if (!datasources) {
            datasources = [];
        }
        let params = [];
        let datasetMap = new Map();
        for (let ds of datasources) {
            const datasets = ds.datasets || [];
            for (let dataset of datasets) {
                const parameters = dataset.parameters || [];
                params = params.concat(parameters);
                datasetMap.set(dataset.name, dataset.fields);
            }
        }
        this.reportParameters = params;
        this.datasetMap = datasetMap;
        const form = reportDef.searchForm || {};
        if (form) {
            this.formPosition = form.formPosition;
            const components = form.components;
            this.buildPageElements(components, this.rootContainer);
        }
        this.pageProperty.refreshValue();
    }

    buildData() {
        this.reportDef.searchFormXml = this.toXml();
        this.reportDef.searchForm = this.toJson();
    }

    buildPageElements(elements, parentContainer) {
        if (!elements || elements.length === 0) {
            return;
        }
        for (var i = 0; i < elements.length; i++) {
            var element = elements[i];
            var type = element.type;
            var targetComponent;
            $.each(this.components, function (index, c) {
                if (c.component.support(type)) {
                    targetComponent = c.component;
                    return false;
                }
            });
            if (!targetComponent) {
                throw "Unknow component : " + type + "";
            }
            _Utils_js__WEBPACK_IMPORTED_MODULE_5__["default"].attachComponent(targetComponent, parentContainer, element);
        }
    }
    getInstance(id) {
        let target;
        $.each(this.instances, function (index, item) {
            if (item.id === id) {
                target = item.instance;
                return false;
            }
        });
        return target;
    }
    toJson() {
        const json = { formPosition: this.formPosition };
        json.components = this.rootContainer.toJson();
        return json;
    }
    toXml() {
        let xml = `<search-form form-position="${this.formPosition || 'up'}">`;
        xml += this.rootContainer.toXml();
        xml += '</search-form>';
        return xml;
    }
    getContainer(containerId) {
        var targetContainer;
        $.each(this.containers, function (index, container) {
            if (container.id === containerId) {
                targetContainer = container;
                return false;
            }
        });
        return targetContainer;
    }
    selectElement(instance) {
        var children = this.propertyPalette.children();
        children.each(function (i, item) {
            $(item).hide();
        });
        if (!instance) {
            this.select = null;
            this.pageProperty.refreshValue();
            this.pageProperty.propertyContainer.show();
            return;
        }
        if (this.select) {
            var sameInstance = false;
            if (this.select.prop("id") === instance.prop("id")) {
                sameInstance = true;
            }
            this.select.removeClass("pb-hasFocus");
            this.select = null;
            if (sameInstance) {
                this.pageProperty.refreshValue();
                this.pageProperty.propertyContainer.show();
                return;
            }
        }
        if (!this.select) {
            this.select = instance;
            this.select.addClass("pb-hasFocus");
        } else {
            this.select.removeClass("pb-hasFocus");
            if (this.select != instance) {
                this.select = instance;
                this.select.addClass("pb-hasFocus");
            }
        }
        var instanceId = instance.prop("id");
        $.each(this.instances, function (index, item) {
            if (item.id === instanceId) {
                var instance = item.instance;
                var property = item.property;
                if (!property) {
                    return false;
                }
                property.refreshValue(instance);
                property.propertyContainer.show();
                return false;
            }
        });
    }
    addInstance(newInstance, newElement, component) {
        this.instances.push({
            id: newElement.prop("id"),
            instance: newInstance,
            property: component.property
        });
    }
    getComponent(item) {
        var componentId = item.attr(_component_Component_js__WEBPACK_IMPORTED_MODULE_10__["default"].ID);
        var target = null;
        $(this.components).each(function (i, item) {
            var id = item.id;
            if (id === componentId) {
                target = item.component;
                return false;
            }
        });
        return target;
    }
}

/***/ }),

/***/ "./src/form/Palette.js":
/*!*****************************!*\
  !*** ./src/form/Palette.js ***!
  \*****************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Palette; });
/* harmony import */ var _component_Grid2X2Component_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./component/Grid2X2Component.js */ "./src/form/component/Grid2X2Component.js");
/* harmony import */ var _component_GridSingleComponent_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./component/GridSingleComponent.js */ "./src/form/component/GridSingleComponent.js");
/* harmony import */ var _component_Grid3x3x3Component_js__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./component/Grid3x3x3Component.js */ "./src/form/component/Grid3x3x3Component.js");
/* harmony import */ var _component_Grid4x4x4x4Component_js__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ./component/Grid4x4x4x4Component.js */ "./src/form/component/Grid4x4x4x4Component.js");
/* harmony import */ var _component_GridCustomComponent_js__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! ./component/GridCustomComponent.js */ "./src/form/component/GridCustomComponent.js");
/* harmony import */ var _component_TextComponent_js__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! ./component/TextComponent.js */ "./src/form/component/TextComponent.js");
/* harmony import */ var _component_RadioComponent_js__WEBPACK_IMPORTED_MODULE_6__ = __webpack_require__(/*! ./component/RadioComponent.js */ "./src/form/component/RadioComponent.js");
/* harmony import */ var _component_CheckboxComponent_js__WEBPACK_IMPORTED_MODULE_7__ = __webpack_require__(/*! ./component/CheckboxComponent.js */ "./src/form/component/CheckboxComponent.js");
/* harmony import */ var _component_SelectComponent_js__WEBPACK_IMPORTED_MODULE_8__ = __webpack_require__(/*! ./component/SelectComponent.js */ "./src/form/component/SelectComponent.js");
/* harmony import */ var _component_SubmitButtonComponent_js__WEBPACK_IMPORTED_MODULE_9__ = __webpack_require__(/*! ./component/SubmitButtonComponent.js */ "./src/form/component/SubmitButtonComponent.js");
/* harmony import */ var _component_ResetButtonComponent_js__WEBPACK_IMPORTED_MODULE_10__ = __webpack_require__(/*! ./component/ResetButtonComponent.js */ "./src/form/component/ResetButtonComponent.js");
/* harmony import */ var _component_DatetimeComponent_js__WEBPACK_IMPORTED_MODULE_11__ = __webpack_require__(/*! ./component/DatetimeComponent.js */ "./src/form/component/DatetimeComponent.js");
/**
 * Created by Jacky.Gao on 2017-10-12.
 */













class Palette {
    constructor() {
        this.components = [];
        this.initContainer();
        this.initComponents();
    }
    initComponents() {
        this.addComponent(new _component_GridSingleComponent_js__WEBPACK_IMPORTED_MODULE_1__["default"]({
            icon: "form form-1col",
            label: "一列布局"
        }));
        this.addComponent(new _component_Grid2X2Component_js__WEBPACK_IMPORTED_MODULE_0__["default"]({
            icon: "form form-2col",
            label: "两列布局"
        }));
        this.addComponent(new _component_Grid3x3x3Component_js__WEBPACK_IMPORTED_MODULE_2__["default"]({
            icon: "form form-3col",
            label: "三列布局"
        }));
        this.addComponent(new _component_Grid4x4x4x4Component_js__WEBPACK_IMPORTED_MODULE_3__["default"]({
            icon: "form form-4col",
            label: "四列布局"
        }));
        this.addComponent(new _component_GridCustomComponent_js__WEBPACK_IMPORTED_MODULE_4__["default"]({
            icon: "form form-custom-col",
            label: "自定义列布局"
        }));
        this.addComponent(new _component_TextComponent_js__WEBPACK_IMPORTED_MODULE_5__["default"]({
            icon: "form form-textbox",
            label: "文本框"
        }));
        this.addComponent(new _component_DatetimeComponent_js__WEBPACK_IMPORTED_MODULE_11__["default"]({
            icon: "glyphicon glyphicon-calendar",
            label: "日期选择框"
        }));
        this.addComponent(new _component_RadioComponent_js__WEBPACK_IMPORTED_MODULE_6__["default"]({
            icon: "form form-radio",
            label: "单选框"
        }));
        this.addComponent(new _component_CheckboxComponent_js__WEBPACK_IMPORTED_MODULE_7__["default"]({
            icon: "form form-checkbox",
            label: "复选框"
        }));
        this.addComponent(new _component_SelectComponent_js__WEBPACK_IMPORTED_MODULE_8__["default"]({
            icon: "form form-dropdown",
            label: "单选列表"
        }));
        this.addComponent(new _component_SubmitButtonComponent_js__WEBPACK_IMPORTED_MODULE_9__["default"]({
            icon: "form form-submit",
            label: "提交按钮"
        }));
        this.addComponent(new _component_ResetButtonComponent_js__WEBPACK_IMPORTED_MODULE_10__["default"]({
            icon: "form form-reset",
            label: "重置按钮"
        }));
    }
    initContainer() {
        this.tabControl = $("<div class='pb-palette'>");
        var ul = $("<ul class='nav nav-tabs' style='margin: 15px;'>");
        var componentLi = $("<li class='active'><a href='#" + Palette.componentId + "' data-toggle='tab'>组件</a>");
        ul.append(componentLi);
        var propertyLi = $("<li><a href='#" + Palette.propertyId + "' data-toggle='tab'>属性</a></li>");
        ul.append(propertyLi);
        this.tabControl.append(ul);
        var tabContent = $("<div class='tab-content'>");
        this.componentPalette = $("<div class=\"tab-pane fade in active container\" id=\"" + Palette.componentId + "\" style=\"width: 100%\">");
        this.propertyPalette = $("<div class=\"tab-pane fade container\" id=\"" + Palette.propertyId + "\" style=\"width:auto\">");
        tabContent.append(this.componentPalette);
        tabContent.append(this.propertyPalette);
        this.tabControl.append(tabContent);
    }
    addComponent(component) {
        if (this.row) {
            var col = $("<div class=\"col-sm-6\">");
            col.append(component.tool);
            this.row.append(col);
            this.row = null;
        } else {
            this.row = $("<div class=\"row\">");
            var col = $("<div class=\"col-sm-6\">");
            col.append(component.tool);
            this.row.append(col);
            this.componentPalette.append(this.row);
        }
        var componentId = component.id;
        this.components.push({
            "id": componentId,
            "component": component
        });
        if (component.property) {
            this.propertyPalette.append(component.property.propertyContainer);
            component.property.propertyContainer.hide();
        }
    }
}
Palette.componentId = "pb_component_container_palette";
Palette.propertyId = "pb_component_property_palette";

/***/ }),

/***/ "./src/form/Toolbar.js":
/*!*****************************!*\
  !*** ./src/form/Toolbar.js ***!
  \*****************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Toolbar; });
/* harmony import */ var _Utils_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Utils.js */ "./src/form/Utils.js");
/* harmony import */ var _instance_Instance_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./instance/Instance.js */ "./src/form/instance/Instance.js");
/**
 * Created by Jacky.Gao on 2017-10-12.
 */



class Toolbar {
    constructor() {
        this.toolbar = $("<nav class=\"navbar navbar-default pb-toolbar\" style='background: #ffffff;min-height:40px' role=\"navigation\">");
        var ul = $("<ul class=\"nav navbar-nav\">");
        this.toolbar.append(ul);

        this.tip = $("<div class='alert alert-success alert-dismissable'  style='position: absolute;top:50px;width:100%;z-index: 100'> <button type='button' class='close' data-dismiss='alert' aria-hidden='true'> &times; </button> 保存成功!  </div>");
        this.toolbar.append(this.tip);
        this.tip.hide();

        //ul.append(this.buildSave());
        ul.append(this.buildRemove());
    }
    buildSave() {
        this.save = $("<i class='glyphicon glyphicon-floppy-save' style='color:#2196F3;font-size: 22px;margin: 10px;' title='保存'></i>");
        return this.save;
    }
    buildRemove() {
        this.remove = $("<button type='button' style='margin: 5px' class='btn btn-default btn-small'><i style='color: red' class='glyphicon glyphicon-remove'></i> 删除选中的元素</button>");
        var self = this;
        this.remove.click(function () {
            self.deleteElement();
        });
        $(document).keydown(function (e) {
            if (e.which === 46 && e.target && e.target === document.body) {
                self.deleteElement();
            }
        });
        return this.remove;
    }
    deleteElement() {
        var select = formBuilder.select;
        if (!select) {
            bootbox.alert("请先选择一个组件.");
            return;
        }
        var parent = select.parent();
        var parentContainer = formBuilder.getContainer(parent.prop("id"));
        parentContainer.removeChild(select);
        var id = select.prop("id");
        var pos = -1,
            targetIns = null;
        $.each(formBuilder.instances, function (i, item) {
            if (item.instance.id === id) {
                pos = i;
                targetIns = item.instance;
                return false;
            }
        });
        if (pos > -1) {
            formBuilder.instances.splice(pos, 1);
        } else {
            bootbox.alert('删除元素未注册,不能被删除.');
            return;
        }
        _Utils_js__WEBPACK_IMPORTED_MODULE_0__["default"].removeContainerInstanceChildren(targetIns);
        select.remove();
        formBuilder.selectElement();
    }
}

/***/ }),

/***/ "./src/form/Utils.js":
/*!***************************!*\
  !*** ./src/form/Utils.js ***!
  \***************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Utils; });
/* harmony import */ var _component_Component_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./component/Component.js */ "./src/form/component/Component.js");
/* harmony import */ var _instance_TabControlInstance_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./instance/TabControlInstance.js */ "./src/form/instance/TabControlInstance.js");
/* harmony import */ var _instance_ContainerInstance_js__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./instance/ContainerInstance.js */ "./src/form/instance/ContainerInstance.js");
/**
 * Created by Jacky.Gao on 2017-10-12.
 */



class Utils {
    static seq(id) {
        var seqValue;
        $.each(Utils.SEQUENCE, function (name, value) {
            if (name === id) {
                value++;
                seqValue = value;
                Utils.SEQUENCE[id] = seqValue;
                return false;
            }
        });
        if (!seqValue) {
            seqValue = 1;
            Utils.SEQUENCE[id] = seqValue;
        }
        return seqValue;
    }
    static attachSortable(target) {
        target.sortable({
            tolerance: "pointer",
            delay: 200,
            dropOnEmpty: true,
            forcePlaceholderSize: true,
            forceHelperSize: true,
            placeholder: "pb-sortable-placeholder",
            connectWith: ".pb-dropable-grid,.pb-tab-grid,.panel-body,.pb-carousel-container",
            start: function (e, ui) {
                ui.item.css("display", "block");
            },
            receive: function (e, ui) {
                Utils.add = true;
            },
            remove: function (e, ui) {
                var item = ui.item;
                var parent = $(this);
                var parentContainer = formBuilder.getContainer(parent.prop("id"));
                parentContainer.removeChild(item);
            },
            stop: function (e, ui) {
                var item = ui.item;
                var parent = item.parent();
                var parentContainer = formBuilder.getContainer(parent.prop("id"));
                if (!parentContainer) {
                    return;
                }
                if (item.hasClass("pb-component")) {
                    //new component
                    var targetComponent = formBuilder.getComponent(item);
                    var newElement = Utils.attachComponent(targetComponent, parentContainer);
                    item.replaceWith(newElement);
                    item = newElement;
                }
                if (Utils.add) {
                    var targetInstance = formBuilder.getInstance(item.prop("id"));
                    parentContainer.addChild(targetInstance);
                    Utils.add = false;
                }
                var newOrder = parent.sortable("toArray");
                if (newOrder.length > 1) {
                    parentContainer.newOrder(newOrder);
                }
            }
        });
    }
    static attachComponent(targetComponent, parentContainer, initJson) {
        var newInstance;
        if (initJson) {
            newInstance = targetComponent.newInstance(initJson.cols);
            newInstance.initFromJson(initJson);
        } else {
            newInstance = targetComponent.newInstance();
        }
        parentContainer.addChild(newInstance);
        if (newInstance instanceof _instance_ContainerInstance_js__WEBPACK_IMPORTED_MODULE_2__["default"]) {
            $.each(newInstance.containers, function (i, container) {
                formBuilder.containers.push(container);
            });
        }
        var newElement = newInstance.element;
        newElement.attr(_component_Component_js__WEBPACK_IMPORTED_MODULE_0__["default"].ID, targetComponent.id);
        formBuilder.addInstance(newInstance, newElement, targetComponent);
        if (initJson) {
            parentContainer.addElement(newElement);
        }
        var childrenContainers;
        if (newElement.hasClass("row")) {
            childrenContainers = newElement.children(".pb-dropable-grid");
        } else if (newElement.hasClass("tabcontainer")) {
            childrenContainers = newElement.find(".pb-tab-grid");
        } else if (newElement.hasClass("panel-group") || newElement.hasClass("panel-default")) {
            childrenContainers = newElement.find(".panel-body");
        } else if (newElement.hasClass("carousel")) {
            childrenContainers = newElement.find(".pb-carousel-container");
        } else if (newElement.hasClass('btn')) {
            childrenContainers = newElement;
        }
        if (childrenContainers) {
            childrenContainers.each(function (index, child) {
                Utils.attachSortable($(child));
            });
        }
        newElement.click(function (event) {
            formBuilder.selectElement($(this));
            event.stopPropagation();
        });
        if (!newElement.hasClass("panel") && !newElement.hasClass("panel-default")) {
            newElement.addClass("pb-element");
        }
        newElement.mouseover(function (e) {
            newElement.addClass("pb-element-hover");
            e.stopPropagation();
        });
        newElement.mouseout(function (e) {
            newElement.removeClass("pb-element-hover");
            e.stopPropagation();
        });
        return newElement;
    }
    static removeContainerInstanceChildren(ins) {
        var childrenInstances = [];
        if (ins instanceof _instance_TabControlInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"]) {
            var tabs = ins.tabs;
            $.each(tabs, function (index, tab) {
                var children = tab.container.children;
                childrenInstances = childrenInstances.concat(children);
            });
        } else if (ins instanceof _instance_ContainerInstance_js__WEBPACK_IMPORTED_MODULE_2__["default"]) {
            $.each(ins.containers, function (index, container) {
                var children = container.children;
                childrenInstances = childrenInstances.concat(children);
            });
        }
        if (childrenInstances.length === 0) return;
        $.each(childrenInstances, function (index, child) {
            var pos = -1,
                id = child.id;
            $.each(formBuilder.instances, function (i, item) {
                if (item.id === id) {
                    pos = i;
                    return false;
                }
            });
            if (pos > -1) {
                formBuilder.instances.splice(pos, 1);
            } else {
                bootbox.alert('删除元素未注册,不能被删除.');
            }
            Utils.removeContainerInstanceChildren(child);
        });
    }
}
Utils.SEQUENCE = {};
Utils.binding = true;
Utils.add = false;

/***/ }),

/***/ "./src/form/component/CheckboxComponent.js":
/*!*************************************************!*\
  !*** ./src/form/component/CheckboxComponent.js ***!
  \*************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return CheckboxComponent; });
/* harmony import */ var _Component_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Component.js */ "./src/form/component/Component.js");
/* harmony import */ var _instance_CheckboxInstance_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../instance/CheckboxInstance.js */ "./src/form/instance/CheckboxInstance.js");
/* harmony import */ var _property_CheckboxProperty_js__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../property/CheckboxProperty.js */ "./src/form/property/CheckboxProperty.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */



class CheckboxComponent extends _Component_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(options) {
        super(options);
        this.property = new _property_CheckboxProperty_js__WEBPACK_IMPORTED_MODULE_2__["default"]();
    }
    newInstance() {
        return new _instance_CheckboxInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"]();
    }
    getType() {
        return _instance_CheckboxInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"].TYPE;
    }
    getId() {
        this.id = "checkbox_component";
        return this.id;
    }
}

/***/ }),

/***/ "./src/form/component/Component.js":
/*!*****************************************!*\
  !*** ./src/form/component/Component.js ***!
  \*****************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Component; });
/**
 * Created by Jacky.Gao on 2017-10-12.
 */

class Component {
    constructor(options) {
        this.options = options;
        this.entityList = [];
        this.tool = $("<div><i class='" + options.icon + "' style='margin-right:5px'>" + options.label + "</div>");
        this.tool.addClass("pb-component");
        this.tool.attr(Component.ID, this.getId());
        this.tool.draggable({
            revert: false,
            connectToSortable: ".pb-dropable-grid",
            helper: "clone"
        });
    }
    support(type) {
        if (type === this.getType()) {
            return true;
        }
        return false;
    }
    getId() {
        return '';
    }
}
Component.ID = "component_id";
Component.GRID = "component_grid";

/***/ }),

/***/ "./src/form/component/DatetimeComponent.js":
/*!*************************************************!*\
  !*** ./src/form/component/DatetimeComponent.js ***!
  \*************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return DatetimeComponent; });
/* harmony import */ var _Component_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Component.js */ "./src/form/component/Component.js");
/* harmony import */ var _property_DatetimeProperty_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../property/DatetimeProperty.js */ "./src/form/property/DatetimeProperty.js");
/* harmony import */ var _instance_DatetimeInstance_js__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../instance/DatetimeInstance.js */ "./src/form/instance/DatetimeInstance.js");
/**
 * Created by Jacky.Gao on 2017-10-23.
 */



class DatetimeComponent extends _Component_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(options) {
        super(options);
        this.property = new _property_DatetimeProperty_js__WEBPACK_IMPORTED_MODULE_1__["default"]();
    }
    newInstance() {
        return new _instance_DatetimeInstance_js__WEBPACK_IMPORTED_MODULE_2__["default"]();
    }
    getType() {
        return _instance_DatetimeInstance_js__WEBPACK_IMPORTED_MODULE_2__["default"].TYPE;
    }
    getId() {
        this.id = "datetime_component";
        return this.id;
    }
}

/***/ }),

/***/ "./src/form/component/Grid2X2Component.js":
/*!************************************************!*\
  !*** ./src/form/component/Grid2X2Component.js ***!
  \************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Grid2X2Component; });
/* harmony import */ var _GridComponent_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./GridComponent.js */ "./src/form/component/GridComponent.js");
/* harmony import */ var _instance_Grid2X2Instance_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../instance/Grid2X2Instance.js */ "./src/form/instance/Grid2X2Instance.js");
/**
 * Created by Jacky.Gao on 2017-10-15.
 */


class Grid2X2Component extends _GridComponent_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(options) {
        super(options);
    }
    getId() {
        this.id = "component_grid2x2";
        return this.id;
    }
    newInstance() {
        return new _instance_Grid2X2Instance_js__WEBPACK_IMPORTED_MODULE_1__["default"]();
    }
    getType() {
        return _instance_Grid2X2Instance_js__WEBPACK_IMPORTED_MODULE_1__["default"].TYPE;
    }
}

/***/ }),

/***/ "./src/form/component/Grid3x3x3Component.js":
/*!**************************************************!*\
  !*** ./src/form/component/Grid3x3x3Component.js ***!
  \**************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Grid3x3x3Component; });
/* harmony import */ var _GridComponent_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./GridComponent.js */ "./src/form/component/GridComponent.js");
/* harmony import */ var _instance_Grid3x3x3Instance_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../instance/Grid3x3x3Instance.js */ "./src/form/instance/Grid3x3x3Instance.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */


class Grid3x3x3Component extends _GridComponent_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(options) {
        super(options);
    }
    newInstance() {
        return new _instance_Grid3x3x3Instance_js__WEBPACK_IMPORTED_MODULE_1__["default"]();
    }
    getType() {
        return _instance_Grid3x3x3Instance_js__WEBPACK_IMPORTED_MODULE_1__["default"].TYPE;
    }
    getId() {
        this.id = "component_grid3x3x3";
        return this.id;
    }
}

/***/ }),

/***/ "./src/form/component/Grid4x4x4x4Component.js":
/*!****************************************************!*\
  !*** ./src/form/component/Grid4x4x4x4Component.js ***!
  \****************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Grid4x4x4x4Component; });
/* harmony import */ var _GridComponent_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./GridComponent.js */ "./src/form/component/GridComponent.js");
/* harmony import */ var _instance_Grid4x4x4x4Instance_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../instance/Grid4x4x4x4Instance.js */ "./src/form/instance/Grid4x4x4x4Instance.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */


class Grid4x4x4x4Component extends _GridComponent_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(options) {
        super(options);
    }
    newInstance() {
        return new _instance_Grid4x4x4x4Instance_js__WEBPACK_IMPORTED_MODULE_1__["default"]();
    }
    getType() {
        return _instance_Grid4x4x4x4Instance_js__WEBPACK_IMPORTED_MODULE_1__["default"].TYPE;
    }
    getId() {
        this.id = "component_grid4x4x4x4";
        return this.id;
    }
}

/***/ }),

/***/ "./src/form/component/GridComponent.js":
/*!*********************************************!*\
  !*** ./src/form/component/GridComponent.js ***!
  \*********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return GridComponent; });
/* harmony import */ var _property_GridProperty_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../property/GridProperty.js */ "./src/form/property/GridProperty.js");
/* harmony import */ var _Component_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./Component.js */ "./src/form/component/Component.js");
/**
 * Created by Jacky.Gao on 2017-10-15.
 */


class GridComponent extends _Component_js__WEBPACK_IMPORTED_MODULE_1__["default"] {
    constructor(options) {
        super(options);
        this.property = GridComponent.property;
    }
}
GridComponent.property = new _property_GridProperty_js__WEBPACK_IMPORTED_MODULE_0__["default"]();

/***/ }),

/***/ "./src/form/component/GridCustomComponent.js":
/*!***************************************************!*\
  !*** ./src/form/component/GridCustomComponent.js ***!
  \***************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return GridCustomComponent; });
/* harmony import */ var _GridComponent_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./GridComponent.js */ "./src/form/component/GridComponent.js");
/* harmony import */ var _instance_GridCustomInstance_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../instance/GridCustomInstance.js */ "./src/form/instance/GridCustomInstance.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */


class GridCustomComponent extends _GridComponent_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(options) {
        super(options);
    }
    newInstance(cols) {
        return new _instance_GridCustomInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"](cols);
    }
    getType() {
        return _instance_GridCustomInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"].TYPE;
    }
    getId() {
        this.id = "component_gridcustom";
        return this.id;
    }
}

/***/ }),

/***/ "./src/form/component/GridSingleComponent.js":
/*!***************************************************!*\
  !*** ./src/form/component/GridSingleComponent.js ***!
  \***************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return GridSingleComponent; });
/* harmony import */ var _GridComponent_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./GridComponent.js */ "./src/form/component/GridComponent.js");
/* harmony import */ var _instance_GridSingleInstance_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../instance/GridSingleInstance.js */ "./src/form/instance/GridSingleInstance.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */


class GridSingleComponent extends _GridComponent_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(options) {
        super(options);
    }
    newInstance() {
        return new _instance_GridSingleInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"]();
    }
    getType() {
        return _instance_GridSingleInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"].TYPE;
    }
    getId() {
        this.id = "component_gridsingle";
        return this.id;
    }
}

/***/ }),

/***/ "./src/form/component/RadioComponent.js":
/*!**********************************************!*\
  !*** ./src/form/component/RadioComponent.js ***!
  \**********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return RadioComponent; });
/* harmony import */ var _Component_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Component.js */ "./src/form/component/Component.js");
/* harmony import */ var _property_RadioProperty_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../property/RadioProperty.js */ "./src/form/property/RadioProperty.js");
/* harmony import */ var _instance_RadioInstance_js__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../instance/RadioInstance.js */ "./src/form/instance/RadioInstance.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */



class RadioComponent extends _Component_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(options) {
        super(options);
        this.property = new _property_RadioProperty_js__WEBPACK_IMPORTED_MODULE_1__["default"]();
    }
    newInstance() {
        return new _instance_RadioInstance_js__WEBPACK_IMPORTED_MODULE_2__["default"]();
    }
    getType() {
        return _instance_RadioInstance_js__WEBPACK_IMPORTED_MODULE_2__["default"].TYPE;
    }
    getId() {
        this.id = "radio_component";
        return this.id;
    }
}

/***/ }),

/***/ "./src/form/component/ResetButtonComponent.js":
/*!****************************************************!*\
  !*** ./src/form/component/ResetButtonComponent.js ***!
  \****************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return ResetButtonComponent; });
/* harmony import */ var _Component_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Component.js */ "./src/form/component/Component.js");
/* harmony import */ var _instance_ResetButtonInstance_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../instance/ResetButtonInstance.js */ "./src/form/instance/ResetButtonInstance.js");
/* harmony import */ var _property_ButtonProperty_js__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../property/ButtonProperty.js */ "./src/form/property/ButtonProperty.js");
/* harmony import */ var _Utils_js__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ../Utils.js */ "./src/form/Utils.js");
/**
 * Created by Jacky.Gao on 2017-10-20.
 */




class ResetButtonComponent extends _Component_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(options) {
        super(options);
        this.property = new _property_ButtonProperty_js__WEBPACK_IMPORTED_MODULE_2__["default"]();
    }
    newInstance() {
        var seq = _Utils_js__WEBPACK_IMPORTED_MODULE_3__["default"].seq(this.id);
        return new _instance_ResetButtonInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"]("重置" + seq);
    }
    getType() {
        return _instance_ResetButtonInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"].TYPE;
    }
    getId() {
        this.id = "reset_button";
        return this.id;
    }
}

/***/ }),

/***/ "./src/form/component/SelectComponent.js":
/*!***********************************************!*\
  !*** ./src/form/component/SelectComponent.js ***!
  \***********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return SelectComponent; });
/* harmony import */ var _Component_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Component.js */ "./src/form/component/Component.js");
/* harmony import */ var _property_SelectProperty_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../property/SelectProperty.js */ "./src/form/property/SelectProperty.js");
/* harmony import */ var _Utils_js__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../Utils.js */ "./src/form/Utils.js");
/* harmony import */ var _instance_SelectInstance_js__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ../instance/SelectInstance.js */ "./src/form/instance/SelectInstance.js");
/**
 * Created by Jacky.Gao on 2017-10-20.
 */




class SelectComponent extends _Component_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(options) {
        super(options);
        this.property = new _property_SelectProperty_js__WEBPACK_IMPORTED_MODULE_1__["default"]();
    }
    newInstance() {
        var seq = _Utils_js__WEBPACK_IMPORTED_MODULE_2__["default"].seq(this.id);
        return new _instance_SelectInstance_js__WEBPACK_IMPORTED_MODULE_3__["default"](seq);
    }
    getType() {
        return _instance_SelectInstance_js__WEBPACK_IMPORTED_MODULE_3__["default"].TYPE;
    }
    getId() {
        this.id = "single_select";
        return this.id;
    }
}

/***/ }),

/***/ "./src/form/component/SubmitButtonComponent.js":
/*!*****************************************************!*\
  !*** ./src/form/component/SubmitButtonComponent.js ***!
  \*****************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return SubmitButtonComponent; });
/* harmony import */ var _Component_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Component.js */ "./src/form/component/Component.js");
/* harmony import */ var _instance_SubmitButtonInstance_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../instance/SubmitButtonInstance.js */ "./src/form/instance/SubmitButtonInstance.js");
/* harmony import */ var _property_ButtonProperty_js__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../property/ButtonProperty.js */ "./src/form/property/ButtonProperty.js");
/* harmony import */ var _Utils_js__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ../Utils.js */ "./src/form/Utils.js");
/**
 * Created by Jacky.Gao on 2017-10-20.
 */




class SubmitButtonComponent extends _Component_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(options) {
        super(options);
        this.property = new _property_ButtonProperty_js__WEBPACK_IMPORTED_MODULE_2__["default"]();
    }
    newInstance() {
        var seq = _Utils_js__WEBPACK_IMPORTED_MODULE_3__["default"].seq(this.id);
        return new _instance_SubmitButtonInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"]("提交" + seq);
    }
    getType() {
        return _instance_SubmitButtonInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"].TYPE;
    }
    getId() {
        this.id = "submit_button";
        return this.id;
    }
}

/***/ }),

/***/ "./src/form/component/TextComponent.js":
/*!*********************************************!*\
  !*** ./src/form/component/TextComponent.js ***!
  \*********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return TextComponent; });
/* harmony import */ var _Component_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Component.js */ "./src/form/component/Component.js");
/* harmony import */ var _instance_TextInstance_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../instance/TextInstance.js */ "./src/form/instance/TextInstance.js");
/* harmony import */ var _property_TextProperty_js__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ../property/TextProperty.js */ "./src/form/property/TextProperty.js");
/* harmony import */ var _Utils_js__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ../Utils.js */ "./src/form/Utils.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */




class TextComponent extends _Component_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(options) {
        super(options);
        this.property = new _property_TextProperty_js__WEBPACK_IMPORTED_MODULE_2__["default"]();
    }
    newInstance() {
        var seq = _Utils_js__WEBPACK_IMPORTED_MODULE_3__["default"].seq(this.id);
        return new _instance_TextInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"]("输入框" + seq);
    }
    getType() {
        return _instance_TextInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"].TYPE;
    }
    getId() {
        this.id = "component_texteditor";
        return this.id;
    }
}

/***/ }),

/***/ "./src/form/container/CanvasContainer.js":
/*!***********************************************!*\
  !*** ./src/form/container/CanvasContainer.js ***!
  \***********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return CanvasContainer; });
/* harmony import */ var _Container_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Container.js */ "./src/form/container/Container.js");
/**
 * Created by Jacky.Gao on 2017-10-12.
 */

class CanvasContainer extends _Container_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(canvas) {
        super();
        this.container = canvas;
        this.container.uniqueId();
        this.id = this.container.prop("id");
    }
    addElement(element) {
        this.container.append(element);
    }
    toJson() {
        var children = [];
        $.each(this.getChildren(), function (index, child) {
            children.push(child.toJson());
        });
        return children;
    }
    toXml() {
        let xml = '';
        $.each(this.getChildren(), function (index, child) {
            xml += child.toXml();
        });
        return xml;
    }
    getType() {
        return "Canvas";
    }
    toHtml() {
        var div = $("<div class='container' style='width: 100%;;'>");
        var row = $("<div class='row'>");
        var col = $("<div class='col-md-12'>");
        row.append(col);
        div.append(row);
        this.buildChildrenHtml(col);
        return div;
    }
}

/***/ }),

/***/ "./src/form/container/ColContainer.js":
/*!********************************************!*\
  !*** ./src/form/container/ColContainer.js ***!
  \********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return ColContainer; });
/* harmony import */ var _Container_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Container.js */ "./src/form/container/Container.js");
/**
 * Created by Jacky.Gao on 2017-10-15.
 */

class ColContainer extends _Container_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(colsize) {
        super();
        this.colsize = colsize;
        this.container = $("<div style='min-height:80px;padding: 1px'>");
        this.container.addClass("col-md-" + colsize + "");
        this.container.addClass("pb-dropable-grid");
    }
    toJson() {
        const json = {
            size: this.colsize,
            children: []
        };
        for (let child of this.getChildren()) {
            json.children.push(child.toJson());
        }
        return json;
    }
    toXml() {
        let xml = `<col size="${this.colsize}">`;
        for (let child of this.getChildren()) {
            xml += child.toXml();
        }
        xml += `</col>`;
        return xml;
    }
    addElement(element) {
        this.container.append(element);
    }
    initFromJson(json) {
        var children = json.children;
        formBuilder.buildPageElements(children, this);
    }
    getType() {
        return "Col";
    }
    toHtml() {
        var col = $("<div class='col-md-" + this.colsize + "'>");
        this.buildChildrenHtml(col);
        return col;
    }
}

/***/ }),

/***/ "./src/form/container/Container.js":
/*!*****************************************!*\
  !*** ./src/form/container/Container.js ***!
  \*****************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Container; });
/**
 * Created by Jacky.Gao on 2017-10-12.
 */
class Container {
    constructor() {
        this.children = [];
        this.orderArray = [];
    }
    buildChildrenHtml(html) {
        var children = this.getChildren();
        $.each(children, function (index, child) {
            html.append(child.toHtml());
        });
        return children;
    }
    getChildren() {
        for (var i = this.orderArray.length - 1; i > -1; i--) {
            var id = this.orderArray[i];
            var target = Container.searchAndRemoveChild(id, this.children);
            if (target) {
                this.children.unshift(target);
            }
        }
        return this.children;
    }
    addChild(child) {
        if ($.inArray(child, this.children) === -1) {
            this.children.push(child);
        }
    }
    getContainer() {
        if (!this.id) {
            this.id = this.container.prop("id");
            if (!this.id) {
                this.container.uniqueId();
                this.id = this.container.prop("id");
            }
        }
        return this.container;
    }
    removeChild(child) {
        var id = child.prop("id");
        if (!id || id === "") return;
        var pos = -1;
        $.each(this.children, function (index, item) {
            if (item.id === id) {
                pos = index;
                return false;
            }
        });
        if (pos > -1) {
            this.children.splice(pos, 1);
        }
    }
    newOrder(orderArray) {
        this.orderArray = orderArray;
    }
    static searchAndRemoveChild(id, children) {
        var target,
            pos = -1;
        $.each(children, function (index, instance) {
            if (instance.id === id) {
                target = instance;
                pos = index;
                return false;
            }
        });
        if (pos != -1) {
            children.splice(pos, 1);
        }
        return target;
    }
}

/***/ }),

/***/ "./src/form/container/TabContainer.js":
/*!********************************************!*\
  !*** ./src/form/container/TabContainer.js ***!
  \********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return TabContainer; });
/* harmony import */ var _Container_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Container.js */ "./src/form/container/Container.js");
/**
 * Created by Jacky.Gao on 2017-10-12.
 */


class TabContainer extends _Container_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(id) {
        super();
        this.id = id;
        this.container = $("<div class='tab-pane fade pb-tab-grid' id='" + this.id + "'>");
    }
    addElement(element) {
        this.container.append(element);
    }
    initFromJson(json) {
        formBuilder.buildPageElements(json, this);
    }
    toJSON() {
        var children = [];
        $.each(this.getChildren(), function (index, child) {
            children.push(child.toJSON());
        });
        return children;
    }
    toHtml() {
        var div = $("<div class='tab-pane fade pb-tab-grid' id='" + this.id + "1'>");
        div.append(this.buildChildrenHtml(div));
        return div;
    }
}

/***/ }),

/***/ "./src/form/css/form.css":
/*!*******************************!*\
  !*** ./src/form/css/form.css ***!
  \*******************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

// style-loader: Adds some css to the DOM by adding a <style> tag

// load the styles
var content = __webpack_require__(/*! !../../../node_modules/css-loader!./form.css */ "./node_modules/css-loader/index.js!./src/form/css/form.css");
if(typeof content === 'string') content = [[module.i, content, '']];
// add the styles to the DOM
var update = __webpack_require__(/*! ../../../node_modules/style-loader/addStyles.js */ "./node_modules/style-loader/addStyles.js")(content, {});
if(content.locals) module.exports = content.locals;
// Hot Module Replacement
if(false) {}

/***/ }),

/***/ "./src/form/css/iconfont.css":
/*!***********************************!*\
  !*** ./src/form/css/iconfont.css ***!
  \***********************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

// style-loader: Adds some css to the DOM by adding a <style> tag

// load the styles
var content = __webpack_require__(/*! !../../../node_modules/css-loader!./iconfont.css */ "./node_modules/css-loader/index.js!./src/form/css/iconfont.css");
if(typeof content === 'string') content = [[module.i, content, '']];
// add the styles to the DOM
var update = __webpack_require__(/*! ../../../node_modules/style-loader/addStyles.js */ "./node_modules/style-loader/addStyles.js")(content, {});
if(content.locals) module.exports = content.locals;
// Hot Module Replacement
if(false) {}

/***/ }),

/***/ "./src/form/css/iconfont.eot":
/*!***********************************!*\
  !*** ./src/form/css/iconfont.eot ***!
  \***********************************/
/*! no static exports found */
/***/ (function(module, exports) {

module.exports = "data:application/vnd.ms-fontobject;base64,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"

/***/ }),

/***/ "./src/form/css/iconfont.ttf":
/*!***********************************!*\
  !*** ./src/form/css/iconfont.ttf ***!
  \***********************************/
/*! no static exports found */
/***/ (function(module, exports) {

module.exports = "data:application/x-font-ttf;base64,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"

/***/ }),

/***/ "./src/form/external/bootstrap-datetimepicker.css":
/*!********************************************************!*\
  !*** ./src/form/external/bootstrap-datetimepicker.css ***!
  \********************************************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

// style-loader: Adds some css to the DOM by adding a <style> tag

// load the styles
var content = __webpack_require__(/*! !../../../node_modules/css-loader!./bootstrap-datetimepicker.css */ "./node_modules/css-loader/index.js!./src/form/external/bootstrap-datetimepicker.css");
if(typeof content === 'string') content = [[module.i, content, '']];
// add the styles to the DOM
var update = __webpack_require__(/*! ../../../node_modules/style-loader/addStyles.js */ "./node_modules/style-loader/addStyles.js")(content, {});
if(content.locals) module.exports = content.locals;
// Hot Module Replacement
if(false) {}

/***/ }),

/***/ "./src/form/external/images/ui-icons_444444_256x240.png":
/*!**************************************************************!*\
  !*** ./src/form/external/images/ui-icons_444444_256x240.png ***!
  \**************************************************************/
/*! no static exports found */
/***/ (function(module, exports) {

module.exports = "data:image/png;base64,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"

/***/ }),

/***/ "./src/form/external/images/ui-icons_555555_256x240.png":
/*!**************************************************************!*\
  !*** ./src/form/external/images/ui-icons_555555_256x240.png ***!
  \**************************************************************/
/*! no static exports found */
/***/ (function(module, exports) {

module.exports = "data:image/png;base64,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"

/***/ }),

/***/ "./src/form/external/images/ui-icons_777620_256x240.png":
/*!**************************************************************!*\
  !*** ./src/form/external/images/ui-icons_777620_256x240.png ***!
  \**************************************************************/
/*! no static exports found */
/***/ (function(module, exports) {

module.exports = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAQAAAADwCAMAAADYSUr5AAABDlBMVEV3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diB3diBVLkeJAAAAWXRSTlMAGRAzBAhQv4KZLyJVcUBmYBoTMswNITwWQkhLIB5aIycxUyyFNIeAw2rIz8Y4RRy8uL58q7WljKqorR+yKf0BnlEk7woGAgOPomKUSqCvbd+cR2M/b3+RaPlAXvEAAAABYktHRACIBR1IAAAACXBIWXMAAABIAAAASABGyWs+AAAPZElEQVR42u1dC2PbthEGyUiq6ZiSXblLE6ex1mTO5iXZq+u6ro3abG26pOkSd13v//+RAXzhcIeHWMoUbeOTLesIEMB9PIB3ACgLERERMQIkkOy6CTvWH0bOQO/mJeDXP8EMqMzDEkIsEBRMAmh7jHSVmuAjAKwC8FRAzi8/DmoS1AI5AQltj5FOryAjgJ7OK2CZkwEZYO23q+BJ5wwKkttfui1z4s20VTAL5k2kF5hbiPcKcwvwNGB4C7CTwproI4CdDcxEPKUTExx+DNiAj0u9C9AuNPxdYOe46Y5QRERERERExIhx6Z7gjv2ghEVrQJ33hJ5BsxsBfsIq8M0HsAkhWfqglFgawAhgGWh2M1xMWAWUAE90qUofMhhi7be32JNsmVFJPKeLwBQglAQMNh3ALVjYbNaI1jaYD0jM0nw9atcWYEXiaXH/+QDeQ3Y6BoRx3e8CERERERERERG7Qz/HP+iaBsvvHXj0LAD4cip0yN27fXw7AGtQoDTwH+HqkWTgWczTwZVmr8DbAEuqv35bCT6CWDorjGnAqwOSCI7EhlFWHjkBXIkb1M/DZQgRwCeAwK9B+HRPFlPBOjeZ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"

/***/ }),

/***/ "./src/form/external/images/ui-icons_777777_256x240.png":
/*!**************************************************************!*\
  !*** ./src/form/external/images/ui-icons_777777_256x240.png ***!
  \**************************************************************/
/*! no static exports found */
/***/ (function(module, exports) {

module.exports = "data:image/png;base64,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"

/***/ }),

/***/ "./src/form/external/images/ui-icons_cc0000_256x240.png":
/*!**************************************************************!*\
  !*** ./src/form/external/images/ui-icons_cc0000_256x240.png ***!
  \**************************************************************/
/*! no static exports found */
/***/ (function(module, exports) {

module.exports = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAQAAAADwCAMAAADYSUr5AAABDlBMVEXMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADMAADP1XLPAAAAWXRSTlMAGRAzBAhQv4KZLyJVcUBmYBoTMswNITwWQkhLIB5aIycxUyyFNIeAw2rIz8Y4RRy8uL58q7WljKqorR+yKf0BnlEk7woGAgOPomKUSqCvbd+cR2M/b3+RaPlAXvEAAAABYktHRACIBR1IAAAACXBIWXMAAABIAAAASABGyWs+AAAPZElEQVR42u1dC2PbthEGyUiq6ZiSXblLE6ex1mTO5iXZq+u6ro3abG26pOkSd13v//+RAXzhcIeHWMoUbeOTLesIEMB9PIB3ACgLERERMQIkkOy6CTvWH0bOQO/mJeDXP8EMqMzDEkIsEBRMAmh7jHSVmuAjAKwC8FRAzi8/DmoS1AI5AQltj5FOryAjgJ7OK2CZkwEZYO23q+BJ5wwKkttfui1z4s20VTAL5k2kF5hbiPcKcwvwNGB4C7CTwproI4CdDcxEPKUTExx+DNiAj0u9C9AuNPxdYOe46Y5QRERERERExIhx6Z7gjv2ghEVrQJ33hJ5BsxsBfsIq8M0HsAkhWfqglFgawAhgGWh2M1xMWAWUAE90qUofMhhi7be32JNsmVFJPKeLwBQglAQMNh3ALVjYbNaI1jaYD0jM0nw9atcWYEXiaXH/+QDeQ3Y6BoRx3e8CERERERERERG7Qz/HP+iaBsvvHXj0LAD4cip0yN27fXw7AGtQoDTwH+HqkWTgWczTwZVmr8DbAEuqv35bCT6CWDorjGnAqwOSCI7EhlFWHjkBXIkb1M/DZQgRwCeAwK9B+HRPFlPBOjeZ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"

/***/ }),

/***/ "./src/form/external/images/ui-icons_ffffff_256x240.png":
/*!**************************************************************!*\
  !*** ./src/form/external/images/ui-icons_ffffff_256x240.png ***!
  \**************************************************************/
/*! no static exports found */
/***/ (function(module, exports) {

module.exports = "data:image/png;base64,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"

/***/ }),

/***/ "./src/form/external/jquery-ui.css":
/*!*****************************************!*\
  !*** ./src/form/external/jquery-ui.css ***!
  \*****************************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

// style-loader: Adds some css to the DOM by adding a <style> tag

// load the styles
var content = __webpack_require__(/*! !../../../node_modules/css-loader!./jquery-ui.css */ "./node_modules/css-loader/index.js!./src/form/external/jquery-ui.css");
if(typeof content === 'string') content = [[module.i, content, '']];
// add the styles to the DOM
var update = __webpack_require__(/*! ../../../node_modules/style-loader/addStyles.js */ "./node_modules/style-loader/addStyles.js")(content, {});
if(content.locals) module.exports = content.locals;
// Hot Module Replacement
if(false) {}

/***/ }),

/***/ "./src/form/index.js":
/*!***************************!*\
  !*** ./src/form/index.js ***!
  \***************************/
/*! no exports provided */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _FormBuilder_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./FormBuilder.js */ "./src/form/FormBuilder.js");
/**
 * Created by Jacky.Gao on 2017-10-24.
 */


$(document).ready(function () {
    (function ($) {
        $.fn.datetimepicker.dates['zh-CN'] = {
            days: ["星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六", "星期日"],
            daysShort: ["周日", "周一", "周二", "周三", "周四", "周五", "周六", "周日"],
            daysMin: ["日", "一", "二", "三", "四", "五", "六", "日"],
            months: ["一月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "十一月", "十二月"],
            monthsShort: ["一月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "十一月", "十二月"],
            today: "今天",
            suffix: [],
            meridiem: ["上午", "下午"]
        };
    })(jQuery);
    const formBuilder = new _FormBuilder_js__WEBPACK_IMPORTED_MODULE_0__["default"]($("#container"));
    formBuilder.initData(window.parent.__current_report_def);
});

/***/ }),

/***/ "./src/form/instance/ButtonInstance.js":
/*!*********************************************!*\
  !*** ./src/form/instance/ButtonInstance.js ***!
  \*********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return ButtonInstance; });
/* harmony import */ var _Instance_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Instance.js */ "./src/form/instance/Instance.js");
/**
 * Created by Jacky.Gao on 2017-10-20.
 */


class ButtonInstance extends _Instance_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(label) {
        super();
        this.element = $('<div></div>');
        this.label = label;
        this.style = "btn-default";
        this.button = $(`<button type='button' class='btn btn-default btn-sm'>${label}</button>`);
        this.element.append(this.button);
        this.element.uniqueId();
        this.id = this.element.prop("id");
        this.editorType = "button";
        this.align = 'left';
    }
    setStyle(style) {
        this.button.removeClass(this.style);
        this.button.addClass(style);
        this.style = style;
    }
    setAlign(align) {
        this.element.css('text-align', align);
        this.align = align;
    }
    setLabel(label) {
        this.label = label;
        this.button.html(label);
    }
    initFromJson(json) {
        this.setLabel(json.label);
        this.setStyle(json.style);
        this.setAlign(json.align);
    }
    toJSON() {}
}

/***/ }),

/***/ "./src/form/instance/Checkbox.js":
/*!***************************************!*\
  !*** ./src/form/instance/Checkbox.js ***!
  \***************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Checkbox; });
/* harmony import */ var _Utils_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../Utils.js */ "./src/form/Utils.js");
/* harmony import */ var _CheckboxInstance_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./CheckboxInstance.js */ "./src/form/instance/CheckboxInstance.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */


class Checkbox {
    constructor(optionsInline) {
        var seq = _Utils_js__WEBPACK_IMPORTED_MODULE_0__["default"].seq(Checkbox.ID);
        this.label = "选项" + seq;
        this.value = this.label;
        this.checkbox = $("<input type='checkbox' value='" + this.value + "'>");
        var inlineClass = _CheckboxInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"].LABEL_POSITION[0];
        if (optionsInline) {
            inlineClass = _CheckboxInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"].LABEL_POSITION[1];
        }
        this.element = $("<span class='" + inlineClass + "'></span>");
        this.element.append(this.checkbox);
        this.labelElement = $("<span style='margin-left: 15px'>" + this.label + "</span>");
        this.element.append(this.labelElement);
    }
    setValue(json) {
        this.label = json.label;
        this.value = json.value;
        this.checkbox.prop("value", json.value);
        this.labelElement.html(json.label);
    }
    initFromJson(json) {
        this.setValue(json);
    }
    toJson() {
        var json = {
            value: this.value,
            label: this.label
        };
        return json;
    }
}
Checkbox.ID = "Checkbox";

/***/ }),

/***/ "./src/form/instance/CheckboxInstance.js":
/*!***********************************************!*\
  !*** ./src/form/instance/CheckboxInstance.js ***!
  \***********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return CheckboxInstance; });
/* harmony import */ var _instance_Instance_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../instance/Instance.js */ "./src/form/instance/Instance.js");
/* harmony import */ var _Utils_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../Utils.js */ "./src/form/Utils.js");
/* harmony import */ var _Checkbox_js__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./Checkbox.js */ "./src/form/instance/Checkbox.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */



class CheckboxInstance extends _instance_Instance_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor() {
        super();
        var seq = _Utils_js__WEBPACK_IMPORTED_MODULE_1__["default"].seq(CheckboxInstance.ID);
        var label = "复选框" + seq;
        this.element = this.newElement(label);
        this.inputElement = $("<div>");
        this.element.append(this.inputElement);
        this.options = [];
        this.optionsInline = false;
        this.element.uniqueId();
        this.id = this.element.prop("id");
        this.addOption();
        this.addOption();
        this.addOption();
    }
    setOptionsInline(optionsInline) {
        if (optionsInline === this.optionsInline) {
            return;
        }
        this.optionsInline = optionsInline;
        $.each(this.options, function (index, checkbox) {
            var element = checkbox.element;
            element.removeClass();
            if (optionsInline) {
                element.addClass(CheckboxInstance.LABEL_POSITION[1]);
                element.find("input").first().css("margin-left", "");
            } else {
                element.addClass(CheckboxInstance.LABEL_POSITION[0]);
                element.find("input").first().css("margin-left", "auto");
            }
        });
    }
    removeOption(option) {
        var targetIndex;
        $.each(this.options, function (index, item) {
            if (item === option) {
                targetIndex = index;
                return false;
            }
        });
        this.options.splice(targetIndex, 1);
        option.element.remove();
    }
    addOption(json) {
        var checkbox = new _Checkbox_js__WEBPACK_IMPORTED_MODULE_2__["default"](this.optionsInline);
        if (json) {
            checkbox.initFromJson(json);
        }
        this.options.push(checkbox);
        this.inputElement.append(checkbox.element);
        if (!this.optionsInline) {
            checkbox.element.find("input").first().css("margin-left", "auto");
        }
        return checkbox;
    }
    initFromJson(json) {
        $.each(this.options, function (index, item) {
            item.element.remove();
        });
        this.options.splice(0, this.options.length);
        super.fromJson(json);
        var options = json.options;
        for (var i = 0; i < options.length; i++) {
            this.addOption(options[i]);
        }
        if (json.optionsInline !== undefined) {
            this.setOptionsInline(json.optionsInline);
        }
    }
    toJson() {
        const json = {
            label: this.label,
            optionsInline: this.optionsInline,
            labelPosition: this.labelPosition,
            bindParameter: this.bindParameter,
            type: CheckboxInstance.TYPE,
            options: []
        };
        for (let option of this.options) {
            json.options.push(option.toJson());
        }
        return json;
    }
    toXml() {
        let xml = `<input-checkbox label="${this.label}" type="${CheckboxInstance.TYPE}" options-inline="${this.optionsInline === undefined ? false : this.optionsInline}" label-position="${this.labelPosition || 'top'}" bind-parameter="${this.bindParameter || ''}">`;
        for (let option of this.options) {
            xml += `<option label="${option.label}" value="${option.value}"></option>`;
        }
        xml += `</input-checkbox>`;
        return xml;
    }
}
CheckboxInstance.TYPE = "Checkbox";
CheckboxInstance.LABEL_POSITION = ["checkbox", "checkbox-inline"];
CheckboxInstance.ID = "check_instance";

/***/ }),

/***/ "./src/form/instance/ContainerInstance.js":
/*!************************************************!*\
  !*** ./src/form/instance/ContainerInstance.js ***!
  \************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return ContainerInstance; });
/* harmony import */ var _Instance_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Instance.js */ "./src/form/instance/Instance.js");
/**
 * Created by Jacky.Gao on 2017-10-12.
 */

class ContainerInstance extends _Instance_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor() {
        super();
        this.containers = [];
        this.visible = "true";
    }
    initFromJson(json) {
        var cols = json.cols;
        for (var i = 0; i < cols.length; i++) {
            var col = cols[i];
            var c = this.containers[i];
            c.initFromJson(col);
        }
        if (json.showBorder) {
            this.showBorder = json.showBorder;
            this.borderWidth = json.borderWidth;
            this.borderColor = json.borderColor;
            this.setBorderWidth(this.borderWidth);
        }
    }
}

/***/ }),

/***/ "./src/form/instance/DatetimeInstance.js":
/*!***********************************************!*\
  !*** ./src/form/instance/DatetimeInstance.js ***!
  \***********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return DatetimeInstance; });
/* harmony import */ var _Instance_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Instance.js */ "./src/form/instance/Instance.js");
/* harmony import */ var _Utils_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../Utils.js */ "./src/form/Utils.js");
/**
 * Created by Jacky.Gao on 2017-10-23.
 */


class DatetimeInstance extends _Instance_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor() {
        super();
        this.isDate = true;
        var seq = _Utils_js__WEBPACK_IMPORTED_MODULE_1__["default"].seq(DatetimeInstance.ID);
        var label = "日期选择" + seq;
        this.element = this.newElement(label);
        this.dateFormat = "yyyy-mm-dd";
        this.inputElement = $("<div>");
        this.element.append(this.inputElement);
        this.datePickerinputGroup = $("<div class='input-group date'>");
        this.inputElement.append(this.datePickerinputGroup);
        var text = $("<input type='text' class='form-control'>");
        this.datePickerinputGroup.append(text);
        var pickerIcon = $("<span class='input-group-addon'><span class='glyphicon glyphicon-calendar'></span></span>");
        this.datePickerinputGroup.append(pickerIcon);
        this.datePickerinputGroup.datetimepicker({
            format: this.dateFormat,
            autoclose: 1,
            startView: 2,
            minView: 2
        });
        this.element.uniqueId();
        this.id = this.element.prop("id");
    }
    setDateFormat(format) {
        if (this.dateFormat === format || format === '' || format === undefined) {
            return;
        }
        this.dateFormat = format;
        this.datePickerinputGroup.datetimepicker('remove');
        const options = {
            format: this.dateFormat,
            autoclose: 1
        };
        if (this.dateFormat === 'yyyy-mm-dd') {
            options.startView = 2;
            options.minView = 2;
        }
        this.datePickerinputGroup.datetimepicker(options);
    }
    initFromJson(json) {
        super.fromJson(json);
        this.setDateFormat(json.format);
        if (json.searchOperator) {
            this.searchOperator = json.searchOperator;
        }
    }
    toJson() {
        return {
            label: this.label,
            labelPosition: this.labelPosition,
            bindParameter: this.bindParameter,
            format: this.dateFormat,
            type: DatetimeInstance.TYPE
        };
    }
    toXml() {
        let xml = `<input-datetime label="${this.label}" type="${DatetimeInstance.TYPE}" label-position="${this.labelPosition || 'top'}" bind-parameter="${this.bindParameter || ''}" format="${this.dateFormat}"></input-datetime>`;
        return xml;
    }
}
DatetimeInstance.TYPE = "Datetime";
DatetimeInstance.ID = "datetime_instance";

/***/ }),

/***/ "./src/form/instance/Grid2X2Instance.js":
/*!**********************************************!*\
  !*** ./src/form/instance/Grid2X2Instance.js ***!
  \**********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Grid2X2Instance; });
/* harmony import */ var _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../container/ColContainer.js */ "./src/form/container/ColContainer.js");
/* harmony import */ var _ContainerInstance_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./ContainerInstance.js */ "./src/form/instance/ContainerInstance.js");
/**
 * Created by Jacky.Gao on 2017-10-15.
 */


class Grid2X2Instance extends _ContainerInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"] {
    constructor() {
        super();
        this.element = $("<div class=\"row\" style=\"margin: 0px;min-width:100px;\">");
        var col1 = new _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_0__["default"](6);
        var col2 = new _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_0__["default"](6);
        this.containers.push(col1, col2);
        this.element.append(col1.getContainer());
        this.element.append(col2.getContainer());
        this.element.uniqueId();
        this.id = this.element.prop("id");
        this.showBorder = false;
        this.borderWidth = 1;
        this.borderColor = "#eee";
    }
    toJson() {
        const json = {
            showBorder: this.showBorder,
            borderWidth: this.borderWidth,
            borderColor: this.borderColor,
            type: Grid2X2Instance.TYPE,
            cols: []
        };
        for (let container of this.containers) {
            json.cols.push(container.toJson());
        }
        return json;
    }
    toXml() {
        let xml = `<grid show-border="${this.showBorder}" type="${Grid2X2Instance.TYPE}" border-width="${this.borderWidth}" border-color="${this.borderColor}">`;
        for (let container of this.containers) {
            xml += container.toXml();
        }
        xml += `</grid>`;
        return xml;
    }
    setBorderWidth(width) {
        var self = this;
        $.each(this.containers, function (index, container) {
            if (width) {
                container.container.css("border", "solid " + width + "px " + self.borderColor + "");
            } else {
                container.container.css("border", "");
            }
        });
        if (width) {
            this.borderWidth = width;
        }
    }
    setBorderColor(color) {
        var self = this;
        $.each(this.containers, function (index, container) {
            container.container.css("border", "solid " + self.borderWidth + "px " + color + "");
        });
        this.borderColor = color;
    }
}
Grid2X2Instance.TYPE = "Grid2X2";

/***/ }),

/***/ "./src/form/instance/Grid3x3x3Instance.js":
/*!************************************************!*\
  !*** ./src/form/instance/Grid3x3x3Instance.js ***!
  \************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Grid3x3x3Instance; });
/* harmony import */ var _ContainerInstance_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./ContainerInstance.js */ "./src/form/instance/ContainerInstance.js");
/* harmony import */ var _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../container/ColContainer.js */ "./src/form/container/ColContainer.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */


class Grid3x3x3Instance extends _ContainerInstance_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor() {
        super();
        this.element = $("<div class=\"row\" style=\"margin: 0px;min-width:100px;\">");
        var col1 = new _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_1__["default"](4);
        var col2 = new _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_1__["default"](4);
        var col3 = new _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_1__["default"](4);
        this.containers.push(col1, col2, col3);
        this.element.append(col1.getContainer());
        this.element.append(col2.getContainer());
        this.element.append(col3.getContainer());
        this.element.uniqueId();
        this.id = this.element.prop("id");
        this.showBorder = false;
        this.borderWidth = 1;
        this.borderColor = "#cccccc";
    }
    toJson() {
        const json = {
            showBorder: this.showBorder,
            borderWidth: this.borderWidth,
            borderColor: this.borderColor,
            type: Grid3x3x3Instance.TYPE,
            cols: []
        };
        for (let container of this.containers) {
            json.cols.push(container.toJson());
        }
        return json;
    }
    toXml() {
        let xml = `<grid show-border="${this.showBorder}" type="${Grid3x3x3Instance.TYPE}" border-width="${this.borderWidth}" border-color="${this.borderColor}">`;
        for (let container of this.containers) {
            xml += container.toXml();
        }
        xml += `</grid>`;
        return xml;
    }
    setBorderWidth() {
        var self = this;
        $.each(this.containers, function (index, container) {
            if (width) {
                container.container.css("border", "solid " + width + "px " + self.borderColor + "");
            } else {
                container.container.css("border", "");
            }
        });
        if (width) {
            this.borderWidth = width;
        }
    }
    setBorderColor(color) {
        var self = this;
        $.each(this.containers, function (index, container) {
            container.container.css("border", "solid " + self.borderWidth + "px " + color + "");
        });
        this.borderColor = color;
    }
}
Grid3x3x3Instance.TYPE = "Grid3x3x3";

/***/ }),

/***/ "./src/form/instance/Grid4x4x4x4Instance.js":
/*!**************************************************!*\
  !*** ./src/form/instance/Grid4x4x4x4Instance.js ***!
  \**************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Grid4x4x4x4Instance; });
/* harmony import */ var _ContainerInstance_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./ContainerInstance.js */ "./src/form/instance/ContainerInstance.js");
/* harmony import */ var _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../container/ColContainer.js */ "./src/form/container/ColContainer.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */


class Grid4x4x4x4Instance extends _ContainerInstance_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor() {
        super();
        this.element = $("<div class=\"row\" style=\"margin: 0px;min-width:100px;\">");
        var col1 = new _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_1__["default"](3);
        var col2 = new _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_1__["default"](3);
        var col3 = new _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_1__["default"](3);
        var col4 = new _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_1__["default"](3);
        this.containers.push(col1, col2, col3, col4);
        this.element.append(col1.getContainer());
        this.element.append(col2.getContainer());
        this.element.append(col3.getContainer());
        this.element.append(col4.getContainer());
        this.element.uniqueId();
        this.id = this.element.prop("id");
        this.showBorder = false;
        this.borderWidth = 1;
        this.borderColor = "#cccccc";
    }
    toJson() {
        const json = {
            showBorder: this.showBorder,
            borderWidth: this.borderWidth,
            borderColor: this.borderColor,
            type: Grid4x4x4x4Instance.TYPE,
            cols: []
        };
        for (let container of this.containers) {
            json.cols.push(container.toJson());
        }
        return json;
    }
    toXml() {
        let xml = `<grid show-border="${this.showBorder}" type="${Grid4x4x4x4Instance.TYPE}" border-width="${this.borderWidth}" border-color="${this.borderColor}">`;
        for (let container of this.containers) {
            xml += container.toXml();
        }
        xml += `</grid>`;
        return xml;
    }
    setBorderWidth(width) {
        var self = this;
        $.each(this.containers, function (index, container) {
            if (width) {
                container.container.css("border", "solid " + width + "px " + self.borderColor + "");
            } else {
                container.container.css("border", "");
            }
        });
        if (width) {
            this.borderWidth = width;
        }
    }
    setBorderColor(color) {
        var self = this;
        $.each(this.containers, function (index, container) {
            container.container.css("border", "solid " + self.borderWidth + "px " + color + "");
        });
        this.borderColor = color;
    }
}
Grid4x4x4x4Instance.TYPE = "Grid4x4x4x4";

/***/ }),

/***/ "./src/form/instance/GridCustomInstance.js":
/*!*************************************************!*\
  !*** ./src/form/instance/GridCustomInstance.js ***!
  \*************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return GridCustomInstance; });
/* harmony import */ var _ContainerInstance_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./ContainerInstance.js */ "./src/form/instance/ContainerInstance.js");
/* harmony import */ var _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../container/ColContainer.js */ "./src/form/container/ColContainer.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */


class GridCustomInstance extends _ContainerInstance_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(colsJson) {
        super();
        this.element = $("<div class=\"row\" style=\"margin: 0px;min-width:100px;\">");
        var value;
        if (!colsJson) {
            while (!value) {
                value = prompt("请输入列信息,列之间用“,”分隔,列数之和为12，如“2,8,2”，表示有三列，比重为2,8,2", "2,8,2");
            }
        } else {
            value = "";
            for (var i = 0; i < colsJson.length; i++) {
                var size = colsJson[i].size;
                if (value.length > 0) {
                    value += ",";
                }
                value += size;
            }
        }
        var cols = value.split(",");
        for (var i = 0; i < cols.length; i++) {
            var colNum = parseInt(cols[i]);
            if (!colNum) {
                colNum = 1;
            }
            var col = new _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_1__["default"](colNum);
            this.containers.push(col);
            this.element.append(col.getContainer());
        }
        this.element.uniqueId();
        this.id = this.element.prop("id");
        this.showBorder = false;
        this.borderWidth = 1;
        this.borderColor = "#cccccc";
    }
    getElement() {
        return this.element;
    }
    toJson() {
        const json = {
            showBorder: this.showBorder,
            borderWidth: this.borderWidth,
            borderColor: this.borderColor,
            type: GridCustomInstance.TYPE,
            cols: []
        };
        for (let container of this.containers) {
            json.cols.push(container.toJson());
        }
        return json;
    }
    toXml() {
        let xml = `<grid show-border="${this.showBorder}" type="${GridCustomInstance.TYPE}" border-width="${this.borderWidth}" border-color="${this.borderColor}">`;
        for (let container of this.containers) {
            xml += container.toXml();
        }
        xml += `</grid>`;
        return xml;
    }
    setBorderWidth(width) {
        var self = this;
        $.each(this.containers, function (index, container) {
            if (width) {
                container.container.css("border", "solid " + width + "px " + self.borderColor + "");
            } else {
                container.container.css("border", "");
            }
        });
        if (width) {
            this.borderWidth = width;
        }
    }
    setBorderColor(color) {
        var self = this;
        $.each(this.containers, function (index, container) {
            container.container.css("border", "solid " + self.borderWidth + "px " + color + "");
        });
        this.borderColor = color;
    }
}
GridCustomInstance.TYPE = "GridCustom";

/***/ }),

/***/ "./src/form/instance/GridSingleInstance.js":
/*!*************************************************!*\
  !*** ./src/form/instance/GridSingleInstance.js ***!
  \*************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return GridSingleInstance; });
/* harmony import */ var _ContainerInstance_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./ContainerInstance.js */ "./src/form/instance/ContainerInstance.js");
/* harmony import */ var _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../container/ColContainer.js */ "./src/form/container/ColContainer.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */


class GridSingleInstance extends _ContainerInstance_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor() {
        super();
        this.element = $("<div class=\"row\" style=\"margin: 0px;min-width:100px;\">");
        this.col1 = new _container_ColContainer_js__WEBPACK_IMPORTED_MODULE_1__["default"](12);
        this.containers.push(this.col1);
        this.element.append(this.col1.getContainer());
        this.element.uniqueId();
        this.id = this.element.prop("id");
        this.showBorder = false;
        this.borderWidth = 1;
        this.borderColor = "#cccccc";
    }
    toJson() {
        const json = {
            showBorder: this.showBorder,
            borderWidth: this.borderWidth,
            borderColor: this.borderColor,
            type: GridSingleInstance.TYPE,
            cols: []
        };
        for (let container of this.containers) {
            json.cols.push(container.toJson());
        }
        return json;
    }
    toXml() {
        let xml = `<grid show-border="${this.showBorder}" type="${GridSingleInstance.TYPE}" border-width="${this.borderWidth}" border-color="${this.borderColor}">`;
        for (let container of this.containers) {
            xml += container.toXml();
        }
        xml += `</grid>`;
        return xml;
    }
    setBorderWidth(width) {
        var self = this;
        $.each(this.containers, function (index, container) {
            container.container.css("border", "solid " + width + "px " + self.borderColor + "");
        });
        this.borderWidth = width;
    }
    setBorderColor(color) {
        var self = this;
        $.each(this.containers, function (index, container) {
            container.container.css("border", "solid " + self.borderWidth + "px " + color + "");
        });
        this.borderColor = color;
    }
}
GridSingleInstance.TYPE = "GridSingle";

/***/ }),

/***/ "./src/form/instance/Instance.js":
/*!***************************************!*\
  !*** ./src/form/instance/Instance.js ***!
  \***************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Instance; });
/* harmony import */ var _Utils_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../Utils.js */ "./src/form/Utils.js");
/**
 * Created by Jacky.Gao on 2017-10-12.
 */


class Instance {
    constructor() {
        this.labelPosition = Instance.TOP;
        this.enable = "true";
        this.visible = "true";
    }
    newElement(label) {
        this.element = $("<div class='form-group row' style='margin:0px'>");
        this.label = label;
        this.labelElement = $("<span class='control-label' style='font-size: 13px'></span>");
        this.element.append(this.labelElement);
        this.labelElement.text(label);
        return this.element;
    }
    setLabel(label) {
        this.label = label;
        if (this.isRequired) {
            this.labelElement.html(this.label + "<span style='color:red'>*</span>");
        } else {
            this.labelElement.html(this.label);
        }
    }
    setLabelPosition(position) {
        if (this.labelPosition === position) {
            return;
        }
        this.labelPosition = position;
        if (position === Instance.TOP) {
            this.labelElement.removeClass(Instance.POS_CLASSES[0]);
            this.inputElement.removeClass(Instance.POS_CLASSES[1]);
        } else if (position === Instance.LEFT) {
            this.labelElement.addClass(Instance.POS_CLASSES[0]);
            this.inputElement.addClass(Instance.POS_CLASSES[1]);
        }
    }
    setBindParameter(bindParameter) {
        this.bindParameter = bindParameter;
    }
    getElementId() {
        if (_Utils_js__WEBPACK_IMPORTED_MODULE_0__["default"].binding) {
            if (!this.bindTableName) {
                this.bindTableName = formBuilder.bindTable.name;
            }
            if (this.bindTableName && this.bindField) {
                return this.bindTableName + "." + this.bindField;
            }
            return null;
        } else {
            return this.label;
        }
    }
    fromJson(json) {
        this.setLabel(json.label);
        this.setLabelPosition(json.labelPosition);
        this.setBindParameter(json.bindParameter);
    }
    initFromJson(json) {}
}
Instance.LEFT = "left";
Instance.TOP = "top";
Instance.POS_CLASSES = ["col-md-3", "col-md-9"];

/***/ }),

/***/ "./src/form/instance/Option.js":
/*!*************************************!*\
  !*** ./src/form/instance/Option.js ***!
  \*************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Option; });
/**
 * Created by Jacky.Gao on 2015/12/4.
 */
class Option {
    constructor(label) {
        this.label = label;
        this.value = label;
        this.element = $("<option value='" + label + "'>" + label + "</option>");
    }
    initFromJson(json) {
        this.setValue(json);
    }
    toJson() {
        return {
            label: this.label,
            value: this.value
        };
    }
    setValue(json) {
        this.value = json.value;
        this.element.prop("value", json.value);
        this.label = json.label;
        this.element.text(json.label);
    }
    remove() {
        this.element.remove();
    }
}

/***/ }),

/***/ "./src/form/instance/Radio.js":
/*!************************************!*\
  !*** ./src/form/instance/Radio.js ***!
  \************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Radio; });
/* harmony import */ var _Utils_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../Utils.js */ "./src/form/Utils.js");
/* harmony import */ var _CheckboxInstance_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./CheckboxInstance.js */ "./src/form/instance/CheckboxInstance.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */


class Radio {
    constructor(optionsInline) {
        var seq = _Utils_js__WEBPACK_IMPORTED_MODULE_0__["default"].seq(Radio.ID);
        this.label = "选项" + seq;
        this.value = this.label;
        this.radio = $("<input type='radio'>");
        var inlineClass = _CheckboxInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"].LABEL_POSITION[0];
        if (optionsInline) {
            inlineClass = _CheckboxInstance_js__WEBPACK_IMPORTED_MODULE_1__["default"].LABEL_POSITION[1];
        }
        this.element = $("<span class='" + inlineClass + "'></span>");
        this.element.append(this.radio);
        this.labelElement = $("<span>" + this.label + "</span>");
        this.element.append(this.labelElement);
    }
    setValue(json) {
        this.label = json.label;
        this.value = json.value;
        this.radio.prop("value", this.value);
        this.labelElement.html(json.label);
    }
    initFromJson(json) {
        this.setValue(json);
    }
    toJson() {
        return { label: this.label, value: this.value };
    }
}
Radio.ID = "Radio";

/***/ }),

/***/ "./src/form/instance/RadioInstance.js":
/*!********************************************!*\
  !*** ./src/form/instance/RadioInstance.js ***!
  \********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return RadioInstance; });
/* harmony import */ var _Instance_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Instance.js */ "./src/form/instance/Instance.js");
/* harmony import */ var _Utils_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../Utils.js */ "./src/form/Utils.js");
/* harmony import */ var _Radio_js__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./Radio.js */ "./src/form/instance/Radio.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */



class RadioInstance extends _Instance_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(seq) {
        super();
        this.seq = _Utils_js__WEBPACK_IMPORTED_MODULE_1__["default"].seq(RadioInstance.ID);
        this.label = "单选框" + this.seq;
        this.element = this.newElement(this.label);
        this.inputElement = $("<div>");
        this.element.append(this.inputElement);
        this.options = [];
        this.element.uniqueId();
        this.id = this.element.prop("id");
        this.optionsInline = false;
        this.addOption();
        this.addOption();
        this.addOption();
    }
    setOptionsInline(optionsInline) {
        if (optionsInline === this.optionsInline) {
            return;
        }
        this.optionsInline = optionsInline;
        $.each(this.options, function (index, radio) {
            var element = radio.element;
            element.removeClass();
            if (optionsInline) {
                element.addClass(RadioInstance.LABEL_POSITION[1]);
                element.css("padding-left", "0px");
            } else {
                element.addClass(RadioInstance.LABEL_POSITION[0]);
            }
        });
    }
    removeOption(option) {
        var targetIndex;
        $.each(this.options, function (index, item) {
            if (item === option) {
                targetIndex = index;
                return false;
            }
        });
        this.options.splice(targetIndex, 1);
        option.element.remove();
    }
    addOption(json) {
        var radio = new _Radio_js__WEBPACK_IMPORTED_MODULE_2__["default"](this.optionsInline);
        if (json) {
            radio.initFromJson(json);
        }
        this.options.push(radio);
        this.inputElement.append(radio.element);
        var input = radio.element.find("input").first();
        if (!this.optionsInline) {
            input.css("margin-left", "auto");
        }
        input.prop("name", "radiooption" + this.seq);
        return radio;
    }
    initFromJson(json) {
        $.each(this.options, function (index, item) {
            item.element.remove();
        });
        this.options.splice(0, this.options.length);
        super.fromJson(json);
        var options = json.options;
        for (var i = 0; i < options.length; i++) {
            this.addOption(options[i]);
        }
        if (json.optionsInline !== undefined) {
            this.setOptionsInline(json.optionsInline);
        }
    }
    toJson() {
        const json = {
            label: this.label,
            optionsInline: this.optionsInline,
            labelPosition: this.labelPosition,
            bindParameter: this.bindParameter,
            type: RadioInstance.TYPE,
            options: []
        };
        for (let option of this.options) {
            json.options.push(option.toJson());
        }
        return json;
    }
    toXml() {
        let xml = `<input-radio label="${this.label}" type="${RadioInstance.TYPE}" options-inline="${this.optionsInline}" label-position="${this.labelPosition || 'top'}" bind-parameter="${this.bindParameter || ''}">`;
        for (let option of this.options) {
            xml += `<option label="${option.label}" value="${option.value}"></option>`;
        }
        xml += `</input-radio>`;
        return xml;
    }
}
RadioInstance.TYPE = "Radio";
RadioInstance.LABEL_POSITION = ["checkbox", "checkbox-inline"];
RadioInstance.ID = "radio_instance";

/***/ }),

/***/ "./src/form/instance/ResetButtonInstance.js":
/*!**************************************************!*\
  !*** ./src/form/instance/ResetButtonInstance.js ***!
  \**************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return ResetButtonInstance; });
/* harmony import */ var _ButtonInstance_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./ButtonInstance.js */ "./src/form/instance/ButtonInstance.js");
/**
 * Created by Jacky.Gao on 2017-10-20.
 */


class ResetButtonInstance extends _ButtonInstance_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(label) {
        super(label);
        this.editorType = "reset-button";
    }
    toJson() {
        return {
            label: this.label,
            style: this.style,
            align: this.align,
            type: ResetButtonInstance.TYPE
        };
    }
    toXml() {
        return `<button-reset label="${this.label}" align="${this.align}" type="${ResetButtonInstance.TYPE}" style="${this.style}"></button-reset>`;
    }
}
ResetButtonInstance.TYPE = 'Reset-button';

/***/ }),

/***/ "./src/form/instance/SelectInstance.js":
/*!*********************************************!*\
  !*** ./src/form/instance/SelectInstance.js ***!
  \*********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return SelectInstance; });
/* harmony import */ var _Instance_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Instance.js */ "./src/form/instance/Instance.js");
/* harmony import */ var _Option_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./Option.js */ "./src/form/instance/Option.js");
/**
 * Created by Jacky.Gao on 2017-10-20.
 */


class SelectInstance extends _Instance_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(seq) {
        super();
        var label = "单选列表" + seq;
        this.element = this.newElement(label);
        this.inputElement = $("<div>");
        this.select = $("<select class='form-control'>");
        this.inputElement.append(this.select);
        this.element.append(this.inputElement);
        this.options = [];
        this.optionNum = 1;
        for (var i = 1; i < 5; i++) {
            this.addOption();
        }
        this.element.uniqueId();
        this.id = this.element.prop("id");
    }
    addOption(json) {
        var option = new _Option_js__WEBPACK_IMPORTED_MODULE_1__["default"]("选项" + this.optionNum++);
        if (json) {
            option.initFromJson(json);
        }
        this.options.push(option);
        this.select.append(option.element);
        return option;
    }
    removeOption(option) {
        var targetIndex;
        $.each(this.options, function (index, item) {
            if (item === option) {
                targetIndex = index;
                return false;
            }
        });
        this.options.splice(targetIndex, 1);
        option.remove();
    }
    initFromJson(json) {
        $.each(this.options, function (index, item) {
            item.element.remove();
        });
        this.options.splice(0, this.options.length);
        super.fromJson(json);
        if (json.searchOperator) {
            this.searchOperator = json.searchOperator;
        }
        var options = json.options;
        for (var i = 0; i < options.length; i++) {
            this.addOption(options[i]);
        }
        this.useDataset = json.useDataset;
        this.dataset = json.dataset;
        this.labelField = json.labelField;
        this.valueField = json.valueField;
    }
    toJson() {
        const json = {
            label: this.label,
            optionsInline: this.optionsInline,
            labelPosition: this.labelPosition,
            bindParameter: this.bindParameter,
            type: SelectInstance.TYPE,
            useDataset: this.useDataset,
            dataset: this.dataset,
            labelField: this.labelField,
            valueField: this.valueField,
            options: []
        };
        for (let option of this.options) {
            json.options.push(option.toJson());
        }
        return json;
    }
    toXml() {
        let xml = `<input-select label="${this.label}" type="${SelectInstance.TYPE}" label-position="${this.labelPosition || 'top'}" bind-parameter="${this.bindParameter || ''}"`;
        if (this.useDataset) {
            xml += ` use-dataset="${this.useDataset}" dataset="${this.dataset}" label-field="${this.labelField}" value-field="${this.valueField}"`;
        }
        xml += '>';
        for (let option of this.options || []) {
            xml += `<option label="${option.label}" value="${option.value}"></option>`;
        }
        xml += `</input-select>`;
        return xml;
    }
}
SelectInstance.TYPE = "Select";

/***/ }),

/***/ "./src/form/instance/SubmitButtonInstance.js":
/*!***************************************************!*\
  !*** ./src/form/instance/SubmitButtonInstance.js ***!
  \***************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return SubmitButtonInstance; });
/* harmony import */ var _ButtonInstance_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./ButtonInstance.js */ "./src/form/instance/ButtonInstance.js");
/**
 * Created by Jacky.Gao on 2017-10-20.
 */


class SubmitButtonInstance extends _ButtonInstance_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(label) {
        super(label);
        this.editorType = "submit-button";
    }
    toJson() {
        return {
            label: this.label,
            style: this.style,
            align: this.align,
            type: SubmitButtonInstance.TYPE
        };
    }
    toXml() {
        return `<button-submit label="${this.label}" align="${this.align}" type="${SubmitButtonInstance.TYPE}" style="${this.style}"></button-submit>`;
    }
}
SubmitButtonInstance.TYPE = "Submit-button";

/***/ }),

/***/ "./src/form/instance/Tab.js":
/*!**********************************!*\
  !*** ./src/form/instance/Tab.js ***!
  \**********************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Tab; });
/* harmony import */ var _container_TabContainer_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../container/TabContainer.js */ "./src/form/container/TabContainer.js");
/**
 * Created by Jacky.Gao on 2017-10-12.
 */


class Tab {
    constructor(seq, tabnum) {
        this.li = $("<li>");
        this.id = "tabContent" + seq + "" + tabnum;
        this.tabName = "页签" + tabnum;
        this.link = $("<a href='#" + this.id + "' data-toggle='tab'>" + this.tabName + "</a>");
        this.link.click(function (e) {
            $(this).tab('show');
            e.stopPropagation();
        });
        this.li.append(this.link);
        this.container = new _container_TabContainer_js__WEBPACK_IMPORTED_MODULE_0__["default"](this.id);
    }
    getTabName() {
        return this.tabName;
    }
    setTabName(tabName) {
        this.tabName = tabName;
        this.link.text(tabName);
    }
    liToHtml() {
        var li = $("<li>");
        li.append($("<a href='#" + this.id + "1' data-toggle='tab'>" + this.tabName + "</a>"));
        return li;
    }
    getTabContent() {
        return this.container.getContainer();
    }
    remove() {
        this.li.remove();
        this.container.getContainer().remove();
    }
    initFromJson(json) {
        this.setTabName(json.tabName);
        this.container.initFromJson(json.container);
    }
    toJSON() {
        return {
            id: this.id,
            tabName: this.tabName,
            type: this.getType(),
            container: this.container.toJSON()
        };
    }
    getType() {
        return "Tab";
    }
}

/***/ }),

/***/ "./src/form/instance/TabControlInstance.js":
/*!*************************************************!*\
  !*** ./src/form/instance/TabControlInstance.js ***!
  \*************************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return TabControlInstance; });
/* harmony import */ var _ContainerInstance_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./ContainerInstance.js */ "./src/form/instance/ContainerInstance.js");
/* harmony import */ var _Tab_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./Tab.js */ "./src/form/instance/Tab.js");
/**
 * Created by Jacky.Gao on 2017-10-12.
 */


class TabControlInstance extends _ContainerInstance_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(seq) {
        super();
        this.seq = seq;
        this.tabs = [];
        this.tabNum = 1;
        this.element = $("<div style='min-height: 100px;' class='tabcontainer'>");
        this.ul = $("<ul class='nav nav-tabs'>");
        this.element.append(this.ul);
        this.tabContent = $("<div class='tab-content'>");
        this.element.append(this.tabContent);
        this.addTab(true);
        this.addTab();
        this.addTab();
        this.element.uniqueId();
        this.id = this.element.prop("id");
        this.visible = "true";
    }
    addTab(active, json) {
        let tabnum = this.tabNum++;
        const tab = new _Tab_js__WEBPACK_IMPORTED_MODULE_1__["default"](this.seq, tabnum);
        if (json) {
            tab.initFromJson(json);
        }
        this.containers.push(tab.container);
        formBuilder.containers.push(tab.container);
        var li = tab.li;
        if (active) {
            li.addClass("active");
        }
        this.ul.append(li);
        var tabContent = tab.getTabContent();
        if (active) {
            tabContent.addClass("in active");
        }
        this.tabContent.append(tabContent);
        this.tabs.push(tab);
        return tab;
    }
    getTab(id) {
        let targetTab = null;
        $.each(this.tabs, function (index, tab) {
            if (tab.getId() === id) {
                targetTab = tab;
                return false;
            }
        });
        return targetTab;
    }
    initFromJson(json) {
        $.each(this.tabs, function (index, tab) {
            tab.remove();
        });
        this.tabs.splice(0, this.tabs.length);
        this.visible = json.visible;
        var tabs = json.tabs;
        for (var i = 0; i < tabs.length; i++) {
            var tab = tabs[i];
            if (i === 0) {
                this.addTab(true, tab);
            } else {
                this.addTab(false, tab);
            }
        }
    }
    toJSON() {
        var json = { id: this.id, type: TabControlInstance.TYPE, visible: this.visible };
        var tabs = [];
        $.each(this.tabs, function (index, tab) {
            tabs.push(tab.toJSON());
        });
        json.tabs = tabs;
        return json;
    }
}
TabControlInstance.TYPE = "TabControl";

/***/ }),

/***/ "./src/form/instance/TextInstance.js":
/*!*******************************************!*\
  !*** ./src/form/instance/TextInstance.js ***!
  \*******************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return TextInstance; });
/* harmony import */ var _Instance_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Instance.js */ "./src/form/instance/Instance.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */

class TextInstance extends _Instance_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(label) {
        super();
        this.element = this.newElement(label);
        this.inputElement = $("<div>");
        this.element.append(this.inputElement);
        this.textInput = $("<input type=\"text\" class=\"form-control\">");
        this.inputElement.append(this.textInput);
        this.element.uniqueId();
        this.id = this.element.prop("id");
        this.editorType = "text";
    }
    initFromJson(json) {
        super.fromJson(json);
        this.editorType = json.editorType;
        if (json.searchOperator) {
            this.searchOperator = json.searchOperator;
        }
    }
    toJson() {
        const json = {
            label: this.label,
            optionsInline: this.optionsInline,
            labelPosition: this.labelPosition,
            bindParameter: this.bindParameter,
            type: TextInstance.TYPE
        };
        return json;
    }
    toXml() {
        const xml = `<input-text label="${this.label}" type="${TextInstance.TYPE}" label-position="${this.labelPosition || 'top'}" bind-parameter="${this.bindParameter || ''}"></input-text>`;
        return xml;
    }
}
TextInstance.TYPE = "Text";

/***/ }),

/***/ "./src/form/property/ButtonProperty.js":
/*!*********************************************!*\
  !*** ./src/form/property/ButtonProperty.js ***!
  \*********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return ButtonProperty; });
/* harmony import */ var _Property_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Property.js */ "./src/form/property/Property.js");
/**
 * Created by Jacky.Gao on 2017-10-20.
 */


class ButtonProperty extends _Property_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor() {
        super();
        const _this = this;
        this.buttonType = $(`<div class="form-group"></div>`);
        this.col.append(this.buttonType);
        const labelGroup = $(`<div class="form-group"><label>按钮标题</label></div>`);
        this.col.append(labelGroup);
        this.labelEditor = $(`<input type="text" class="form-control">`);
        this.labelEditor.change(function () {
            _this.current.setLabel($(this).val());
        });
        labelGroup.append(this.labelEditor);

        const selectGroup = $("<div class=\"form-group\"><label>按钮风格</label></div>");
        this.col.append(selectGroup);
        this.typeSelect = $("<select class='form-control'>");
        selectGroup.append(this.typeSelect);
        this.typeSelect.append("<option value='btn-default'>默认</option>");
        this.typeSelect.append("<option value='btn-primary'>基本</option>");
        this.typeSelect.append("<option value='btn-success'>成功</option>");
        this.typeSelect.append("<option value='btn-info'>信息</option>");
        this.typeSelect.append("<option value='btn-warning'>警告</option>");
        this.typeSelect.append("<option value='btn-danger'>危险</option>");
        this.typeSelect.append("<option value='btn-link'>链接</option>");
        this.typeSelect.change(function () {
            const style = $(this).children("option:selected").val();
            _this.current.setStyle(style);
        });

        const alignGroup = $(`<div class="form-group"><label>对齐方式</label></div>`);
        this.col.append(alignGroup);
        this.alignSelect = $(`<select class="form-control">
            <option value="left">左对齐</option>
            <option value="right">右对齐</option>
        </select>`);
        alignGroup.append(this.alignSelect);
        this.alignSelect.change(function () {
            _this.current.setAlign($(this).val());
        });
    }
    refreshValue(current) {
        this.current = current;
        this.labelEditor.val(current.label);
        this.typeSelect.val(current.style);
        if (current.editorType === 'reset-button') {
            this.buttonType.html("重置按钮");
        } else {
            this.buttonType.html("提交按钮");
        }
    }
}

/***/ }),

/***/ "./src/form/property/CheckboxProperty.js":
/*!***********************************************!*\
  !*** ./src/form/property/CheckboxProperty.js ***!
  \***********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return CheckboxProperty; });
/* harmony import */ var _Property_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Property.js */ "./src/form/property/Property.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */

class CheckboxProperty extends _Property_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor() {
        super();
        this.init();
    }
    init() {
        this.col.append(this.buildBindParameter());
        this.positionLabelGroup = this.buildPositionLabelGroup();
        this.col.append(this.positionLabelGroup);
        this.col.append(this.buildLabelGroup());
        this.col.append(this.buildOptionsInlineGroup());
        this.optionFormGroup = $("<div class='form-group'>");
        this.col.append(this.optionFormGroup);
    }
    addCheckboxEditor(checkbox) {
        var self = this;
        var inputGroup = $("<div class='input-group'>");
        var text = $("<input type='text' class='form-control'>");
        inputGroup.append(text);
        text.change(function () {
            var value = $(this).val();
            var json = { value: value, label: value };
            var array = value.split(",");
            if (array.length == 2) {
                json.label = array[0];
                json.value = array[1];
            }
            checkbox.setValue(json);
        });
        if (checkbox.label === checkbox.value) {
            text.val(checkbox.label);
        } else {
            text.val(checkbox.label + "," + checkbox.value);
        }
        var addon = $("<span class='input-group-addon'>");
        inputGroup.append(addon);
        var del = $("<span class='pb-icon-delete'><li class='glyphicon glyphicon-trash'></li></span>");
        del.click(function () {
            if (self.current.options.length === 1) {
                bootbox.alert("至少要保留一个选项!");
                return;
            }
            self.current.removeOption(checkbox);
            inputGroup.remove();
        });
        addon.append(del);
        var add = $("<span class='pb-icon-add' style='margin-left: 10px'><li class='glyphicon glyphicon-plus'></span>");
        add.click(function () {
            var newOption = self.current.addOption();
            self.addCheckboxEditor(newOption);
        });
        addon.append(add);
        this.optionFormGroup.append(inputGroup);
    }
    refreshValue(current) {
        super.refreshValue(current);
        this.optionFormGroup.empty();
        this.optionFormGroup.append($("<label>选项(若显示值与实际值不同，则用“,”分隔，如“是,true”等)</label>"));
        var self = this;
        $.each(this.current.options, function (index, checkbox) {
            self.addCheckboxEditor(checkbox);
        });
    }
}

/***/ }),

/***/ "./src/form/property/DatetimeProperty.js":
/*!***********************************************!*\
  !*** ./src/form/property/DatetimeProperty.js ***!
  \***********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return DatetimeProperty; });
/* harmony import */ var _Property_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Property.js */ "./src/form/property/Property.js");
/**
 * Created by Jacky.Gao on 2017-10-23.
 */

class DatetimeProperty extends _Property_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor() {
        super();
        this.init();
    }
    init() {
        this.positionLabelGroup = this.buildPositionLabelGroup();
        this.col.append(this.positionLabelGroup);
        this.col.append(this.buildBindParameter());
        this.col.append(this.buildLabelGroup());
        var formatGroup = $("<div class='form-group'><label class='control-label'>日期格式</label></div>");
        this.col.append(formatGroup);
        this.formatSelect = $("<select class='form-control'>");
        this.formatSelect.append($("<option>yyyy-mm-dd</option>"));
        this.formatSelect.append($("<option>yyyy-mm-dd hh:ii:ss</option>"));
        var self = this;
        this.formatSelect.change(function () {
            self.current.setDateFormat($(this).val());
        });
        formatGroup.append(this.formatSelect);
    }
    refreshValue(current) {
        super.refreshValue(current);
        this.formatSelect.val(current.dateFormat);
    }
}

/***/ }),

/***/ "./src/form/property/GridProperty.js":
/*!*******************************************!*\
  !*** ./src/form/property/GridProperty.js ***!
  \*******************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return GridProperty; });
/* harmony import */ var _Property_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Property.js */ "./src/form/property/Property.js");
/**
 * Created by Jacky.Gao on 2017-10-15.
 */

class GridProperty extends _Property_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor() {
        super();
        this.init();
    }
    init() {
        var showBorderGroup = $("<div class='form-group'><label>显示边线</label></div>");
        this.col.append(showBorderGroup);
        var showLineRadioGroup = $("<div class='checkbox-inline'>");
        showBorderGroup.append(showLineRadioGroup);
        var radioName = "show_grid_line_radio_";
        this.showBorderRadio = $("<span style='margin-right: 10px'>是<input type='radio' name='" + radioName + "'></span>");
        showBorderGroup.append(this.showBorderRadio);
        var self = this;
        this.showBorderRadio.change(function () {
            var value = $(this).find("input").prop("checked");
            if (value) {
                self.current.showBorder = true;
                self.borderPropGroup.show();
                self.borderWidthText.val(self.current.borderWidth);
                self.borderColorText.val(self.current.borderColor);
                self.current.setBorderWidth(self.current.borderWidth);
            }
        });

        this.hideBorderRadio = $("<span>否<input type='radio' name='" + radioName + "'></span>");
        showBorderGroup.append(this.hideBorderRadio);
        this.hideBorderRadio.change(function () {
            var value = $(this).find("input").prop("checked");
            if (value) {
                self.current.showBorder = false;
                self.borderPropGroup.hide();
                self.current.setBorderWidth();
            }
        });

        this.borderPropGroup = $("<div>");
        this.col.append(this.borderPropGroup);
        var borderWidthGroup = $("<div class='form-group'><label>边线宽度(单位px)</label></div>");
        this.borderWidthText = $("<input type='number' class='form-control'>");
        borderWidthGroup.append(this.borderWidthText);
        this.borderPropGroup.append(borderWidthGroup);
        this.borderWidthText.change(function () {
            var width = $(this).val();
            self.current.setBorderWidth(width);
        });

        var borderColorGroup = $("<div class='form-group'><label>边线颜色</label></div>");
        this.borderPropGroup.append(borderColorGroup);
        this.borderColorText = $("<input type='color' class='form-control'>");
        borderColorGroup.append(this.borderColorText);
        this.borderColorText.change(function () {
            var color = $(this).val();
            self.current.setBorderColor(color);
        });
        this.borderPropGroup.hide();
    }
    refreshValue(current) {
        this.current = current;
        if (current.showBorder) {
            this.showBorderRadio.find("input").prop("checked", true);
            this.borderPropGroup.show();
            this.borderWidthText.val(current.borderWidth);
            this.borderColorText.val(current.borderColor);
        } else {
            this.hideBorderRadio.find("input").prop("checked", true);
            this.borderPropGroup.hide();
        }
    }
}

/***/ }),

/***/ "./src/form/property/PageProperty.js":
/*!*******************************************!*\
  !*** ./src/form/property/PageProperty.js ***!
  \*******************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return PageProperty; });
/* harmony import */ var _Property_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Property.js */ "./src/form/property/Property.js");
/**
 * Created by Jacky.Gao on 2017-10-12.
 */

class PageProperty extends _Property_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor() {
        super();
        this.init();
    }
    init() {
        var positionGroup = $("<div class='form-group'>");
        positionGroup.append($("<label>查询表单位置</label>"));
        this.positionSelect = $(`<select class='form-control'>
            <option value="up">预览工具栏之上</option>
            <option value="down">预览工具栏之下</option>
        </select>`);
        positionGroup.append(this.positionSelect);
        var self = this;
        this.positionSelect.change(function () {
            window.formBuilder.formPosition = $(this).val();
        });
        this.col.append(positionGroup);
    }
    refreshValue(current) {
        this.positionSelect.val(window.formBuilder.formPosition);
    }
}

/***/ }),

/***/ "./src/form/property/Property.js":
/*!***************************************!*\
  !*** ./src/form/property/Property.js ***!
  \***************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return Property; });
/**
 * Created by Jacky.Gao on 2017-10-12.
 */
class Property {
    constructor() {
        this.propertyContainer = $("<div class='row'>");
        this.col = $("<div class='col-md-12'>");
        this.propertyContainer.append(this.col);
    }
    buildOptionsInlineGroup() {
        const inlineGroup = $("<div class='form-group'><label class='control-label'>选项换行显示</label></div>");
        this.optionsInlineSelect = $("<select class='form-control'>");
        this.optionsInlineSelect.append($("<option value='0'>是</option>"));
        this.optionsInlineSelect.append($("<option value='1'>否</option>"));
        inlineGroup.append(this.optionsInlineSelect);
        const self = this;
        this.optionsInlineSelect.change(function () {
            let value = false;
            if ($(this).val() === "1") {
                value = true;
            }
            self.current.setOptionsInline(value);
        });
        return inlineGroup;
    }
    buildBindParameter() {
        const group = $("<div class='form-group'><label>绑定的查询参数</label></div>");
        this.bindFieldEditor = $("<input type='text' class='form-control'>");
        group.append(this.bindFieldEditor);
        const self = this;
        this.bindFieldEditor.change(function () {
            const value = $(this).val();
            self.current.setBindParameter(value);
        });
        return group;
    }
    buildLabelGroup() {
        const labelGroup = $("<div class='form-group'>");
        const labelLabel = $("<label>标题</label>");
        labelGroup.append(labelLabel);
        this.textLabel = $("<input type='text' class='form-control'>");
        const self = this;
        this.textLabel.change(function () {
            self.current.setLabel($(this).val());
        });
        labelGroup.append(this.textLabel);
        return labelGroup;
    }
    buildPositionLabelGroup() {
        const positionLabelGroup = $("<div class='form-group'>");
        const positionLabel = $("<label class='control-label'>标题位置</label>");
        positionLabelGroup.append(positionLabel);
        this.positionLabelSelect = $("<select class='form-control'>");
        positionLabelGroup.append(this.positionLabelSelect);
        this.positionLabelSelect.append("<option value='top' selected>上边</option>");
        this.positionLabelSelect.append("<option value='left'>左边</option>");
        const self = this;
        this.positionLabelSelect.change(function () {
            self.current.setLabelPosition($(this).val());
        });
        return positionLabelGroup;
    }

    refreshValue(instance) {
        this.current = instance;
        if (this.optionsInlineSelect) {
            if (instance.optionsInline) {
                this.optionsInlineSelect.val("1");
            } else {
                this.optionsInlineSelect.val("0");
            }
        }
        this.positionLabelSelect.val(instance.labelPosition);
        this.textLabel.val(instance.label);
        this.bindFieldEditor.val(instance.bindParameter);
    }
}

/***/ }),

/***/ "./src/form/property/RadioProperty.js":
/*!********************************************!*\
  !*** ./src/form/property/RadioProperty.js ***!
  \********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return RadioProperty; });
/* harmony import */ var _Property_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Property.js */ "./src/form/property/Property.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */

class RadioProperty extends _Property_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor() {
        super();
        this.init();
    }
    init() {
        this.col.append(this.buildBindParameter());
        this.positionLabelGroup = this.buildPositionLabelGroup();
        this.col.append(this.positionLabelGroup);
        this.col.append(this.buildLabelGroup());
        this.col.append(this.buildOptionsInlineGroup());
        this.optionFormGroup = $("<div class='form-group'>");
        this.col.append(this.optionFormGroup);
    }
    addRadioEditor(radio) {
        var self = this;
        var inputGroup = $("<div class='input-group'>");
        var text = $("<input type='text' class='form-control'>");
        inputGroup.append(text);
        text.change(function () {
            var value = $(this).val();
            var json = { value: value, label: value };
            var array = value.split(",");
            if (array.length == 2) {
                json.label = array[0];
                json.value = array[1];
            }
            radio.setValue(json);
        });
        if (radio.label === radio.value) {
            text.val(radio.label);
        } else {
            text.val(radio.label + "," + radio.value);
        }
        var addon = $("<span class='input-group-addon'>");
        inputGroup.append(addon);
        var del = $("<span class='pb-icon-delete'><li class='glyphicon glyphicon-trash'></li></span>");
        del.click(function () {
            if (self.current.options.length === 1) {
                bootbox.alert("至少要保留一个选项!");
                return;
            }
            self.current.removeOption(radio);
            inputGroup.remove();
        });
        addon.append(del);
        var add = $("<span class='pb-icon-add' style='margin-left: 10px'><li class='glyphicon glyphicon-plus'></span>");
        add.click(function () {
            var newOption = self.current.addOption();
            self.addRadioEditor(newOption);
        });
        addon.append(add);
        this.optionFormGroup.append(inputGroup);
    }
    refreshValue(current) {
        super.refreshValue(current);
        this.optionFormGroup.empty();
        this.optionFormGroup.append($("<label>选项(若显示值与实际值不同，则用“,”分隔，如“是,true”等)</label>"));
        var self = this;
        $.each(this.current.options, function (index, checkbox) {
            self.addRadioEditor(checkbox);
        });
    }
}

/***/ }),

/***/ "./src/form/property/SelectProperty.js":
/*!*********************************************!*\
  !*** ./src/form/property/SelectProperty.js ***!
  \*********************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return SelectProperty; });
/* harmony import */ var _Property_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Property.js */ "./src/form/property/Property.js");
/**
 * Created by Jacky.Gao on 2017-10-20.
 */

class SelectProperty extends _Property_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(report) {
        super();
        this.col.append(this.buildBindParameter());
        this.positionLabelGroup = this.buildPositionLabelGroup();
        this.col.append(this.positionLabelGroup);
        this.col.append(this.buildLabelGroup());
        this.optionFormGroup = $("<div class='form-group'>");
        this.col.append(this.optionFormGroup);
    }
    refreshValue(editor) {
        super.refreshValue(editor);
        this.optionFormGroup.empty();
        const group = $(`<div class="form-group"><label>数据来源</label></div>`);
        const datasourceSelect = $(`<select class="form-control">
            <option value="dataset">数据集</option>
            <option value="simple">固定值</option>
        </select>`);
        group.append(datasourceSelect);
        this.optionFormGroup.append(group);
        this.simpleOptionGroup = $(`<div class="form-group"></div>`);
        this.optionFormGroup.append(this.simpleOptionGroup);
        this.datasetGroup = $(`<div class="form-group"></div>`);
        this.optionFormGroup.append(this.datasetGroup);
        const _this = this;
        datasourceSelect.change(function () {
            if ($(this).val() === 'dataset') {
                editor.useDataset = true;
                _this.datasetGroup.show();
                _this.simpleOptionGroup.hide();
            } else {
                editor.useDataset = false;
                _this.datasetGroup.hide();
                _this.simpleOptionGroup.show();
            }
        });
        const datasetGroup = $(`<div class="form-group"><label>数据集</label></div>`);
        this.datasetGroup.append(datasetGroup);
        const datasetSelect = $(`<select class="form-control"></select>`);
        datasetGroup.append(datasetSelect);
        let dsName = null;
        for (let datasetName of formBuilder.datasetMap.keys()) {
            datasetSelect.append(`<option>${datasetName}</option>`);
            dsName = datasetName;
        }
        if (editor.dataset) {
            dsName = editor.dataset;
        } else {
            editor.dataset = dsName;
        }
        datasetSelect.val(dsName);
        let fields = formBuilder.datasetMap.get(dsName);
        if (!fields) fields = [];
        const labelGroup = $(`<div class="form-group"><label>显示值字段名</label></div>`);
        this.datasetGroup.append(labelGroup);
        const labelSelect = $(`<select class="form-control"></select>`);
        labelGroup.append(labelSelect);
        const valueGroup = $(`<div class="form-group"><label>实际值字段名</label></div>`);
        this.datasetGroup.append(valueGroup);
        const valueSelect = $(`<select class="form-control"></select>`);
        labelSelect.change(function () {
            editor.labelField = $(this).val();
        });
        valueSelect.change(function () {
            editor.valueField = $(this).val();
        });
        let targetField = null;
        for (let field of fields) {
            labelSelect.append(`<option>${field.name}</option>`);
            valueSelect.append(`<option>${field.name}</option>`);
            targetField = field.name;
        }
        datasetSelect.change(function () {
            const dsName = $(this).val();
            if (!dsName) {
                return;
            }
            editor.dataset = dsName;
            labelSelect.empty();
            valueSelect.empty();
            fields = formBuilder.datasetMap.get(dsName);
            if (!fields) fields = [];
            for (let field of fields) {
                labelSelect.append(`<option>${field.name}</option>`);
                valueSelect.append(`<option>${field.name}</option>`);
                targetField = field.name;
            }
            editor.labelField = targetField;
            editor.valueField = targetField;
            labelSelect.val(targetField);
            valueSelect.val(targetField);
        });
        if (editor.labelField) {
            targetField = editor.labelField;
        } else {
            editor.labelField = targetField;
        }
        labelSelect.val(targetField);
        if (editor.valueField) {
            targetField = editor.valueField;
        } else {
            editor.valueField = targetField;
        }
        valueSelect.val(targetField);
        valueGroup.append(valueSelect);
        if (editor.useDataset) {
            datasourceSelect.val('dataset');
            this.datasetGroup.show();
            this.simpleOptionGroup.hide();
        } else {
            this.datasetGroup.hide();
            this.simpleOptionGroup.show();
            datasourceSelect.val('simple');
        }
        this.simpleOptionGroup.append($("<label>固定值选项(若显示值与实际值不同，则用“,”分隔，如“是,true”等)</label>"));
        var self = this;
        $.each(editor.options, function (index, option) {
            self.addOptionEditor(option);
        });
    }
    addOptionEditor(option) {
        var inputGroup = $("<div class='input-group'>");
        var input = $("<input class='form-control' type='text'>");

        if (option.label === option.value) {
            input.val(option.label);
        } else {
            input.val(option.label + "," + option.value);
        }

        input.change(function () {
            var value = $(this).val();
            var json = { value: value, label: value };
            var array = value.split(",");
            if (array.length == 2) {
                json.label = array[0];
                json.value = array[1];
            }
            option.setValue(json);
        });
        inputGroup.append(input);
        var addon = $("<span class='input-group-addon'>");
        inputGroup.append(addon);
        var self = this;
        var del = $("<span class='pb-icon-delete'><li class='glyphicon glyphicon-trash'></li></span>");
        del.click(function () {
            if (self.current.options.length === 1) {
                bootbox.alert("至少要保留一个列表选项!");
                return;
            }
            self.current.removeOption(option);
            inputGroup.remove();
        });
        addon.append(del);
        var add = $("<span class='pb-icon-add' style='margin-left: 10px'><li class='glyphicon glyphicon-plus'></span>");
        add.click(function () {
            var newOption = self.current.addOption();
            self.addOptionEditor(newOption);
        });
        addon.append(add);
        this.simpleOptionGroup.append(inputGroup);
    }
}

/***/ }),

/***/ "./src/form/property/TextProperty.js":
/*!*******************************************!*\
  !*** ./src/form/property/TextProperty.js ***!
  \*******************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return TextProperty; });
/* harmony import */ var _Property_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./Property.js */ "./src/form/property/Property.js");
/**
 * Created by Jacky.Gao on 2017-10-16.
 */

class TextProperty extends _Property_js__WEBPACK_IMPORTED_MODULE_0__["default"] {
    constructor(report) {
        super();
        this.init(report);
    }
    init(report) {
        this.col.append(this.buildBindParameter());
        this.positionLabelGroup = this.buildPositionLabelGroup();
        this.col.append(this.positionLabelGroup);
        this.col.append(this.buildLabelGroup());
    }
    refreshValue(current) {
        super.refreshValue(current);
        if (this.typeSelect) {
            this.typeSelect.val(current.editorType);
        }
    }
}

/***/ })

/******/ });
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8vd2VicGFjay9ib290c3RyYXAiLCJ3ZWJwYWNrOi8vLy4vbm9kZV9tb2R1bGVzL2Jvb3RzdHJhcC9kaXN0L2pzL2Jvb3RzdHJhcC5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9jc3MvZm9ybS5jc3MiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vY3NzL2ljb25mb250LmNzcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9leHRlcm5hbC9ib290c3RyYXAtZGF0ZXRpbWVwaWNrZXIuY3NzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL2V4dGVybmFsL2pxdWVyeS11aS5jc3MiLCJ3ZWJwYWNrOi8vLy4vbm9kZV9tb2R1bGVzL2Nzcy1sb2FkZXIvbGliL2Nzcy1iYXNlLmpzIiwid2VicGFjazovLy8uL25vZGVfbW9kdWxlcy9jc3MtbG9hZGVyL2xpYi91cmwvZXNjYXBlLmpzIiwid2VicGFjazovLy8uL25vZGVfbW9kdWxlcy9zdHlsZS1sb2FkZXIvYWRkU3R5bGVzLmpzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL0Zvcm1CdWlsZGVyLmpzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL1BhbGV0dGUuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vVG9vbGJhci5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9VdGlscy5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9jb21wb25lbnQvQ2hlY2tib3hDb21wb25lbnQuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vY29tcG9uZW50L0NvbXBvbmVudC5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9jb21wb25lbnQvRGF0ZXRpbWVDb21wb25lbnQuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vY29tcG9uZW50L0dyaWQyWDJDb21wb25lbnQuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vY29tcG9uZW50L0dyaWQzeDN4M0NvbXBvbmVudC5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9jb21wb25lbnQvR3JpZDR4NHg0eDRDb21wb25lbnQuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vY29tcG9uZW50L0dyaWRDb21wb25lbnQuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vY29tcG9uZW50L0dyaWRDdXN0b21Db21wb25lbnQuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vY29tcG9uZW50L0dyaWRTaW5nbGVDb21wb25lbnQuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vY29tcG9uZW50L1JhZGlvQ29tcG9uZW50LmpzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL2NvbXBvbmVudC9SZXNldEJ1dHRvbkNvbXBvbmVudC5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9jb21wb25lbnQvU2VsZWN0Q29tcG9uZW50LmpzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL2NvbXBvbmVudC9TdWJtaXRCdXR0b25Db21wb25lbnQuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vY29tcG9uZW50L1RleHRDb21wb25lbnQuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vY29udGFpbmVyL0NhbnZhc0NvbnRhaW5lci5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9jb250YWluZXIvQ29sQ29udGFpbmVyLmpzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL2NvbnRhaW5lci9Db250YWluZXIuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vY29udGFpbmVyL1RhYkNvbnRhaW5lci5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9jc3MvZm9ybS5jc3M/ZTY3MCIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9jc3MvaWNvbmZvbnQuY3NzPzZjYjEiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vY3NzL2ljb25mb250LmVvdCIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9jc3MvaWNvbmZvbnQudHRmIiwid2VicGFjazovLy8uL3NyYy9mb3JtL2V4dGVybmFsL2Jvb3RzdHJhcC1kYXRldGltZXBpY2tlci5jc3M/NTM4MyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9leHRlcm5hbC9pbWFnZXMvdWktaWNvbnNfNDQ0NDQ0XzI1NngyNDAucG5nIiwid2VicGFjazovLy8uL3NyYy9mb3JtL2V4dGVybmFsL2ltYWdlcy91aS1pY29uc181NTU1NTVfMjU2eDI0MC5wbmciLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vZXh0ZXJuYWwvaW1hZ2VzL3VpLWljb25zXzc3NzYyMF8yNTZ4MjQwLnBuZyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9leHRlcm5hbC9pbWFnZXMvdWktaWNvbnNfNzc3Nzc3XzI1NngyNDAucG5nIiwid2VicGFjazovLy8uL3NyYy9mb3JtL2V4dGVybmFsL2ltYWdlcy91aS1pY29uc19jYzAwMDBfMjU2eDI0MC5wbmciLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vZXh0ZXJuYWwvaW1hZ2VzL3VpLWljb25zX2ZmZmZmZl8yNTZ4MjQwLnBuZyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9leHRlcm5hbC9qcXVlcnktdWkuY3NzP2YyZWIiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vaW5kZXguanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vaW5zdGFuY2UvQnV0dG9uSW5zdGFuY2UuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vaW5zdGFuY2UvQ2hlY2tib3guanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vaW5zdGFuY2UvQ2hlY2tib3hJbnN0YW5jZS5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9pbnN0YW5jZS9Db250YWluZXJJbnN0YW5jZS5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9pbnN0YW5jZS9EYXRldGltZUluc3RhbmNlLmpzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL2luc3RhbmNlL0dyaWQyWDJJbnN0YW5jZS5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9pbnN0YW5jZS9HcmlkM3gzeDNJbnN0YW5jZS5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9pbnN0YW5jZS9HcmlkNHg0eDR4NEluc3RhbmNlLmpzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL2luc3RhbmNlL0dyaWRDdXN0b21JbnN0YW5jZS5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9pbnN0YW5jZS9HcmlkU2luZ2xlSW5zdGFuY2UuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vaW5zdGFuY2UvSW5zdGFuY2UuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vaW5zdGFuY2UvT3B0aW9uLmpzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL2luc3RhbmNlL1JhZGlvLmpzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL2luc3RhbmNlL1JhZGlvSW5zdGFuY2UuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vaW5zdGFuY2UvUmVzZXRCdXR0b25JbnN0YW5jZS5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9pbnN0YW5jZS9TZWxlY3RJbnN0YW5jZS5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9pbnN0YW5jZS9TdWJtaXRCdXR0b25JbnN0YW5jZS5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9pbnN0YW5jZS9UYWIuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vaW5zdGFuY2UvVGFiQ29udHJvbEluc3RhbmNlLmpzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL2luc3RhbmNlL1RleHRJbnN0YW5jZS5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9wcm9wZXJ0eS9CdXR0b25Qcm9wZXJ0eS5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9wcm9wZXJ0eS9DaGVja2JveFByb3BlcnR5LmpzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL3Byb3BlcnR5L0RhdGV0aW1lUHJvcGVydHkuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vcHJvcGVydHkvR3JpZFByb3BlcnR5LmpzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL3Byb3BlcnR5L1BhZ2VQcm9wZXJ0eS5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9wcm9wZXJ0eS9Qcm9wZXJ0eS5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9wcm9wZXJ0eS9SYWRpb1Byb3BlcnR5LmpzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL3Byb3BlcnR5L1NlbGVjdFByb3BlcnR5LmpzIiwid2VicGFjazovLy8uL3NyYy9mb3JtL3Byb3BlcnR5L1RleHRQcm9wZXJ0eS5qcyJdLCJuYW1lcyI6WyJGb3JtQnVpbGRlciIsImNvbnN0cnVjdG9yIiwiY29udGFpbmVyIiwid2luZG93IiwiZm9ybUJ1aWxkZXIiLCJmb3JtUG9zaXRpb24iLCJ0b29sYmFyIiwiVG9vbGJhciIsImFwcGVuZCIsInBhbGV0dGUiLCJQYWxldHRlIiwicHJvcGVydHlQYWxldHRlIiwiY29tcG9uZW50cyIsInBhZ2VQcm9wZXJ0eSIsIlBhZ2VQcm9wZXJ0eSIsInByb3BlcnR5Q29udGFpbmVyIiwic2hvdyIsInRhYkNvbnRyb2wiLCJjb250YWluZXJzIiwiaW5zdGFuY2VzIiwiaW5pdFJvb3RDb250YWluZXIiLCJib2R5IiwiJCIsInNoYWRvd0NvbnRhaW5lciIsInJvdyIsImNhbnZhcyIsInJvb3RDb250YWluZXIiLCJDYW52YXNDb250YWluZXIiLCJwdXNoIiwiVXRpbHMiLCJhdHRhY2hTb3J0YWJsZSIsImluaXREYXRhIiwicmVwb3J0RGVmIiwiX2Zvcm1CdWlsZGVyIiwiZGF0YXNvdXJjZXMiLCJwYXJhbXMiLCJkYXRhc2V0TWFwIiwiTWFwIiwiZHMiLCJkYXRhc2V0cyIsImRhdGFzZXQiLCJwYXJhbWV0ZXJzIiwiY29uY2F0Iiwic2V0IiwibmFtZSIsImZpZWxkcyIsInJlcG9ydFBhcmFtZXRlcnMiLCJmb3JtIiwic2VhcmNoRm9ybSIsImJ1aWxkUGFnZUVsZW1lbnRzIiwicmVmcmVzaFZhbHVlIiwiYnVpbGREYXRhIiwic2VhcmNoRm9ybVhtbCIsInRvWG1sIiwidG9Kc29uIiwiZWxlbWVudHMiLCJwYXJlbnRDb250YWluZXIiLCJsZW5ndGgiLCJpIiwiZWxlbWVudCIsInR5cGUiLCJ0YXJnZXRDb21wb25lbnQiLCJlYWNoIiwiaW5kZXgiLCJjIiwiY29tcG9uZW50Iiwic3VwcG9ydCIsImF0dGFjaENvbXBvbmVudCIsImdldEluc3RhbmNlIiwiaWQiLCJ0YXJnZXQiLCJpdGVtIiwiaW5zdGFuY2UiLCJqc29uIiwieG1sIiwiZ2V0Q29udGFpbmVyIiwiY29udGFpbmVySWQiLCJ0YXJnZXRDb250YWluZXIiLCJzZWxlY3RFbGVtZW50IiwiY2hpbGRyZW4iLCJoaWRlIiwic2VsZWN0Iiwic2FtZUluc3RhbmNlIiwicHJvcCIsInJlbW92ZUNsYXNzIiwiYWRkQ2xhc3MiLCJpbnN0YW5jZUlkIiwicHJvcGVydHkiLCJhZGRJbnN0YW5jZSIsIm5ld0luc3RhbmNlIiwibmV3RWxlbWVudCIsImdldENvbXBvbmVudCIsImNvbXBvbmVudElkIiwiYXR0ciIsIkNvbXBvbmVudCIsIklEIiwiaW5pdENvbnRhaW5lciIsImluaXRDb21wb25lbnRzIiwiYWRkQ29tcG9uZW50IiwiR3JpZFNpbmdsZUNvbXBvbmVudCIsImljb24iLCJsYWJlbCIsIkdyaWQyWDJDb21wb25lbnQiLCJHcmlkM3gzeDNDb21wb25lbnQiLCJHcmlkNHg0eDR4NENvbXBvbmVudCIsIkdyaWRDdXN0b21Db21wb25lbnQiLCJUZXh0Q29tcG9uZW50IiwiRGF0ZXRpbWVDb21wb25lbnQiLCJSYWRpb0NvbXBvbmVudCIsIkNoZWNrYm94Q29tcG9uZW50IiwiU2VsZWN0Q29tcG9uZW50IiwiU3VibWl0QnV0dG9uQ29tcG9uZW50IiwiUmVzZXRCdXR0b25Db21wb25lbnQiLCJ1bCIsImNvbXBvbmVudExpIiwicHJvcGVydHlMaSIsInByb3BlcnR5SWQiLCJ0YWJDb250ZW50IiwiY29tcG9uZW50UGFsZXR0ZSIsImNvbCIsInRvb2wiLCJ0aXAiLCJidWlsZFJlbW92ZSIsImJ1aWxkU2F2ZSIsInNhdmUiLCJyZW1vdmUiLCJzZWxmIiwiY2xpY2siLCJkZWxldGVFbGVtZW50IiwiZG9jdW1lbnQiLCJrZXlkb3duIiwiZSIsIndoaWNoIiwiYm9vdGJveCIsImFsZXJ0IiwicGFyZW50IiwicmVtb3ZlQ2hpbGQiLCJwb3MiLCJ0YXJnZXRJbnMiLCJzcGxpY2UiLCJyZW1vdmVDb250YWluZXJJbnN0YW5jZUNoaWxkcmVuIiwic2VxIiwic2VxVmFsdWUiLCJTRVFVRU5DRSIsInZhbHVlIiwic29ydGFibGUiLCJ0b2xlcmFuY2UiLCJkZWxheSIsImRyb3BPbkVtcHR5IiwiZm9yY2VQbGFjZWhvbGRlclNpemUiLCJmb3JjZUhlbHBlclNpemUiLCJwbGFjZWhvbGRlciIsImNvbm5lY3RXaXRoIiwic3RhcnQiLCJ1aSIsImNzcyIsInJlY2VpdmUiLCJhZGQiLCJzdG9wIiwiaGFzQ2xhc3MiLCJyZXBsYWNlV2l0aCIsInRhcmdldEluc3RhbmNlIiwiYWRkQ2hpbGQiLCJuZXdPcmRlciIsImluaXRKc29uIiwiY29scyIsImluaXRGcm9tSnNvbiIsIkNvbnRhaW5lckluc3RhbmNlIiwiYWRkRWxlbWVudCIsImNoaWxkcmVuQ29udGFpbmVycyIsImZpbmQiLCJjaGlsZCIsImV2ZW50Iiwic3RvcFByb3BhZ2F0aW9uIiwibW91c2VvdmVyIiwibW91c2VvdXQiLCJpbnMiLCJjaGlsZHJlbkluc3RhbmNlcyIsIlRhYkNvbnRyb2xJbnN0YW5jZSIsInRhYnMiLCJ0YWIiLCJiaW5kaW5nIiwib3B0aW9ucyIsIkNoZWNrYm94UHJvcGVydHkiLCJDaGVja2JveEluc3RhbmNlIiwiZ2V0VHlwZSIsIlRZUEUiLCJnZXRJZCIsImVudGl0eUxpc3QiLCJkcmFnZ2FibGUiLCJyZXZlcnQiLCJjb25uZWN0VG9Tb3J0YWJsZSIsImhlbHBlciIsIkdSSUQiLCJEYXRldGltZVByb3BlcnR5IiwiRGF0ZXRpbWVJbnN0YW5jZSIsIkdyaWRDb21wb25lbnQiLCJHcmlkMlgySW5zdGFuY2UiLCJHcmlkM3gzeDNJbnN0YW5jZSIsIkdyaWQ0eDR4NHg0SW5zdGFuY2UiLCJHcmlkUHJvcGVydHkiLCJHcmlkQ3VzdG9tSW5zdGFuY2UiLCJHcmlkU2luZ2xlSW5zdGFuY2UiLCJSYWRpb1Byb3BlcnR5IiwiUmFkaW9JbnN0YW5jZSIsIkJ1dHRvblByb3BlcnR5IiwiUmVzZXRCdXR0b25JbnN0YW5jZSIsIlNlbGVjdFByb3BlcnR5IiwiU2VsZWN0SW5zdGFuY2UiLCJTdWJtaXRCdXR0b25JbnN0YW5jZSIsIlRleHRQcm9wZXJ0eSIsIlRleHRJbnN0YW5jZSIsIkNvbnRhaW5lciIsInVuaXF1ZUlkIiwiZ2V0Q2hpbGRyZW4iLCJ0b0h0bWwiLCJkaXYiLCJidWlsZENoaWxkcmVuSHRtbCIsIkNvbENvbnRhaW5lciIsImNvbHNpemUiLCJzaXplIiwib3JkZXJBcnJheSIsImh0bWwiLCJzZWFyY2hBbmRSZW1vdmVDaGlsZCIsInVuc2hpZnQiLCJpbkFycmF5IiwiVGFiQ29udGFpbmVyIiwidG9KU09OIiwicmVhZHkiLCJmbiIsImRhdGV0aW1lcGlja2VyIiwiZGF0ZXMiLCJkYXlzIiwiZGF5c1Nob3J0IiwiZGF5c01pbiIsIm1vbnRocyIsIm1vbnRoc1Nob3J0IiwidG9kYXkiLCJzdWZmaXgiLCJtZXJpZGllbSIsImpRdWVyeSIsIl9fY3VycmVudF9yZXBvcnRfZGVmIiwiQnV0dG9uSW5zdGFuY2UiLCJJbnN0YW5jZSIsInN0eWxlIiwiYnV0dG9uIiwiZWRpdG9yVHlwZSIsImFsaWduIiwic2V0U3R5bGUiLCJzZXRBbGlnbiIsInNldExhYmVsIiwiQ2hlY2tib3giLCJvcHRpb25zSW5saW5lIiwiY2hlY2tib3giLCJpbmxpbmVDbGFzcyIsIkxBQkVMX1BPU0lUSU9OIiwibGFiZWxFbGVtZW50Iiwic2V0VmFsdWUiLCJpbnB1dEVsZW1lbnQiLCJhZGRPcHRpb24iLCJzZXRPcHRpb25zSW5saW5lIiwiZmlyc3QiLCJyZW1vdmVPcHRpb24iLCJvcHRpb24iLCJ0YXJnZXRJbmRleCIsImZyb21Kc29uIiwidW5kZWZpbmVkIiwibGFiZWxQb3NpdGlvbiIsImJpbmRQYXJhbWV0ZXIiLCJ2aXNpYmxlIiwic2hvd0JvcmRlciIsImJvcmRlcldpZHRoIiwiYm9yZGVyQ29sb3IiLCJzZXRCb3JkZXJXaWR0aCIsImlzRGF0ZSIsImRhdGVGb3JtYXQiLCJkYXRlUGlja2VyaW5wdXRHcm91cCIsInRleHQiLCJwaWNrZXJJY29uIiwiZm9ybWF0IiwiYXV0b2Nsb3NlIiwic3RhcnRWaWV3IiwibWluVmlldyIsInNldERhdGVGb3JtYXQiLCJzZWFyY2hPcGVyYXRvciIsImNvbDEiLCJjb2wyIiwid2lkdGgiLCJzZXRCb3JkZXJDb2xvciIsImNvbG9yIiwiY29sMyIsImNvbDQiLCJjb2xzSnNvbiIsInByb21wdCIsInNwbGl0IiwiY29sTnVtIiwicGFyc2VJbnQiLCJnZXRFbGVtZW50IiwiVE9QIiwiZW5hYmxlIiwiaXNSZXF1aXJlZCIsInNldExhYmVsUG9zaXRpb24iLCJwb3NpdGlvbiIsIlBPU19DTEFTU0VTIiwiTEVGVCIsInNldEJpbmRQYXJhbWV0ZXIiLCJnZXRFbGVtZW50SWQiLCJiaW5kVGFibGVOYW1lIiwiYmluZFRhYmxlIiwiYmluZEZpZWxkIiwiT3B0aW9uIiwiUmFkaW8iLCJyYWRpbyIsImlucHV0Iiwib3B0aW9uTnVtIiwidXNlRGF0YXNldCIsImxhYmVsRmllbGQiLCJ2YWx1ZUZpZWxkIiwiVGFiIiwidGFibnVtIiwibGkiLCJ0YWJOYW1lIiwibGluayIsImdldFRhYk5hbWUiLCJzZXRUYWJOYW1lIiwibGlUb0h0bWwiLCJnZXRUYWJDb250ZW50IiwidGFiTnVtIiwiYWRkVGFiIiwiYWN0aXZlIiwiZ2V0VGFiIiwidGFyZ2V0VGFiIiwidGV4dElucHV0IiwiUHJvcGVydHkiLCJfdGhpcyIsImJ1dHRvblR5cGUiLCJsYWJlbEdyb3VwIiwibGFiZWxFZGl0b3IiLCJjaGFuZ2UiLCJjdXJyZW50IiwidmFsIiwic2VsZWN0R3JvdXAiLCJ0eXBlU2VsZWN0IiwiYWxpZ25Hcm91cCIsImFsaWduU2VsZWN0IiwiaW5pdCIsImJ1aWxkQmluZFBhcmFtZXRlciIsInBvc2l0aW9uTGFiZWxHcm91cCIsImJ1aWxkUG9zaXRpb25MYWJlbEdyb3VwIiwiYnVpbGRMYWJlbEdyb3VwIiwiYnVpbGRPcHRpb25zSW5saW5lR3JvdXAiLCJvcHRpb25Gb3JtR3JvdXAiLCJhZGRDaGVja2JveEVkaXRvciIsImlucHV0R3JvdXAiLCJhcnJheSIsImFkZG9uIiwiZGVsIiwibmV3T3B0aW9uIiwiZW1wdHkiLCJmb3JtYXRHcm91cCIsImZvcm1hdFNlbGVjdCIsInNob3dCb3JkZXJHcm91cCIsInNob3dMaW5lUmFkaW9Hcm91cCIsInJhZGlvTmFtZSIsInNob3dCb3JkZXJSYWRpbyIsImJvcmRlclByb3BHcm91cCIsImJvcmRlcldpZHRoVGV4dCIsImJvcmRlckNvbG9yVGV4dCIsImhpZGVCb3JkZXJSYWRpbyIsImJvcmRlcldpZHRoR3JvdXAiLCJib3JkZXJDb2xvckdyb3VwIiwicG9zaXRpb25Hcm91cCIsInBvc2l0aW9uU2VsZWN0IiwiaW5saW5lR3JvdXAiLCJvcHRpb25zSW5saW5lU2VsZWN0IiwiZ3JvdXAiLCJiaW5kRmllbGRFZGl0b3IiLCJsYWJlbExhYmVsIiwidGV4dExhYmVsIiwicG9zaXRpb25MYWJlbCIsInBvc2l0aW9uTGFiZWxTZWxlY3QiLCJhZGRSYWRpb0VkaXRvciIsInJlcG9ydCIsImVkaXRvciIsImRhdGFzb3VyY2VTZWxlY3QiLCJzaW1wbGVPcHRpb25Hcm91cCIsImRhdGFzZXRHcm91cCIsImRhdGFzZXRTZWxlY3QiLCJkc05hbWUiLCJkYXRhc2V0TmFtZSIsImtleXMiLCJnZXQiLCJsYWJlbFNlbGVjdCIsInZhbHVlR3JvdXAiLCJ2YWx1ZVNlbGVjdCIsInRhcmdldEZpZWxkIiwiZmllbGQiLCJhZGRPcHRpb25FZGl0b3IiXSwibWFwcGluZ3MiOiI7UUFBQTtRQUNBOztRQUVBO1FBQ0E7O1FBRUE7UUFDQTtRQUNBO1FBQ0E7UUFDQTtRQUNBO1FBQ0E7UUFDQTtRQUNBO1FBQ0E7O1FBRUE7UUFDQTs7UUFFQTtRQUNBOztRQUVBO1FBQ0E7UUFDQTs7O1FBR0E7UUFDQTs7UUFFQTtRQUNBOztRQUVBO1FBQ0E7UUFDQTtRQUNBLDBDQUEwQyxnQ0FBZ0M7UUFDMUU7UUFDQTs7UUFFQTtRQUNBO1FBQ0E7UUFDQSx3REFBd0Qsa0JBQWtCO1FBQzFFO1FBQ0EsaURBQWlELGNBQWM7UUFDL0Q7O1FBRUE7UUFDQTtRQUNBO1FBQ0E7UUFDQTtRQUNBO1FBQ0E7UUFDQTtRQUNBO1FBQ0E7UUFDQTtRQUNBLHlDQUF5QyxpQ0FBaUM7UUFDMUUsZ0hBQWdILG1CQUFtQixFQUFFO1FBQ3JJO1FBQ0E7O1FBRUE7UUFDQTtRQUNBO1FBQ0EsMkJBQTJCLDBCQUEwQixFQUFFO1FBQ3ZELGlDQUFpQyxlQUFlO1FBQ2hEO1FBQ0E7UUFDQTs7UUFFQTtRQUNBLHNEQUFzRCwrREFBK0Q7O1FBRXJIO1FBQ0E7OztRQUdBO1FBQ0E7Ozs7Ozs7Ozs7OztBQ2xGQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQSxDQUFDOztBQUVEO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBLGdCQUFnQjtBQUNoQjtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQSxnREFBZ0QsZ0JBQWdCO0FBQ2hFLGdDQUFnQztBQUNoQztBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBLEdBQUc7O0FBRUgsQ0FBQzs7QUFFRDtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7O0FBR0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBOztBQUVBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7OztBQUdBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBLEtBQUs7QUFDTDs7QUFFQTs7QUFFQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBOztBQUVBLENBQUM7O0FBRUQ7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7OztBQUdBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0EsZ0NBQWdDO0FBQ2hDO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBLE9BQU87QUFDUDtBQUNBO0FBQ0E7QUFDQSxLQUFLO0FBQ0w7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBLE9BQU87QUFDUDtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0EsS0FBSztBQUNMO0FBQ0E7QUFDQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTtBQUNBLEtBQUs7QUFDTDs7QUFFQTs7QUFFQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQSxLQUFLO0FBQ0w7QUFDQTtBQUNBLEtBQUs7O0FBRUwsQ0FBQzs7QUFFRDtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7O0FBR0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0EsMkJBQTJCO0FBQzNCLDJCQUEyQjtBQUMzQjtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBOztBQUVBOztBQUVBLHNGQUFzRixlQUFlO0FBQ3JHOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBLEtBQUs7QUFDTDtBQUNBOztBQUVBOztBQUVBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUEsaURBQWlELHFEQUFxRDtBQUN0RztBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0EsV0FBVztBQUNYLFNBQVM7QUFDVDtBQUNBLEtBQUs7QUFDTDtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBOztBQUVBO0FBQ0E7OztBQUdBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQSwrQkFBK0I7QUFDL0I7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQSxLQUFLO0FBQ0w7O0FBRUE7O0FBRUE7QUFDQTs7O0FBR0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7O0FBR0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTs7QUFFQSw2QkFBNkI7QUFDN0I7QUFDQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQSxLQUFLO0FBQ0wsR0FBRzs7QUFFSCxDQUFDOztBQUVEO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0Esb0NBQW9DO0FBQ3BDO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0EsS0FBSztBQUNMO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0EsT0FBTztBQUNQO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7OztBQUdBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQSwrQkFBK0I7O0FBRS9CO0FBQ0E7QUFDQTtBQUNBLEtBQUs7QUFDTDs7QUFFQTs7QUFFQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0EsR0FBRzs7QUFFSCxDQUFDOztBQUVEO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0EsMkJBQTJCOztBQUUzQjs7QUFFQTs7QUFFQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0EsS0FBSztBQUNMOztBQUVBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBLDJCQUEyQjtBQUMzQjs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBOztBQUVBOztBQUVBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBOztBQUVBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQSxLQUFLO0FBQ0w7O0FBRUE7O0FBRUE7QUFDQTs7O0FBR0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7O0FBR0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0Esc0VBQXNFLHNCQUFzQjtBQUM1RjtBQUNBO0FBQ0E7O0FBRUEsQ0FBQzs7QUFFRDtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7O0FBR0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBLFNBQVM7QUFDVDtBQUNBOztBQUVBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBLHNDQUFzQyxnQ0FBZ0M7O0FBRXRFOztBQUVBOztBQUVBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBOztBQUVBO0FBQ0E7QUFDQTtBQUNBLE9BQU87QUFDUCxLQUFLOztBQUVMO0FBQ0E7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7O0FBRUEseUNBQXlDLGdDQUFnQzs7QUFFekU7QUFDQTtBQUNBO0FBQ0E7QUFDQSxXQUFXO0FBQ1g7QUFDQTtBQUNBLEtBQUs7QUFDTDs7QUFFQTtBQUNBOztBQUVBOztBQUVBOztBQUVBOztBQUVBOztBQUVBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBLE9BQU87QUFDUDs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBLE9BQU87QUFDUCxLQUFLO0FBQ0w7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBLEtBQUs7QUFDTDtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBLEtBQUs7QUFDTDs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0EsT0FBTzs7QUFFUDs7QUFFQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBLEtBQUs7QUFDTDs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUEsS0FBSztBQUNMO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQSxLQUFLO0FBQ0w7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQSxLQUFLO0FBQ0w7O0FBRUE7QUFDQTtBQUNBLDJCQUEyQjtBQUMzQjtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0EsT0FBTztBQUNQO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0EsS0FBSztBQUNMOztBQUVBLGtEQUFrRDtBQUNsRDtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7O0FBR0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBLCtCQUErQjs7QUFFL0I7QUFDQTtBQUNBO0FBQ0EsS0FBSztBQUNMOztBQUVBOztBQUVBO0FBQ0E7OztBQUdBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7OztBQUdBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBLGlFQUFpRSxrQ0FBa0M7O0FBRW5HOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0EsT0FBTztBQUNQLEtBQUs7QUFDTDtBQUNBLEdBQUc7O0FBRUgsQ0FBQzs7QUFFRDtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0EsdUlBQXVJOztBQUV2STtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBLEtBQUs7O0FBRUw7QUFDQSxzQ0FBc0MsT0FBTztBQUM3QztBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBLDJEQUEyRCxXQUFXO0FBQ3RFOztBQUVBLDBDQUEwQyxTQUFTO0FBQ25EO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBOztBQUVBLDhEQUE4RCxZQUFZO0FBQzFFOztBQUVBLGtEQUFrRCxVQUFVO0FBQzVEO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBLEtBQUs7QUFDTDtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQSxzQkFBc0I7O0FBRXRCO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQSxpQ0FBaUMsS0FBSztBQUN0Qzs7QUFFQTtBQUNBO0FBQ0EsT0FBTztBQUNQO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQSxrQ0FBa0MsaUJBQWlCLGtDQUFrQztBQUNyRjtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUEseUJBQXlCOztBQUV6QjtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQSxLQUFLOztBQUVMO0FBQ0E7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0EsS0FBSztBQUNMOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTs7QUFFQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0EsS0FBSztBQUNMOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7O0FBRUE7QUFDQTtBQUNBOztBQUVBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBLGNBQWMsb0NBQW9DO0FBQ2xEO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQSxTQUFTO0FBQ1Q7QUFDQSxLQUFLOztBQUVMOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0EsS0FBSztBQUNMO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0EsMEJBQTBCO0FBQzFCO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBLHVFQUF1RTtBQUN2RSwwQkFBMEIsV0FBVyx3RUFBd0U7QUFDN0c7QUFDQTtBQUNBO0FBQ0E7QUFDQSw4QkFBOEIsa0JBQWtCO0FBQ2hELHFCQUFxQjtBQUNyQiw4QkFBOEIsdURBQXVEOztBQUVyRixzQkFBc0I7QUFDdEI7O0FBRUE7QUFDQSxvQ0FBb0MsZ0ZBQWdGO0FBQ3BILG9DQUFvQyxnRkFBZ0Y7QUFDcEgsb0NBQW9DLGlGQUFpRjtBQUNySCxvQ0FBb0M7O0FBRXBDOztBQUVBO0FBQ0EsaUJBQWlCO0FBQ2pCOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0EsbURBQW1EO0FBQ25EO0FBQ0EsT0FBTyxrRkFBa0Y7QUFDekY7QUFDQTtBQUNBLEtBQUs7QUFDTDtBQUNBO0FBQ0EscURBQXFEO0FBQ3JEO0FBQ0EsT0FBTyx1REFBdUQ7QUFDOUQ7QUFDQTtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQSxLQUFLO0FBQ0w7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBLEtBQUs7QUFDTDs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBLEtBQUs7QUFDTDs7QUFFQTs7QUFFQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBLENBQUM7O0FBRUQ7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7OztBQUdBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7O0FBRUEsZ0NBQWdDO0FBQ2hDO0FBQ0E7QUFDQTtBQUNBO0FBQ0EsR0FBRzs7O0FBR0g7QUFDQTs7QUFFQSxpQ0FBaUM7O0FBRWpDOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQSxLQUFLO0FBQ0w7QUFDQTtBQUNBOztBQUVBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7OztBQUdBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQSxLQUFLO0FBQ0w7O0FBRUE7O0FBRUE7QUFDQTs7O0FBR0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQSxDQUFDOztBQUVEO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0EscUNBQXFDO0FBQ3JDO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0EsT0FBTztBQUNQLDZCQUE2QixxQkFBcUI7QUFDbEQ7QUFDQTtBQUNBO0FBQ0EsT0FBTztBQUNQOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQSw0QkFBNEIsS0FBSztBQUNqQztBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBLEtBQUs7QUFDTDs7QUFFQTs7QUFFQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0EsS0FBSztBQUNMLEdBQUc7O0FBRUgsQ0FBQzs7QUFFRDtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7O0FBR0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7O0FBRUE7QUFDQTtBQUNBO0FBQ0EsS0FBSztBQUNMO0FBQ0E7QUFDQSxLQUFLOztBQUVMO0FBQ0E7O0FBRUE7O0FBRUE7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBLE9BQU87QUFDUDtBQUNBO0FBQ0E7QUFDQSxPQUFPO0FBQ1AsS0FBSztBQUNMOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQSxPQUFPO0FBQ1A7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBOzs7QUFHQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQSxLQUFLO0FBQ0w7O0FBRUE7O0FBRUE7QUFDQTs7O0FBR0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7O0FBR0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUEsQ0FBQzs7QUFFRDtBQUNBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7O0FBR0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0EsOEJBQThCOztBQUU5Qjs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTs7QUFFQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7QUFDQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBOztBQUVBOztBQUVBO0FBQ0E7O0FBRUE7QUFDQTs7QUFFQTs7QUFFQTs7QUFFQTtBQUNBOztBQUVBO0FBQ0E7QUFDQTtBQUNBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0EsT0FBTztBQUNQO0FBQ0E7OztBQUdBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7QUFDQTs7QUFFQTtBQUNBO0FBQ0EsS0FBSztBQUNMOztBQUVBOztBQUVBO0FBQ0E7OztBQUdBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7OztBQUdBO0FBQ0E7O0FBRUE7QUFDQTtBQUNBO0FBQ0E7O0FB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QUFNM1YsTUFBTixDQUFhNFYsR0FBYjtBQUNBLFlBQUl2TSxNQUFJdkksRUFBRSxrR0FBRixDQUFSO0FBQ0F1SSxZQUFJOUIsS0FBSixDQUFVLFlBQVU7QUFDaEIsZ0JBQUlzTyxZQUFVdk8sS0FBS3FOLE9BQUwsQ0FBYWhGLFNBQWIsRUFBZDtBQUNBckksaUJBQUsrUSxlQUFMLENBQXFCeEMsU0FBckI7QUFDSCxTQUhEO0FBSUFGLGNBQU0zVixNQUFOLENBQWFxSixHQUFiO0FBQ0EsYUFBS29PLGlCQUFMLENBQXVCelgsTUFBdkIsQ0FBOEJ5VixVQUE5QjtBQUNIO0FBaksrQyxDOzs7Ozs7Ozs7Ozs7QUNKcEQ7QUFBQTtBQUFBO0FBQUE7OztBQUdBO0FBQ2UsTUFBTS9JLFlBQU4sU0FBMkIySCxvREFBM0IsQ0FBbUM7QUFDOUM1VSxnQkFBWTZYLE1BQVosRUFBbUI7QUFDZjtBQUNBLGFBQUtyQyxJQUFMLENBQVVxQyxNQUFWO0FBQ0g7QUFDRHJDLFNBQUtxQyxNQUFMLEVBQVk7QUFDUixhQUFLdlEsR0FBTCxDQUFTL0csTUFBVCxDQUFnQixLQUFLa1Ysa0JBQUwsRUFBaEI7QUFDQSxhQUFLQyxrQkFBTCxHQUF3QixLQUFLQyx1QkFBTCxFQUF4QjtBQUNBLGFBQUtyTyxHQUFMLENBQVMvRyxNQUFULENBQWdCLEtBQUttVixrQkFBckI7QUFDQSxhQUFLcE8sR0FBTCxDQUFTL0csTUFBVCxDQUFnQixLQUFLcVYsZUFBTCxFQUFoQjtBQUNIO0FBQ0QzUyxpQkFBYWlTLE9BQWIsRUFBcUI7QUFDakIsY0FBTWpTLFlBQU4sQ0FBbUJpUyxPQUFuQjtBQUNBLFlBQUcsS0FBS0csVUFBUixFQUFtQjtBQUNmLGlCQUFLQSxVQUFMLENBQWdCRixHQUFoQixDQUFvQkQsUUFBUTdGLFVBQTVCO0FBQ0g7QUFDSjtBQWhCNkMsQyIsImZpbGUiOiJzZWFyY2hmb3JtLmJ1bmRsZS5qcyIsInNvdXJjZXNDb250ZW50IjpbIiBcdC8vIFRoZSBtb2R1bGUgY2FjaGVcbiBcdHZhciBpbnN0YWxsZWRNb2R1bGVzID0ge307XG5cbiBcdC8vIFRoZSByZXF1aXJlIGZ1bmN0aW9uXG4gXHRmdW5jdGlvbiBfX3dlYnBhY2tfcmVxdWlyZV9fKG1vZHVsZUlkKSB7XG5cbiBcdFx0Ly8gQ2hlY2sgaWYgbW9kdWxlIGlzIGluIGNhY2hlXG4gXHRcdGlmKGluc3RhbGxlZE1vZHVsZXNbbW9kdWxlSWRdKSB7XG4gXHRcdFx0cmV0dXJuIGluc3RhbGxlZE1vZHVsZXNbbW9kdWxlSWRdLmV4cG9ydHM7XG4gXHRcdH1cbiBcdFx0Ly8gQ3JlYXRlIGEgbmV3IG1vZHVsZSAoYW5kIHB1dCBpdCBpbnRvIHRoZSBjYWNoZSlcbiBcdFx0dmFyIG1vZHVsZSA9IGluc3RhbGxlZE1vZHVsZXNbbW9kdWxlSWRdID0ge1xuIFx0XHRcdGk6IG1vZHVsZUlkLFxuIFx0XHRcdGw6IGZhbHNlLFxuIFx0XHRcdGV4cG9ydHM6IHt9XG4gXHRcdH07XG5cbiBcdFx0Ly8gRXhlY3V0ZSB0aGUgbW9kdWxlIGZ1bmN0aW9uXG4gXHRcdG1vZHVsZXNbbW9kdWxlSWRdLmNhbGwobW9kdWxlLmV4cG9ydHMsIG1vZHVsZSwgbW9kdWxlLmV4cG9ydHMsIF9fd2VicGFja19yZXF1aXJlX18pO1xuXG4gXHRcdC8vIEZsYWcgdGhlIG1vZHVsZSBhcyBsb2FkZWRcbiBcdFx0bW9kdWxlLmwgPSB0cnVlO1xuXG4gXHRcdC8vIFJldHVybiB0aGUgZXhwb3J0cyBvZiB0aGUgbW9kdWxlXG4gXHRcdHJldHVybiBtb2R1bGUuZXhwb3J0cztcbiBcdH1cblxuXG4gXHQvLyBleHBvc2UgdGhlIG1vZHVsZXMgb2JqZWN0IChfX3dlYnBhY2tfbW9kdWxlc19fKVxuIFx0X193ZWJwYWNrX3JlcXVpcmVfXy5tID0gbW9kdWxlcztcblxuIFx0Ly8gZXhwb3NlIHRoZSBtb2R1bGUgY2FjaGVcbiBcdF9fd2VicGFja19yZXF1aXJlX18uYyA9IGluc3RhbGxlZE1vZHVsZXM7XG5cbiBcdC8vIGRlZmluZSBnZXR0ZXIgZnVuY3Rpb24gZm9yIGhhcm1vbnkgZXhwb3J0c1xuIFx0X193ZWJwYWNrX3JlcXVpcmVfXy5kID0gZnVuY3Rpb24oZXhwb3J0cywgbmFtZSwgZ2V0dGVyKSB7XG4gXHRcdGlmKCFfX3dlYnBhY2tfcmVxdWlyZV9fLm8oZXhwb3J0cywgbmFtZSkpIHtcbiBcdFx0XHRPYmplY3QuZGVmaW5lUHJvcGVydHkoZXhwb3J0cywgbmFtZSwgeyBlbnVtZXJhYmxlOiB0cnVlLCBnZXQ6IGdldHRlciB9KTtcbiBcdFx0fVxuIFx0fTtcblxuIFx0Ly8gZGVmaW5lIF9fZXNNb2R1bGUgb24gZXhwb3J0c1xuIFx0X193ZWJwYWNrX3JlcXVpcmVfXy5yID0gZnVuY3Rpb24oZXhwb3J0cykge1xuIFx0XHRpZih0eXBlb2YgU3ltYm9sICE9PSAndW5kZWZpbmVkJyAmJiBTeW1ib2wudG9TdHJpbmdUYWcpIHtcbiBcdFx0XHRPYmplY3QuZGVmaW5lUHJvcGVydHkoZXhwb3J0cywgU3ltYm9sLnRvU3RyaW5nVGFnLCB7IHZhbHVlOiAnTW9kdWxlJyB9KTtcbiBcdFx0fVxuIFx0XHRPYmplY3QuZGVmaW5lUHJvcGVydHkoZXhwb3J0cywgJ19fZXNNb2R1bGUnLCB7IHZhbHVlOiB0cnVlIH0pO1xuIFx0fTtcblxuIFx0Ly8gY3JlYXRlIGEgZmFrZSBuYW1lc3BhY2Ugb2JqZWN0XG4gXHQvLyBtb2RlICYgMTogdmFsdWUgaXMgYSBtb2R1bGUgaWQsIHJlcXVpcmUgaXRcbiBcdC8vIG1vZGUgJiAyOiBtZXJnZSBhbGwgcHJvcGVydGllcyBvZiB2YWx1ZSBpbnRvIHRoZSBuc1xuIFx0Ly8gbW9kZSAmIDQ6IHJldHVybiB2YWx1ZSB3aGVuIGFscmVhZHkgbnMgb2JqZWN0XG4gXHQvLyBtb2RlICYgOHwxOiBiZWhhdmUgbGlrZSByZXF1aXJlXG4gXHRfX3dlYnBhY2tfcmVxdWlyZV9fLnQgPSBmdW5jdGlvbih2YWx1ZSwgbW9kZSkge1xuIFx0XHRpZihtb2RlICYgMSkgdmFsdWUgPSBfX3dlYnBhY2tfcmVxdWlyZV9fKHZhbHVlKTtcbiBcdFx0aWYobW9kZSAmIDgpIHJldHVybiB2YWx1ZTtcbiBcdFx0aWYoKG1vZGUgJiA0KSAmJiB0eXBlb2YgdmFsdWUgPT09ICdvYmplY3QnICYmIHZhbHVlICYmIHZhbHVlLl9fZXNNb2R1bGUpIHJldHVybiB2YWx1ZTtcbiBcdFx0dmFyIG5zID0gT2JqZWN0LmNyZWF0ZShudWxsKTtcbiBcdFx0X193ZWJwYWNrX3JlcXVpcmVfXy5yKG5zKTtcbiBcdFx0T2JqZWN0LmRlZmluZVByb3BlcnR5KG5zLCAnZGVmYXVsdCcsIHsgZW51bWVyYWJsZTogdHJ1ZSwgdmFsdWU6IHZhbHVlIH0pO1xuIFx0XHRpZihtb2RlICYgMiAmJiB0eXBlb2YgdmFsdWUgIT0gJ3N0cmluZycpIGZvcih2YXIga2V5IGluIHZhbHVlKSBfX3dlYnBhY2tfcmVxdWlyZV9fLmQobnMsIGtleSwgZnVuY3Rpb24oa2V5KSB7IHJldHVybiB2YWx1ZVtrZXldOyB9LmJpbmQobnVsbCwga2V5KSk7XG4gXHRcdHJldHVybiBucztcbiBcdH07XG5cbiBcdC8vIGdldERlZmF1bHRFeHBvcnQgZnVuY3Rpb24gZm9yIGNvbXBhdGliaWxpdHkgd2l0aCBub24taGFybW9ueSBtb2R1bGVzXG4gXHRfX3dlYnBhY2tfcmVxdWlyZV9fLm4gPSBmdW5jdGlvbihtb2R1bGUpIHtcbiBcdFx0dmFyIGdldHRlciA9IG1vZHVsZSAmJiBtb2R1bGUuX19lc01vZHVsZSA/XG4gXHRcdFx0ZnVuY3Rpb24gZ2V0RGVmYXVsdCgpIHsgcmV0dXJuIG1vZHVsZVsnZGVmYXVsdCddOyB9IDpcbiBcdFx0XHRmdW5jdGlvbiBnZXRNb2R1bGVFeHBvcnRzKCkgeyByZXR1cm4gbW9kdWxlOyB9O1xuIFx0XHRfX3dlYnBhY2tfcmVxdWlyZV9fLmQoZ2V0dGVyLCAnYScsIGdldHRlcik7XG4gXHRcdHJldHVybiBnZXR0ZXI7XG4gXHR9O1xuXG4gXHQvLyBPYmplY3QucHJvdG90eXBlLmhhc093blByb3BlcnR5LmNhbGxcbiBcdF9fd2VicGFja19yZXF1aXJlX18ubyA9IGZ1bmN0aW9uKG9iamVjdCwgcHJvcGVydHkpIHsgcmV0dXJuIE9iamVjdC5wcm90b3R5cGUuaGFzT3duUHJvcGVydHkuY2FsbChvYmplY3QsIHByb3BlcnR5KTsgfTtcblxuIFx0Ly8gX193ZWJwYWNrX3B1YmxpY19wYXRoX19cbiBcdF9fd2VicGFja19yZXF1aXJlX18ucCA9IFwiXCI7XG5cblxuIFx0Ly8gTG9hZCBlbnRyeSBtb2R1bGUgYW5kIHJldHVybiBleHBvcnRzXG4gXHRyZXR1cm4gX193ZWJwYWNrX3JlcXVpcmVfXyhfX3dlYnBhY2tfcmVxdWlyZV9fLnMgPSBcIi4vc3JjL2Zvcm0vaW5kZXguanNcIik7XG4iLCIvKiFcbiAqIEJvb3RzdHJhcCB2My40LjEgKGh0dHBzOi8vZ2V0Ym9vdHN0cmFwLmNvbS8pXG4gKiBDb3B5cmlnaHQgMjAxMS0yMDE5IFR3aXR0ZXIsIEluYy5cbiAqIExpY2Vuc2VkIHVuZGVyIHRoZSBNSVQgbGljZW5zZVxuICovXG5cbmlmICh0eXBlb2YgalF1ZXJ5ID09PSAndW5kZWZpbmVkJykge1xuICB0aHJvdyBuZXcgRXJyb3IoJ0Jvb3RzdHJhcFxcJ3MgSmF2YVNjcmlwdCByZXF1aXJlcyBqUXVlcnknKVxufVxuXG4rZnVuY3Rpb24gKCQpIHtcbiAgJ3VzZSBzdHJpY3QnO1xuICB2YXIgdmVyc2lvbiA9ICQuZm4uanF1ZXJ5LnNwbGl0KCcgJylbMF0uc3BsaXQoJy4nKVxuICBpZiAoKHZlcnNpb25bMF0gPCAyICYmIHZlcnNpb25bMV0gPCA5KSB8fCAodmVyc2lvblswXSA9PSAxICYmIHZlcnNpb25bMV0gPT0gOSAmJiB2ZXJzaW9uWzJdIDwgMSkgfHwgKHZlcnNpb25bMF0gPiAzKSkge1xuICAgIHRocm93IG5ldyBFcnJvcignQm9vdHN0cmFwXFwncyBKYXZhU2NyaXB0IHJlcXVpcmVzIGpRdWVyeSB2ZXJzaW9uIDEuOS4xIG9yIGhpZ2hlciwgYnV0IGxvd2VyIHRoYW4gdmVyc2lvbiA0JylcbiAgfVxufShqUXVlcnkpO1xuXG4vKiA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT1cbiAqIEJvb3RzdHJhcDogdHJhbnNpdGlvbi5qcyB2My40LjFcbiAqIGh0dHBzOi8vZ2V0Ym9vdHN0cmFwLmNvbS9kb2NzLzMuNC9qYXZhc2NyaXB0LyN0cmFuc2l0aW9uc1xuICogPT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09XG4gKiBDb3B5cmlnaHQgMjAxMS0yMDE5IFR3aXR0ZXIsIEluYy5cbiAqIExpY2Vuc2VkIHVuZGVyIE1JVCAoaHR0cHM6Ly9naXRodWIuY29tL3R3YnMvYm9vdHN0cmFwL2Jsb2IvbWFzdGVyL0xJQ0VOU0UpXG4gKiA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0gKi9cblxuXG4rZnVuY3Rpb24gKCQpIHtcbiAgJ3VzZSBzdHJpY3QnO1xuXG4gIC8vIENTUyBUUkFOU0lUSU9OIFNVUFBPUlQgKFNob3V0b3V0OiBodHRwczovL21vZGVybml6ci5jb20vKVxuICAvLyA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT1cblxuICBmdW5jdGlvbiB0cmFuc2l0aW9uRW5kKCkge1xuICAgIHZhciBlbCA9IGRvY3VtZW50LmNyZWF0ZUVsZW1lbnQoJ2Jvb3RzdHJhcCcpXG5cbiAgICB2YXIgdHJhbnNFbmRFdmVudE5hbWVzID0ge1xuICAgICAgV2Via2l0VHJhbnNpdGlvbiA6ICd3ZWJraXRUcmFuc2l0aW9uRW5kJyxcbiAgICAgIE1velRyYW5zaXRpb24gICAgOiAndHJhbnNpdGlvbmVuZCcsXG4gICAgICBPVHJhbnNpdGlvbiAgICAgIDogJ29UcmFuc2l0aW9uRW5kIG90cmFuc2l0aW9uZW5kJyxcbiAgICAgIHRyYW5zaXRpb24gICAgICAgOiAndHJhbnNpdGlvbmVuZCdcbiAgICB9XG5cbiAgICBmb3IgKHZhciBuYW1lIGluIHRyYW5zRW5kRXZlbnROYW1lcykge1xuICAgICAgaWYgKGVsLnN0eWxlW25hbWVdICE9PSB1bmRlZmluZWQpIHtcbiAgICAgICAgcmV0dXJuIHsgZW5kOiB0cmFuc0VuZEV2ZW50TmFtZXNbbmFtZV0gfVxuICAgICAgfVxuICAgIH1cblxuICAgIHJldHVybiBmYWxzZSAvLyBleHBsaWNpdCBmb3IgaWU4ICggIC5fLilcbiAgfVxuXG4gIC8vIGh0dHBzOi8vYmxvZy5hbGV4bWFjY2F3LmNvbS9jc3MtdHJhbnNpdGlvbnNcbiAgJC5mbi5lbXVsYXRlVHJhbnNpdGlvbkVuZCA9IGZ1bmN0aW9uIChkdXJhdGlvbikge1xuICAgIHZhciBjYWxsZWQgPSBmYWxzZVxuICAgIHZhciAkZWwgPSB0aGlzXG4gICAgJCh0aGlzKS5vbmUoJ2JzVHJhbnNpdGlvbkVuZCcsIGZ1bmN0aW9uICgpIHsgY2FsbGVkID0gdHJ1ZSB9KVxuICAgIHZhciBjYWxsYmFjayA9IGZ1bmN0aW9uICgpIHsgaWYgKCFjYWxsZWQpICQoJGVsKS50cmlnZ2VyKCQuc3VwcG9ydC50cmFuc2l0aW9uLmVuZCkgfVxuICAgIHNldFRpbWVvdXQoY2FsbGJhY2ssIGR1cmF0aW9uKVxuICAgIHJldHVybiB0aGlzXG4gIH1cblxuICAkKGZ1bmN0aW9uICgpIHtcbiAgICAkLnN1cHBvcnQudHJhbnNpdGlvbiA9IHRyYW5zaXRpb25FbmQoKVxuXG4gICAgaWYgKCEkLnN1cHBvcnQudHJhbnNpdGlvbikgcmV0dXJuXG5cbiAgICAkLmV2ZW50LnNwZWNpYWwuYnNUcmFuc2l0aW9uRW5kID0ge1xuICAgICAgYmluZFR5cGU6ICQuc3VwcG9ydC50cmFuc2l0aW9uLmVuZCxcbiAgICAgIGRlbGVnYXRlVHlwZTogJC5zdXBwb3J0LnRyYW5zaXRpb24uZW5kLFxuICAgICAgaGFuZGxlOiBmdW5jdGlvbiAoZSkge1xuICAgICAgICBpZiAoJChlLnRhcmdldCkuaXModGhpcykpIHJldHVybiBlLmhhbmRsZU9iai5oYW5kbGVyLmFwcGx5KHRoaXMsIGFyZ3VtZW50cylcbiAgICAgIH1cbiAgICB9XG4gIH0pXG5cbn0oalF1ZXJ5KTtcblxuLyogPT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09XG4gKiBCb290c3RyYXA6IGFsZXJ0LmpzIHYzLjQuMVxuICogaHR0cHM6Ly9nZXRib290c3RyYXAuY29tL2RvY3MvMy40L2phdmFzY3JpcHQvI2FsZXJ0c1xuICogPT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09XG4gKiBDb3B5cmlnaHQgMjAxMS0yMDE5IFR3aXR0ZXIsIEluYy5cbiAqIExpY2Vuc2VkIHVuZGVyIE1JVCAoaHR0cHM6Ly9naXRodWIuY29tL3R3YnMvYm9vdHN0cmFwL2Jsb2IvbWFzdGVyL0xJQ0VOU0UpXG4gKiA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0gKi9cblxuXG4rZnVuY3Rpb24gKCQpIHtcbiAgJ3VzZSBzdHJpY3QnO1xuXG4gIC8vIEFMRVJUIENMQVNTIERFRklOSVRJT05cbiAgLy8gPT09PT09PT09PT09PT09PT09PT09PVxuXG4gIHZhciBkaXNtaXNzID0gJ1tkYXRhLWRpc21pc3M9XCJhbGVydFwiXSdcbiAgdmFyIEFsZXJ0ICAgPSBmdW5jdGlvbiAoZWwpIHtcbiAgICAkKGVsKS5vbignY2xpY2snLCBkaXNtaXNzLCB0aGlzLmNsb3NlKVxuICB9XG5cbiAgQWxlcnQuVkVSU0lPTiA9ICczLjQuMSdcblxuICBBbGVydC5UUkFOU0lUSU9OX0RVUkFUSU9OID0gMTUwXG5cbiAgQWxlcnQucHJvdG90eXBlLmNsb3NlID0gZnVuY3Rpb24gKGUpIHtcbiAgICB2YXIgJHRoaXMgICAgPSAkKHRoaXMpXG4gICAgdmFyIHNlbGVjdG9yID0gJHRoaXMuYXR0cignZGF0YS10YXJnZXQnKVxuXG4gICAgaWYgKCFzZWxlY3Rvcikge1xuICAgICAgc2VsZWN0b3IgPSAkdGhpcy5hdHRyKCdocmVmJylcbiAgICAgIHNlbGVjdG9yID0gc2VsZWN0b3IgJiYgc2VsZWN0b3IucmVwbGFjZSgvLiooPz0jW15cXHNdKiQpLywgJycpIC8vIHN0cmlwIGZvciBpZTdcbiAgICB9XG5cbiAgICBzZWxlY3RvciAgICA9IHNlbGVjdG9yID09PSAnIycgPyBbXSA6IHNlbGVjdG9yXG4gICAgdmFyICRwYXJlbnQgPSAkKGRvY3VtZW50KS5maW5kKHNlbGVjdG9yKVxuXG4gICAgaWYgKGUpIGUucHJldmVudERlZmF1bHQoKVxuXG4gICAgaWYgKCEkcGFyZW50Lmxlbmd0aCkge1xuICAgICAgJHBhcmVudCA9ICR0aGlzLmNsb3Nlc3QoJy5hbGVydCcpXG4gICAgfVxuXG4gICAgJHBhcmVudC50cmlnZ2VyKGUgPSAkLkV2ZW50KCdjbG9zZS5icy5hbGVydCcpKVxuXG4gICAgaWYgKGUuaXNEZWZhdWx0UHJldmVudGVkKCkpIHJldHVyblxuXG4gICAgJHBhcmVudC5yZW1vdmVDbGFzcygnaW4nKVxuXG4gICAgZnVuY3Rpb24gcmVtb3ZlRWxlbWVudCgpIHtcbiAgICAgIC8vIGRldGFjaCBmcm9tIHBhcmVudCwgZmlyZSBldmVudCB0aGVuIGNsZWFuIHVwIGRhdGFcbiAgICAgICRwYXJlbnQuZGV0YWNoKCkudHJpZ2dlcignY2xvc2VkLmJzLmFsZXJ0JykucmVtb3ZlKClcbiAgICB9XG5cbiAgICAkLnN1cHBvcnQudHJhbnNpdGlvbiAmJiAkcGFyZW50Lmhhc0NsYXNzKCdmYWRlJykgP1xuICAgICAgJHBhcmVudFxuICAgICAgICAub25lKCdic1RyYW5zaXRpb25FbmQnLCByZW1vdmVFbGVtZW50KVxuICAgICAgICAuZW11bGF0ZVRyYW5zaXRpb25FbmQoQWxlcnQuVFJBTlNJVElPTl9EVVJBVElPTikgOlxuICAgICAgcmVtb3ZlRWxlbWVudCgpXG4gIH1cblxuXG4gIC8vIEFMRVJUIFBMVUdJTiBERUZJTklUSU9OXG4gIC8vID09PT09PT09PT09PT09PT09PT09PT09XG5cbiAgZnVuY3Rpb24gUGx1Z2luKG9wdGlvbikge1xuICAgIHJldHVybiB0aGlzLmVhY2goZnVuY3Rpb24gKCkge1xuICAgICAgdmFyICR0aGlzID0gJCh0aGlzKVxuICAgICAgdmFyIGRhdGEgID0gJHRoaXMuZGF0YSgnYnMuYWxlcnQnKVxuXG4gICAgICBpZiAoIWRhdGEpICR0aGlzLmRhdGEoJ2JzLmFsZXJ0JywgKGRhdGEgPSBuZXcgQWxlcnQodGhpcykpKVxuICAgICAgaWYgKHR5cGVvZiBvcHRpb24gPT0gJ3N0cmluZycpIGRhdGFbb3B0aW9uXS5jYWxsKCR0aGlzKVxuICAgIH0pXG4gIH1cblxuICB2YXIgb2xkID0gJC5mbi5hbGVydFxuXG4gICQuZm4uYWxlcnQgICAgICAgICAgICAgPSBQbHVnaW5cbiAgJC5mbi5hbGVydC5Db25zdHJ1Y3RvciA9IEFsZXJ0XG5cblxuICAvLyBBTEVSVCBOTyBDT05GTElDVFxuICAvLyA9PT09PT09PT09PT09PT09PVxuXG4gICQuZm4uYWxlcnQubm9Db25mbGljdCA9IGZ1bmN0aW9uICgpIHtcbiAgICAkLmZuLmFsZXJ0ID0gb2xkXG4gICAgcmV0dXJuIHRoaXNcbiAgfVxuXG5cbiAgLy8gQUxFUlQgREFUQS1BUElcbiAgLy8gPT09PT09PT09PT09PT1cblxuICAkKGRvY3VtZW50KS5vbignY2xpY2suYnMuYWxlcnQuZGF0YS1hcGknLCBkaXNtaXNzLCBBbGVydC5wcm90b3R5cGUuY2xvc2UpXG5cbn0oalF1ZXJ5KTtcblxuLyogPT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09XG4gKiBCb290c3RyYXA6IGJ1dHRvbi5qcyB2My40LjFcbiAqIGh0dHBzOi8vZ2V0Ym9vdHN0cmFwLmNvbS9kb2NzLzMuNC9qYXZhc2NyaXB0LyNidXR0b25zXG4gKiA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT1cbiAqIENvcHlyaWdodCAyMDExLTIwMTkgVHdpdHRlciwgSW5jLlxuICogTGljZW5zZWQgdW5kZXIgTUlUIChodHRwczovL2dpdGh1Yi5jb20vdHdicy9ib290c3RyYXAvYmxvYi9tYXN0ZXIvTElDRU5TRSlcbiAqID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PSAqL1xuXG5cbitmdW5jdGlvbiAoJCkge1xuICAndXNlIHN0cmljdCc7XG5cbiAgLy8gQlVUVE9OIFBVQkxJQyBDTEFTUyBERUZJTklUSU9OXG4gIC8vID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PVxuXG4gIHZhciBCdXR0b24gPSBmdW5jdGlvbiAoZWxlbWVudCwgb3B0aW9ucykge1xuICAgIHRoaXMuJGVsZW1lbnQgID0gJChlbGVtZW50KVxuICAgIHRoaXMub3B0aW9ucyAgID0gJC5leHRlbmQoe30sIEJ1dHRvbi5ERUZBVUxUUywgb3B0aW9ucylcbiAgICB0aGlzLmlzTG9hZGluZyA9IGZhbHNlXG4gIH1cblxuICBCdXR0b24uVkVSU0lPTiAgPSAnMy40LjEnXG5cbiAgQnV0dG9uLkRFRkFVTFRTID0ge1xuICAgIGxvYWRpbmdUZXh0OiAnbG9hZGluZy4uLidcbiAgfVxuXG4gIEJ1dHRvbi5wcm90b3R5cGUuc2V0U3RhdGUgPSBmdW5jdGlvbiAoc3RhdGUpIHtcbiAgICB2YXIgZCAgICA9ICdkaXNhYmxlZCdcbiAgICB2YXIgJGVsICA9IHRoaXMuJGVsZW1lbnRcbiAgICB2YXIgdmFsICA9ICRlbC5pcygnaW5wdXQnKSA/ICd2YWwnIDogJ2h0bWwnXG4gICAgdmFyIGRhdGEgPSAkZWwuZGF0YSgpXG5cbiAgICBzdGF0ZSArPSAnVGV4dCdcblxuICAgIGlmIChkYXRhLnJlc2V0VGV4dCA9PSBudWxsKSAkZWwuZGF0YSgncmVzZXRUZXh0JywgJGVsW3ZhbF0oKSlcblxuICAgIC8vIHB1c2ggdG8gZXZlbnQgbG9vcCB0byBhbGxvdyBmb3JtcyB0byBzdWJtaXRcbiAgICBzZXRUaW1lb3V0KCQucHJveHkoZnVuY3Rpb24gKCkge1xuICAgICAgJGVsW3ZhbF0oZGF0YVtzdGF0ZV0gPT0gbnVsbCA/IHRoaXMub3B0aW9uc1tzdGF0ZV0gOiBkYXRhW3N0YXRlXSlcblxuICAgICAgaWYgKHN0YXRlID09ICdsb2FkaW5nVGV4dCcpIHtcbiAgICAgICAgdGhpcy5pc0xvYWRpbmcgPSB0cnVlXG4gICAgICAgICRlbC5hZGRDbGFzcyhkKS5hdHRyKGQsIGQpLnByb3AoZCwgdHJ1ZSlcbiAgICAgIH0gZWxzZSBpZiAodGhpcy5pc0xvYWRpbmcpIHtcbiAgICAgICAgdGhpcy5pc0xvYWRpbmcgPSBmYWxzZVxuICAgICAgICAkZWwucmVtb3ZlQ2xhc3MoZCkucmVtb3ZlQXR0cihkKS5wcm9wKGQsIGZhbHNlKVxuICAgICAgfVxuICAgIH0sIHRoaXMpLCAwKVxuICB9XG5cbiAgQnV0dG9uLnByb3RvdHlwZS50b2dnbGUgPSBmdW5jdGlvbiAoKSB7XG4gICAgdmFyIGNoYW5nZWQgPSB0cnVlXG4gICAgdmFyICRwYXJlbnQgPSB0aGlzLiRlbGVtZW50LmNsb3Nlc3QoJ1tkYXRhLXRvZ2dsZT1cImJ1dHRvbnNcIl0nKVxuXG4gICAgaWYgKCRwYXJlbnQubGVuZ3RoKSB7XG4gICAgICB2YXIgJGlucHV0ID0gdGhpcy4kZWxlbWVudC5maW5kKCdpbnB1dCcpXG4gICAgICBpZiAoJGlucHV0LnByb3AoJ3R5cGUnKSA9PSAncmFkaW8nKSB7XG4gICAgICAgIGlmICgkaW5wdXQucHJvcCgnY2hlY2tlZCcpKSBjaGFuZ2VkID0gZmFsc2VcbiAgICAgICAgJHBhcmVudC5maW5kKCcuYWN0aXZlJykucmVtb3ZlQ2xhc3MoJ2FjdGl2ZScpXG4gICAgICAgIHRoaXMuJGVsZW1lbnQuYWRkQ2xhc3MoJ2FjdGl2ZScpXG4gICAgICB9IGVsc2UgaWYgKCRpbnB1dC5wcm9wKCd0eXBlJykgPT0gJ2NoZWNrYm94Jykge1xuICAgICAgICBpZiAoKCRpbnB1dC5wcm9wKCdjaGVja2VkJykpICE9PSB0aGlzLiRlbGVtZW50Lmhhc0NsYXNzKCdhY3RpdmUnKSkgY2hhbmdlZCA9IGZhbHNlXG4gICAgICAgIHRoaXMuJGVsZW1lbnQudG9nZ2xlQ2xhc3MoJ2FjdGl2ZScpXG4gICAgICB9XG4gICAgICAkaW5wdXQucHJvcCgnY2hlY2tlZCcsIHRoaXMuJGVsZW1lbnQuaGFzQ2xhc3MoJ2FjdGl2ZScpKVxuICAgICAgaWYgKGNoYW5nZWQpICRpbnB1dC50cmlnZ2VyKCdjaGFuZ2UnKVxuICAgIH0gZWxzZSB7XG4gICAgICB0aGlzLiRlbGVtZW50LmF0dHIoJ2FyaWEtcHJlc3NlZCcsICF0aGlzLiRlbGVtZW50Lmhhc0NsYXNzKCdhY3RpdmUnKSlcbiAgICAgIHRoaXMuJGVsZW1lbnQudG9nZ2xlQ2xhc3MoJ2FjdGl2ZScpXG4gICAgfVxuICB9XG5cblxuICAvLyBCVVRUT04gUExVR0lOIERFRklOSVRJT05cbiAgLy8gPT09PT09PT09PT09PT09PT09PT09PT09XG5cbiAgZnVuY3Rpb24gUGx1Z2luKG9wdGlvbikge1xuICAgIHJldHVybiB0aGlzLmVhY2goZnVuY3Rpb24gKCkge1xuICAgICAgdmFyICR0aGlzICAgPSAkKHRoaXMpXG4gICAgICB2YXIgZGF0YSAgICA9ICR0aGlzLmRhdGEoJ2JzLmJ1dHRvbicpXG4gICAgICB2YXIgb3B0aW9ucyA9IHR5cGVvZiBvcHRpb24gPT0gJ29iamVjdCcgJiYgb3B0aW9uXG5cbiAgICAgIGlmICghZGF0YSkgJHRoaXMuZGF0YSgnYnMuYnV0dG9uJywgKGRhdGEgPSBuZXcgQnV0dG9uKHRoaXMsIG9wdGlvbnMpKSlcblxuICAgICAgaWYgKG9wdGlvbiA9PSAndG9nZ2xlJykgZGF0YS50b2dnbGUoKVxuICAgICAgZWxzZSBpZiAob3B0aW9uKSBkYXRhLnNldFN0YXRlKG9wdGlvbilcbiAgICB9KVxuICB9XG5cbiAgdmFyIG9sZCA9ICQuZm4uYnV0dG9uXG5cbiAgJC5mbi5idXR0b24gICAgICAgICAgICAgPSBQbHVnaW5cbiAgJC5mbi5idXR0b24uQ29uc3RydWN0b3IgPSBCdXR0b25cblxuXG4gIC8vIEJVVFRPTiBOTyBDT05GTElDVFxuICAvLyA9PT09PT09PT09PT09PT09PT1cblxuICAkLmZuLmJ1dHRvbi5ub0NvbmZsaWN0ID0gZnVuY3Rpb24gKCkge1xuICAgICQuZm4uYnV0dG9uID0gb2xkXG4gICAgcmV0dXJuIHRoaXNcbiAgfVxuXG5cbiAgLy8gQlVUVE9OIERBVEEtQVBJXG4gIC8vID09PT09PT09PT09PT09PVxuXG4gICQoZG9jdW1lbnQpXG4gICAgLm9uKCdjbGljay5icy5idXR0b24uZGF0YS1hcGknLCAnW2RhdGEtdG9nZ2xlXj1cImJ1dHRvblwiXScsIGZ1bmN0aW9uIChlKSB7XG4gICAgICB2YXIgJGJ0biA9ICQoZS50YXJnZXQpLmNsb3Nlc3QoJy5idG4nKVxuICAgICAgUGx1Z2luLmNhbGwoJGJ0biwgJ3RvZ2dsZScpXG4gICAgICBpZiAoISgkKGUudGFyZ2V0KS5pcygnaW5wdXRbdHlwZT1cInJhZGlvXCJdLCBpbnB1dFt0eXBlPVwiY2hlY2tib3hcIl0nKSkpIHtcbiAgICAgICAgLy8gUHJldmVudCBkb3VibGUgY2xpY2sgb24gcmFkaW9zLCBhbmQgdGhlIGRvdWJsZSBzZWxlY3Rpb25zIChzbyBjYW5jZWxsYXRpb24pIG9uIGNoZWNrYm94ZXNcbiAgICAgICAgZS5wcmV2ZW50RGVmYXVsdCgpXG4gICAgICAgIC8vIFRoZSB0YXJnZXQgY29tcG9uZW50IHN0aWxsIHJlY2VpdmUgdGhlIGZvY3VzXG4gICAgICAgIGlmICgkYnRuLmlzKCdpbnB1dCxidXR0b24nKSkgJGJ0bi50cmlnZ2VyKCdmb2N1cycpXG4gICAgICAgIGVsc2UgJGJ0bi5maW5kKCdpbnB1dDp2aXNpYmxlLGJ1dHRvbjp2aXNpYmxlJykuZmlyc3QoKS50cmlnZ2VyKCdmb2N1cycpXG4gICAgICB9XG4gICAgfSlcbiAgICAub24oJ2ZvY3VzLmJzLmJ1dHRvbi5kYXRhLWFwaSBibHVyLmJzLmJ1dHRvbi5kYXRhLWFwaScsICdbZGF0YS10b2dnbGVePVwiYnV0dG9uXCJdJywgZnVuY3Rpb24gKGUpIHtcbiAgICAgICQoZS50YXJnZXQpLmNsb3Nlc3QoJy5idG4nKS50b2dnbGVDbGFzcygnZm9jdXMnLCAvXmZvY3VzKGluKT8kLy50ZXN0KGUudHlwZSkpXG4gICAgfSlcblxufShqUXVlcnkpO1xuXG4vKiA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT1cbiAqIEJvb3RzdHJhcDogY2Fyb3VzZWwuanMgdjMuNC4xXG4gKiBodHRwczovL2dldGJvb3RzdHJhcC5jb20vZG9jcy8zLjQvamF2YXNjcmlwdC8jY2Fyb3VzZWxcbiAqID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PVxuICogQ29weXJpZ2h0IDIwMTEtMjAxOSBUd2l0dGVyLCBJbmMuXG4gKiBMaWNlbnNlZCB1bmRlciBNSVQgKGh0dHBzOi8vZ2l0aHViLmNvbS90d2JzL2Jvb3RzdHJhcC9ibG9iL21hc3Rlci9MSUNFTlNFKVxuICogPT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09ICovXG5cblxuK2Z1bmN0aW9uICgkKSB7XG4gICd1c2Ugc3RyaWN0JztcblxuICAvLyBDQVJPVVNFTCBDTEFTUyBERUZJTklUSU9OXG4gIC8vID09PT09PT09PT09PT09PT09PT09PT09PT1cblxuICB2YXIgQ2Fyb3VzZWwgPSBmdW5jdGlvbiAoZWxlbWVudCwgb3B0aW9ucykge1xuICAgIHRoaXMuJGVsZW1lbnQgICAgPSAkKGVsZW1lbnQpXG4gICAgdGhpcy4kaW5kaWNhdG9ycyA9IHRoaXMuJGVsZW1lbnQuZmluZCgnLmNhcm91c2VsLWluZGljYXRvcnMnKVxuICAgIHRoaXMub3B0aW9ucyAgICAgPSBvcHRpb25zXG4gICAgdGhpcy5wYXVzZWQgICAgICA9IG51bGxcbiAgICB0aGlzLnNsaWRpbmcgICAgID0gbnVsbFxuICAgIHRoaXMuaW50ZXJ2YWwgICAgPSBudWxsXG4gICAgdGhpcy4kYWN0aXZlICAgICA9IG51bGxcbiAgICB0aGlzLiRpdGVtcyAgICAgID0gbnVsbFxuXG4gICAgdGhpcy5vcHRpb25zLmtleWJvYXJkICYmIHRoaXMuJGVsZW1lbnQub24oJ2tleWRvd24uYnMuY2Fyb3VzZWwnLCAkLnByb3h5KHRoaXMua2V5ZG93biwgdGhpcykpXG5cbiAgICB0aGlzLm9wdGlvbnMucGF1c2UgPT0gJ2hvdmVyJyAmJiAhKCdvbnRvdWNoc3RhcnQnIGluIGRvY3VtZW50LmRvY3VtZW50RWxlbWVudCkgJiYgdGhpcy4kZWxlbWVudFxuICAgICAgLm9uKCdtb3VzZWVudGVyLmJzLmNhcm91c2VsJywgJC5wcm94eSh0aGlzLnBhdXNlLCB0aGlzKSlcbiAgICAgIC5vbignbW91c2VsZWF2ZS5icy5jYXJvdXNlbCcsICQucHJveHkodGhpcy5jeWNsZSwgdGhpcykpXG4gIH1cblxuICBDYXJvdXNlbC5WRVJTSU9OICA9ICczLjQuMSdcblxuICBDYXJvdXNlbC5UUkFOU0lUSU9OX0RVUkFUSU9OID0gNjAwXG5cbiAgQ2Fyb3VzZWwuREVGQVVMVFMgPSB7XG4gICAgaW50ZXJ2YWw6IDUwMDAsXG4gICAgcGF1c2U6ICdob3ZlcicsXG4gICAgd3JhcDogdHJ1ZSxcbiAgICBrZXlib2FyZDogdHJ1ZVxuICB9XG5cbiAgQ2Fyb3VzZWwucHJvdG90eXBlLmtleWRvd24gPSBmdW5jdGlvbiAoZSkge1xuICAgIGlmICgvaW5wdXR8dGV4dGFyZWEvaS50ZXN0KGUudGFyZ2V0LnRhZ05hbWUpKSByZXR1cm5cbiAgICBzd2l0Y2ggKGUud2hpY2gpIHtcbiAgICAgIGNhc2UgMzc6IHRoaXMucHJldigpOyBicmVha1xuICAgICAgY2FzZSAzOTogdGhpcy5uZXh0KCk7IGJyZWFrXG4gICAgICBkZWZhdWx0OiByZXR1cm5cbiAgICB9XG5cbiAgICBlLnByZXZlbnREZWZhdWx0KClcbiAgfVxuXG4gIENhcm91c2VsLnByb3RvdHlwZS5jeWNsZSA9IGZ1bmN0aW9uIChlKSB7XG4gICAgZSB8fCAodGhpcy5wYXVzZWQgPSBmYWxzZSlcblxuICAgIHRoaXMuaW50ZXJ2YWwgJiYgY2xlYXJJbnRlcnZhbCh0aGlzLmludGVydmFsKVxuXG4gICAgdGhpcy5vcHRpb25zLmludGVydmFsXG4gICAgICAmJiAhdGhpcy5wYXVzZWRcbiAgICAgICYmICh0aGlzLmludGVydmFsID0gc2V0SW50ZXJ2YWwoJC5wcm94eSh0aGlzLm5leHQsIHRoaXMpLCB0aGlzLm9wdGlvbnMuaW50ZXJ2YWwpKVxuXG4gICAgcmV0dXJuIHRoaXNcbiAgfVxuXG4gIENhcm91c2VsLnByb3RvdHlwZS5nZXRJdGVtSW5kZXggPSBmdW5jdGlvbiAoaXRlbSkge1xuICAgIHRoaXMuJGl0ZW1zID0gaXRlbS5wYXJlbnQoKS5jaGlsZHJlbignLml0ZW0nKVxuICAgIHJldHVybiB0aGlzLiRpdGVtcy5pbmRleChpdGVtIHx8IHRoaXMuJGFjdGl2ZSlcbiAgfVxuXG4gIENhcm91c2VsLnByb3RvdHlwZS5nZXRJdGVtRm9yRGlyZWN0aW9uID0gZnVuY3Rpb24gKGRpcmVjdGlvbiwgYWN0aXZlKSB7XG4gICAgdmFyIGFjdGl2ZUluZGV4ID0gdGhpcy5nZXRJdGVtSW5kZXgoYWN0aXZlKVxuICAgIHZhciB3aWxsV3JhcCA9IChkaXJlY3Rpb24gPT0gJ3ByZXYnICYmIGFjdGl2ZUluZGV4ID09PSAwKVxuICAgICAgICAgICAgICAgIHx8IChkaXJlY3Rpb24gPT0gJ25leHQnICYmIGFjdGl2ZUluZGV4ID09ICh0aGlzLiRpdGVtcy5sZW5ndGggLSAxKSlcbiAgICBpZiAod2lsbFdyYXAgJiYgIXRoaXMub3B0aW9ucy53cmFwKSByZXR1cm4gYWN0aXZlXG4gICAgdmFyIGRlbHRhID0gZGlyZWN0aW9uID09ICdwcmV2JyA/IC0xIDogMVxuICAgIHZhciBpdGVtSW5kZXggPSAoYWN0aXZlSW5kZXggKyBkZWx0YSkgJSB0aGlzLiRpdGVtcy5sZW5ndGhcbiAgICByZXR1cm4gdGhpcy4kaXRlbXMuZXEoaXRlbUluZGV4KVxuICB9XG5cbiAgQ2Fyb3VzZWwucHJvdG90eXBlLnRvID0gZnVuY3Rpb24gKHBvcykge1xuICAgIHZhciB0aGF0ICAgICAgICA9IHRoaXNcbiAgICB2YXIgYWN0aXZlSW5kZXggPSB0aGlzLmdldEl0ZW1JbmRleCh0aGlzLiRhY3RpdmUgPSB0aGlzLiRlbGVtZW50LmZpbmQoJy5pdGVtLmFjdGl2ZScpKVxuXG4gICAgaWYgKHBvcyA+ICh0aGlzLiRpdGVtcy5sZW5ndGggLSAxKSB8fCBwb3MgPCAwKSByZXR1cm5cblxuICAgIGlmICh0aGlzLnNsaWRpbmcpICAgICAgIHJldHVybiB0aGlzLiRlbGVtZW50Lm9uZSgnc2xpZC5icy5jYXJvdXNlbCcsIGZ1bmN0aW9uICgpIHsgdGhhdC50byhwb3MpIH0pIC8vIHllcywgXCJzbGlkXCJcbiAgICBpZiAoYWN0aXZlSW5kZXggPT0gcG9zKSByZXR1cm4gdGhpcy5wYXVzZSgpLmN5Y2xlKClcblxuICAgIHJldHVybiB0aGlzLnNsaWRlKHBvcyA+IGFjdGl2ZUluZGV4ID8gJ25leHQnIDogJ3ByZXYnLCB0aGlzLiRpdGVtcy5lcShwb3MpKVxuICB9XG5cbiAgQ2Fyb3VzZWwucHJvdG90eXBlLnBhdXNlID0gZnVuY3Rpb24gKGUpIHtcbiAgICBlIHx8ICh0aGlzLnBhdXNlZCA9IHRydWUpXG5cbiAgICBpZiAodGhpcy4kZWxlbWVudC5maW5kKCcubmV4dCwgLnByZXYnKS5sZW5ndGggJiYgJC5zdXBwb3J0LnRyYW5zaXRpb24pIHtcbiAgICAgIHRoaXMuJGVsZW1lbnQudHJpZ2dlcigkLnN1cHBvcnQudHJhbnNpdGlvbi5lbmQpXG4gICAgICB0aGlzLmN5Y2xlKHRydWUpXG4gICAgfVxuXG4gICAgdGhpcy5pbnRlcnZhbCA9IGNsZWFySW50ZXJ2YWwodGhpcy5pbnRlcnZhbClcblxuICAgIHJldHVybiB0aGlzXG4gIH1cblxuICBDYXJvdXNlbC5wcm90b3R5cGUubmV4dCA9IGZ1bmN0aW9uICgpIHtcbiAgICBpZiAodGhpcy5zbGlkaW5nKSByZXR1cm5cbiAgICByZXR1cm4gdGhpcy5zbGlkZSgnbmV4dCcpXG4gIH1cblxuICBDYXJvdXNlbC5wcm90b3R5cGUucHJldiA9IGZ1bmN0aW9uICgpIHtcbiAgICBpZiAodGhpcy5zbGlkaW5nKSByZXR1cm5cbiAgICByZXR1cm4gdGhpcy5zbGlkZSgncHJldicpXG4gIH1cblxuICBDYXJvdXNlbC5wcm90b3R5cGUuc2xpZGUgPSBmdW5jdGlvbiAodHlwZSwgbmV4dCkge1xuICAgIHZhciAkYWN0aXZlICAgPSB0aGlzLiRlbGVtZW50LmZpbmQoJy5pdGVtLmFjdGl2ZScpXG4gICAgdmFyICRuZXh0ICAgICA9IG5leHQgfHwgdGhpcy5nZXRJdGVtRm9yRGlyZWN0aW9uKHR5cGUsICRhY3RpdmUpXG4gICAgdmFyIGlzQ3ljbGluZyA9IHRoaXMuaW50ZXJ2YWxcbiAgICB2YXIgZGlyZWN0aW9uID0gdHlwZSA9PSAnbmV4dCcgPyAnbGVmdCcgOiAncmlnaHQnXG4gICAgdmFyIHRoYXQgICAgICA9IHRoaXNcblxuICAgIGlmICgkbmV4dC5oYXNDbGFzcygnYWN0aXZlJykpIHJldHVybiAodGhpcy5zbGlkaW5nID0gZmFsc2UpXG5cbiAgICB2YXIgcmVsYXRlZFRhcmdldCA9ICRuZXh0WzBdXG4gICAgdmFyIHNsaWRlRXZlbnQgPSAkLkV2ZW50KCdzbGlkZS5icy5jYXJvdXNlbCcsIHtcbiAgICAgIHJlbGF0ZWRUYXJnZXQ6IHJlbGF0ZWRUYXJnZXQsXG4gICAgICBkaXJlY3Rpb246IGRpcmVjdGlvblxuICAgIH0pXG4gICAgdGhpcy4kZWxlbWVudC50cmlnZ2VyKHNsaWRlRXZlbnQpXG4gICAgaWYgKHNsaWRlRXZlbnQuaXNEZWZhdWx0UHJldmVudGVkKCkpIHJldHVyblxuXG4gICAgdGhpcy5zbGlkaW5nID0gdHJ1ZVxuXG4gICAgaXNDeWNsaW5nICYmIHRoaXMucGF1c2UoKVxuXG4gICAgaWYgKHRoaXMuJGluZGljYXRvcnMubGVuZ3RoKSB7XG4gICAgICB0aGlzLiRpbmRpY2F0b3JzLmZpbmQoJy5hY3RpdmUnKS5yZW1vdmVDbGFzcygnYWN0aXZlJylcbiAgICAgIHZhciAkbmV4dEluZGljYXRvciA9ICQodGhpcy4kaW5kaWNhdG9ycy5jaGlsZHJlbigpW3RoaXMuZ2V0SXRlbUluZGV4KCRuZXh0KV0pXG4gICAgICAkbmV4dEluZGljYXRvciAmJiAkbmV4dEluZGljYXRvci5hZGRDbGFzcygnYWN0aXZlJylcbiAgICB9XG5cbiAgICB2YXIgc2xpZEV2ZW50ID0gJC5FdmVudCgnc2xpZC5icy5jYXJvdXNlbCcsIHsgcmVsYXRlZFRhcmdldDogcmVsYXRlZFRhcmdldCwgZGlyZWN0aW9uOiBkaXJlY3Rpb24gfSkgLy8geWVzLCBcInNsaWRcIlxuICAgIGlmICgkLnN1cHBvcnQudHJhbnNpdGlvbiAmJiB0aGlzLiRlbGVtZW50Lmhhc0NsYXNzKCdzbGlkZScpKSB7XG4gICAgICAkbmV4dC5hZGRDbGFzcyh0eXBlKVxuICAgICAgaWYgKHR5cGVvZiAkbmV4dCA9PT0gJ29iamVjdCcgJiYgJG5leHQubGVuZ3RoKSB7XG4gICAgICAgICRuZXh0WzBdLm9mZnNldFdpZHRoIC8vIGZvcmNlIHJlZmxvd1xuICAgICAgfVxuICAgICAgJGFjdGl2ZS5hZGRDbGFzcyhkaXJlY3Rpb24pXG4gICAgICAkbmV4dC5hZGRDbGFzcyhkaXJlY3Rpb24pXG4gICAgICAkYWN0aXZlXG4gICAgICAgIC5vbmUoJ2JzVHJhbnNpdGlvbkVuZCcsIGZ1bmN0aW9uICgpIHtcbiAgICAgICAgICAkbmV4dC5yZW1vdmVDbGFzcyhbdHlwZSwgZGlyZWN0aW9uXS5qb2luKCcgJykpLmFkZENsYXNzKCdhY3RpdmUnKVxuICAgICAgICAgICRhY3RpdmUucmVtb3ZlQ2xhc3MoWydhY3RpdmUnLCBkaXJlY3Rpb25dLmpvaW4oJyAnKSlcbiAgICAgICAgICB0aGF0LnNsaWRpbmcgPSBmYWxzZVxuICAgICAgICAgIHNldFRpbWVvdXQoZnVuY3Rpb24gKCkge1xuICAgICAgICAgICAgdGhhdC4kZWxlbWVudC50cmlnZ2VyKHNsaWRFdmVudClcbiAgICAgICAgICB9LCAwKVxuICAgICAgICB9KVxuICAgICAgICAuZW11bGF0ZVRyYW5zaXRpb25FbmQoQ2Fyb3VzZWwuVFJBTlNJVElPTl9EVVJBVElPTilcbiAgICB9IGVsc2Uge1xuICAgICAgJGFjdGl2ZS5yZW1vdmVDbGFzcygnYWN0aXZlJylcbiAgICAgICRuZXh0LmFkZENsYXNzKCdhY3RpdmUnKVxuICAgICAgdGhpcy5zbGlkaW5nID0gZmFsc2VcbiAgICAgIHRoaXMuJGVsZW1lbnQudHJpZ2dlcihzbGlkRXZlbnQpXG4gICAgfVxuXG4gICAgaXNDeWNsaW5nICYmIHRoaXMuY3ljbGUoKVxuXG4gICAgcmV0dXJuIHRoaXNcbiAgfVxuXG5cbiAgLy8gQ0FST1VTRUwgUExVR0lOIERFRklOSVRJT05cbiAgLy8gPT09PT09PT09PT09PT09PT09PT09PT09PT1cblxuICBmdW5jdGlvbiBQbHVnaW4ob3B0aW9uKSB7XG4gICAgcmV0dXJuIHRoaXMuZWFjaChmdW5jdGlvbiAoKSB7XG4gICAgICB2YXIgJHRoaXMgICA9ICQodGhpcylcbiAgICAgIHZhciBkYXRhICAgID0gJHRoaXMuZGF0YSgnYnMuY2Fyb3VzZWwnKVxuICAgICAgdmFyIG9wdGlvbnMgPSAkLmV4dGVuZCh7fSwgQ2Fyb3VzZWwuREVGQVVMVFMsICR0aGlzLmRhdGEoKSwgdHlwZW9mIG9wdGlvbiA9PSAnb2JqZWN0JyAmJiBvcHRpb24pXG4gICAgICB2YXIgYWN0aW9uICA9IHR5cGVvZiBvcHRpb24gPT0gJ3N0cmluZycgPyBvcHRpb24gOiBvcHRpb25zLnNsaWRlXG5cbiAgICAgIGlmICghZGF0YSkgJHRoaXMuZGF0YSgnYnMuY2Fyb3VzZWwnLCAoZGF0YSA9IG5ldyBDYXJvdXNlbCh0aGlzLCBvcHRpb25zKSkpXG4gICAgICBpZiAodHlwZW9mIG9wdGlvbiA9PSAnbnVtYmVyJykgZGF0YS50byhvcHRpb24pXG4gICAgICBlbHNlIGlmIChhY3Rpb24pIGRhdGFbYWN0aW9uXSgpXG4gICAgICBlbHNlIGlmIChvcHRpb25zLmludGVydmFsKSBkYXRhLnBhdXNlKCkuY3ljbGUoKVxuICAgIH0pXG4gIH1cblxuICB2YXIgb2xkID0gJC5mbi5jYXJvdXNlbFxuXG4gICQuZm4uY2Fyb3VzZWwgICAgICAgICAgICAgPSBQbHVnaW5cbiAgJC5mbi5jYXJvdXNlbC5Db25zdHJ1Y3RvciA9IENhcm91c2VsXG5cblxuICAvLyBDQVJPVVNFTCBOTyBDT05GTElDVFxuICAvLyA9PT09PT09PT09PT09PT09PT09PVxuXG4gICQuZm4uY2Fyb3VzZWwubm9Db25mbGljdCA9IGZ1bmN0aW9uICgpIHtcbiAgICAkLmZuLmNhcm91c2VsID0gb2xkXG4gICAgcmV0dXJuIHRoaXNcbiAgfVxuXG5cbiAgLy8gQ0FST1VTRUwgREFUQS1BUElcbiAgLy8gPT09PT09PT09PT09PT09PT1cblxuICB2YXIgY2xpY2tIYW5kbGVyID0gZnVuY3Rpb24gKGUpIHtcbiAgICB2YXIgJHRoaXMgICA9ICQodGhpcylcbiAgICB2YXIgaHJlZiAgICA9ICR0aGlzLmF0dHIoJ2hyZWYnKVxuICAgIGlmIChocmVmKSB7XG4gICAgICBocmVmID0gaHJlZi5yZXBsYWNlKC8uKig/PSNbXlxcc10rJCkvLCAnJykgLy8gc3RyaXAgZm9yIGllN1xuICAgIH1cblxuICAgIHZhciB0YXJnZXQgID0gJHRoaXMuYXR0cignZGF0YS10YXJnZXQnKSB8fCBocmVmXG4gICAgdmFyICR0YXJnZXQgPSAkKGRvY3VtZW50KS5maW5kKHRhcmdldClcblxuICAgIGlmICghJHRhcmdldC5oYXNDbGFzcygnY2Fyb3VzZWwnKSkgcmV0dXJuXG5cbiAgICB2YXIgb3B0aW9ucyA9ICQuZXh0ZW5kKHt9LCAkdGFyZ2V0LmRhdGEoKSwgJHRoaXMuZGF0YSgpKVxuICAgIHZhciBzbGlkZUluZGV4ID0gJHRoaXMuYXR0cignZGF0YS1zbGlkZS10bycpXG4gICAgaWYgKHNsaWRlSW5kZXgpIG9wdGlvbnMuaW50ZXJ2YWwgPSBmYWxzZVxuXG4gICAgUGx1Z2luLmNhbGwoJHRhcmdldCwgb3B0aW9ucylcblxuICAgIGlmIChzbGlkZUluZGV4KSB7XG4gICAgICAkdGFyZ2V0LmRhdGEoJ2JzLmNhcm91c2VsJykudG8oc2xpZGVJbmRleClcbiAgICB9XG5cbiAgICBlLnByZXZlbnREZWZhdWx0KClcbiAgfVxuXG4gICQoZG9jdW1lbnQpXG4gICAgLm9uKCdjbGljay5icy5jYXJvdXNlbC5kYXRhLWFwaScsICdbZGF0YS1zbGlkZV0nLCBjbGlja0hhbmRsZXIpXG4gICAgLm9uKCdjbGljay5icy5jYXJvdXNlbC5kYXRhLWFwaScsICdbZGF0YS1zbGlkZS10b10nLCBjbGlja0hhbmRsZXIpXG5cbiAgJCh3aW5kb3cpLm9uKCdsb2FkJywgZnVuY3Rpb24gKCkge1xuICAgICQoJ1tkYXRhLXJpZGU9XCJjYXJvdXNlbFwiXScpLmVhY2goZnVuY3Rpb24gKCkge1xuICAgICAgdmFyICRjYXJvdXNlbCA9ICQodGhpcylcbiAgICAgIFBsdWdpbi5jYWxsKCRjYXJvdXNlbCwgJGNhcm91c2VsLmRhdGEoKSlcbiAgICB9KVxuICB9KVxuXG59KGpRdWVyeSk7XG5cbi8qID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PVxuICogQm9vdHN0cmFwOiBjb2xsYXBzZS5qcyB2My40LjFcbiAqIGh0dHBzOi8vZ2V0Ym9vdHN0cmFwLmNvbS9kb2NzLzMuNC9qYXZhc2NyaXB0LyNjb2xsYXBzZVxuICogPT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09XG4gKiBDb3B5cmlnaHQgMjAxMS0yMDE5IFR3aXR0ZXIsIEluYy5cbiAqIExpY2Vuc2VkIHVuZGVyIE1JVCAoaHR0cHM6Ly9naXRodWIuY29tL3R3YnMvYm9vdHN0cmFwL2Jsb2IvbWFzdGVyL0xJQ0VOU0UpXG4gKiA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0gKi9cblxuLyoganNoaW50IGxhdGVkZWY6IGZhbHNlICovXG5cbitmdW5jdGlvbiAoJCkge1xuICAndXNlIHN0cmljdCc7XG5cbiAgLy8gQ09MTEFQU0UgUFVCTElDIENMQVNTIERFRklOSVRJT05cbiAgLy8gPT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT1cblxuICB2YXIgQ29sbGFwc2UgPSBmdW5jdGlvbiAoZWxlbWVudCwgb3B0aW9ucykge1xuICAgIHRoaXMuJGVsZW1lbnQgICAgICA9ICQoZWxlbWVudClcbiAgICB0aGlzLm9wdGlvbnMgICAgICAgPSAkLmV4dGVuZCh7fSwgQ29sbGFwc2UuREVGQVVMVFMsIG9wdGlvbnMpXG4gICAgdGhpcy4kdHJpZ2dlciAgICAgID0gJCgnW2RhdGEtdG9nZ2xlPVwiY29sbGFwc2VcIl1baHJlZj1cIiMnICsgZWxlbWVudC5pZCArICdcIl0sJyArXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAnW2RhdGEtdG9nZ2xlPVwiY29sbGFwc2VcIl1bZGF0YS10YXJnZXQ9XCIjJyArIGVsZW1lbnQuaWQgKyAnXCJdJylcbiAgICB0aGlzLnRyYW5zaXRpb25pbmcgPSBudWxsXG5cbiAgICBpZiAodGhpcy5vcHRpb25zLnBhcmVudCkge1xuICAgICAgdGhpcy4kcGFyZW50ID0gdGhpcy5nZXRQYXJlbnQoKVxuICAgIH0gZWxzZSB7XG4gICAgICB0aGlzLmFkZEFyaWFBbmRDb2xsYXBzZWRDbGFzcyh0aGlzLiRlbGVtZW50LCB0aGlzLiR0cmlnZ2VyKVxuICAgIH1cblxuICAgIGlmICh0aGlzLm9wdGlvbnMudG9nZ2xlKSB0aGlzLnRvZ2dsZSgpXG4gIH1cblxuICBDb2xsYXBzZS5WRVJTSU9OICA9ICczLjQuMSdcblxuICBDb2xsYXBzZS5UUkFOU0lUSU9OX0RVUkFUSU9OID0gMzUwXG5cbiAgQ29sbGFwc2UuREVGQVVMVFMgPSB7XG4gICAgdG9nZ2xlOiB0cnVlXG4gIH1cblxuICBDb2xsYXBzZS5wcm90b3R5cGUuZGltZW5zaW9uID0gZnVuY3Rpb24gKCkge1xuICAgIHZhciBoYXNXaWR0aCA9IHRoaXMuJGVsZW1lbnQuaGFzQ2xhc3MoJ3dpZHRoJylcbiAgICByZXR1cm4gaGFzV2lkdGggPyAnd2lkdGgnIDogJ2hlaWdodCdcbiAgfVxuXG4gIENvbGxhcHNlLnByb3RvdHlwZS5zaG93ID0gZnVuY3Rpb24gKCkge1xuICAgIGlmICh0aGlzLnRyYW5zaXRpb25pbmcgfHwgdGhpcy4kZWxlbWVudC5oYXNDbGFzcygnaW4nKSkgcmV0dXJuXG5cbiAgICB2YXIgYWN0aXZlc0RhdGFcbiAgICB2YXIgYWN0aXZlcyA9IHRoaXMuJHBhcmVudCAmJiB0aGlzLiRwYXJlbnQuY2hpbGRyZW4oJy5wYW5lbCcpLmNoaWxkcmVuKCcuaW4sIC5jb2xsYXBzaW5nJylcblxuICAgIGlmIChhY3RpdmVzICYmIGFjdGl2ZXMubGVuZ3RoKSB7XG4gICAgICBhY3RpdmVzRGF0YSA9IGFjdGl2ZXMuZGF0YSgnYnMuY29sbGFwc2UnKVxuICAgICAgaWYgKGFjdGl2ZXNEYXRhICYmIGFjdGl2ZXNEYXRhLnRyYW5zaXRpb25pbmcpIHJldHVyblxuICAgIH1cblxuICAgIHZhciBzdGFydEV2ZW50ID0gJC5FdmVudCgnc2hvdy5icy5jb2xsYXBzZScpXG4gICAgdGhpcy4kZWxlbWVudC50cmlnZ2VyKHN0YXJ0RXZlbnQpXG4gICAgaWYgKHN0YXJ0RXZlbnQuaXNEZWZhdWx0UHJldmVudGVkKCkpIHJldHVyblxuXG4gICAgaWYgKGFjdGl2ZXMgJiYgYWN0aXZlcy5sZW5ndGgpIHtcbiAgICAgIFBsdWdpbi5jYWxsKGFjdGl2ZXMsICdoaWRlJylcbiAgICAgIGFjdGl2ZXNEYXRhIHx8IGFjdGl2ZXMuZGF0YSgnYnMuY29sbGFwc2UnLCBudWxsKVxuICAgIH1cblxuICAgIHZhciBkaW1lbnNpb24gPSB0aGlzLmRpbWVuc2lvbigpXG5cbiAgICB0aGlzLiRlbGVtZW50XG4gICAgICAucmVtb3ZlQ2xhc3MoJ2NvbGxhcHNlJylcbiAgICAgIC5hZGRDbGFzcygnY29sbGFwc2luZycpW2RpbWVuc2lvbl0oMClcbiAgICAgIC5hdHRyKCdhcmlhLWV4cGFuZGVkJywgdHJ1ZSlcblxuICAgIHRoaXMuJHRyaWdnZXJcbiAgICAgIC5yZW1vdmVDbGFzcygnY29sbGFwc2VkJylcbiAgICAgIC5hdHRyKCdhcmlhLWV4cGFuZGVkJywgdHJ1ZSlcblxuICAgIHRoaXMudHJhbnNpdGlvbmluZyA9IDFcblxuICAgIHZhciBjb21wbGV0ZSA9IGZ1bmN0aW9uICgpIHtcbiAgICAgIHRoaXMuJGVsZW1lbnRcbiAgICAgICAgLnJlbW92ZUNsYXNzKCdjb2xsYXBzaW5nJylcbiAgICAgICAgLmFkZENsYXNzKCdjb2xsYXBzZSBpbicpW2RpbWVuc2lvbl0oJycpXG4gICAgICB0aGlzLnRyYW5zaXRpb25pbmcgPSAwXG4gICAgICB0aGlzLiRlbGVtZW50XG4gICAgICAgIC50cmlnZ2VyKCdzaG93bi5icy5jb2xsYXBzZScpXG4gICAgfVxuXG4gICAgaWYgKCEkLnN1cHBvcnQudHJhbnNpdGlvbikgcmV0dXJuIGNvbXBsZXRlLmNhbGwodGhpcylcblxuICAgIHZhciBzY3JvbGxTaXplID0gJC5jYW1lbENhc2UoWydzY3JvbGwnLCBkaW1lbnNpb25dLmpvaW4oJy0nKSlcblxuICAgIHRoaXMuJGVsZW1lbnRcbiAgICAgIC5vbmUoJ2JzVHJhbnNpdGlvbkVuZCcsICQucHJveHkoY29tcGxldGUsIHRoaXMpKVxuICAgICAgLmVtdWxhdGVUcmFuc2l0aW9uRW5kKENvbGxhcHNlLlRSQU5TSVRJT05fRFVSQVRJT04pW2RpbWVuc2lvbl0odGhpcy4kZWxlbWVudFswXVtzY3JvbGxTaXplXSlcbiAgfVxuXG4gIENvbGxhcHNlLnByb3RvdHlwZS5oaWRlID0gZnVuY3Rpb24gKCkge1xuICAgIGlmICh0aGlzLnRyYW5zaXRpb25pbmcgfHwgIXRoaXMuJGVsZW1lbnQuaGFzQ2xhc3MoJ2luJykpIHJldHVyblxuXG4gICAgdmFyIHN0YXJ0RXZlbnQgPSAkLkV2ZW50KCdoaWRlLmJzLmNvbGxhcHNlJylcbiAgICB0aGlzLiRlbGVtZW50LnRyaWdnZXIoc3RhcnRFdmVudClcbiAgICBpZiAoc3RhcnRFdmVudC5pc0RlZmF1bHRQcmV2ZW50ZWQoKSkgcmV0dXJuXG5cbiAgICB2YXIgZGltZW5zaW9uID0gdGhpcy5kaW1lbnNpb24oKVxuXG4gICAgdGhpcy4kZWxlbWVudFtkaW1lbnNpb25dKHRoaXMuJGVsZW1lbnRbZGltZW5zaW9uXSgpKVswXS5vZmZzZXRIZWlnaHRcblxuICAgIHRoaXMuJGVsZW1lbnRcbiAgICAgIC5hZGRDbGFzcygnY29sbGFwc2luZycpXG4gICAgICAucmVtb3ZlQ2xhc3MoJ2NvbGxhcHNlIGluJylcbiAgICAgIC5hdHRyKCdhcmlhLWV4cGFuZGVkJywgZmFsc2UpXG5cbiAgICB0aGlzLiR0cmlnZ2VyXG4gICAgICAuYWRkQ2xhc3MoJ2NvbGxhcHNlZCcpXG4gICAgICAuYXR0cignYXJpYS1leHBhbmRlZCcsIGZhbHNlKVxuXG4gICAgdGhpcy50cmFuc2l0aW9uaW5nID0gMVxuXG4gICAgdmFyIGNvbXBsZXRlID0gZnVuY3Rpb24gKCkge1xuICAgICAgdGhpcy50cmFuc2l0aW9uaW5nID0gMFxuICAgICAgdGhpcy4kZWxlbWVudFxuICAgICAgICAucmVtb3ZlQ2xhc3MoJ2NvbGxhcHNpbmcnKVxuICAgICAgICAuYWRkQ2xhc3MoJ2NvbGxhcHNlJylcbiAgICAgICAgLnRyaWdnZXIoJ2hpZGRlbi5icy5jb2xsYXBzZScpXG4gICAgfVxuXG4gICAgaWYgKCEkLnN1cHBvcnQudHJhbnNpdGlvbikgcmV0dXJuIGNvbXBsZXRlLmNhbGwodGhpcylcblxuICAgIHRoaXMuJGVsZW1lbnRcbiAgICAgIFtkaW1lbnNpb25dKDApXG4gICAgICAub25lKCdic1RyYW5zaXRpb25FbmQnLCAkLnByb3h5KGNvbXBsZXRlLCB0aGlzKSlcbiAgICAgIC5lbXVsYXRlVHJhbnNpdGlvbkVuZChDb2xsYXBzZS5UUkFOU0lUSU9OX0RVUkFUSU9OKVxuICB9XG5cbiAgQ29sbGFwc2UucHJvdG90eXBlLnRvZ2dsZSA9IGZ1bmN0aW9uICgpIHtcbiAgICB0aGlzW3RoaXMuJGVsZW1lbnQuaGFzQ2xhc3MoJ2luJykgPyAnaGlkZScgOiAnc2hvdyddKClcbiAgfVxuXG4gIENvbGxhcHNlLnByb3RvdHlwZS5nZXRQYXJlbnQgPSBmdW5jdGlvbiAoKSB7XG4gICAgcmV0dXJuICQoZG9jdW1lbnQpLmZpbmQodGhpcy5vcHRpb25zLnBhcmVudClcbiAgICAgIC5maW5kKCdbZGF0YS10b2dnbGU9XCJjb2xsYXBzZVwiXVtkYXRhLXBhcmVudD1cIicgKyB0aGlzLm9wdGlvbnMucGFyZW50ICsgJ1wiXScpXG4gICAgICAuZWFjaCgkLnByb3h5KGZ1bmN0aW9uIChpLCBlbGVtZW50KSB7XG4gICAgICAgIHZhciAkZWxlbWVudCA9ICQoZWxlbWVudClcbiAgICAgICAgdGhpcy5hZGRBcmlhQW5kQ29sbGFwc2VkQ2xhc3MoZ2V0VGFyZ2V0RnJvbVRyaWdnZXIoJGVsZW1lbnQpLCAkZWxlbWVudClcbiAgICAgIH0sIHRoaXMpKVxuICAgICAgLmVuZCgpXG4gIH1cblxuICBDb2xsYXBzZS5wcm90b3R5cGUuYWRkQXJpYUFuZENvbGxhcHNlZENsYXNzID0gZnVuY3Rpb24gKCRlbGVtZW50LCAkdHJpZ2dlcikge1xuICAgIHZhciBpc09wZW4gPSAkZWxlbWVudC5oYXNDbGFzcygnaW4nKVxuXG4gICAgJGVsZW1lbnQuYXR0cignYXJpYS1leHBhbmRlZCcsIGlzT3BlbilcbiAgICAkdHJpZ2dlclxuICAgICAgLnRvZ2dsZUNsYXNzKCdjb2xsYXBzZWQnLCAhaXNPcGVuKVxuICAgICAgLmF0dHIoJ2FyaWEtZXhwYW5kZWQnLCBpc09wZW4pXG4gIH1cblxuICBmdW5jdGlvbiBnZXRUYXJnZXRGcm9tVHJpZ2dlcigkdHJpZ2dlcikge1xuICAgIHZhciBocmVmXG4gICAgdmFyIHRhcmdldCA9ICR0cmlnZ2VyLmF0dHIoJ2RhdGEtdGFyZ2V0JylcbiAgICAgIHx8IChocmVmID0gJHRyaWdnZXIuYXR0cignaHJlZicpKSAmJiBocmVmLnJlcGxhY2UoLy4qKD89I1teXFxzXSskKS8sICcnKSAvLyBzdHJpcCBmb3IgaWU3XG5cbiAgICByZXR1cm4gJChkb2N1bWVudCkuZmluZCh0YXJnZXQpXG4gIH1cblxuXG4gIC8vIENPTExBUFNFIFBMVUdJTiBERUZJTklUSU9OXG4gIC8vID09PT09PT09PT09PT09PT09PT09PT09PT09XG5cbiAgZnVuY3Rpb24gUGx1Z2luKG9wdGlvbikge1xuICAgIHJldHVybiB0aGlzLmVhY2goZnVuY3Rpb24gKCkge1xuICAgICAgdmFyICR0aGlzICAgPSAkKHRoaXMpXG4gICAgICB2YXIgZGF0YSAgICA9ICR0aGlzLmRhdGEoJ2JzLmNvbGxhcHNlJylcbiAgICAgIHZhciBvcHRpb25zID0gJC5leHRlbmQoe30sIENvbGxhcHNlLkRFRkFVTFRTLCAkdGhpcy5kYXRhKCksIHR5cGVvZiBvcHRpb24gPT0gJ29iamVjdCcgJiYgb3B0aW9uKVxuXG4gICAgICBpZiAoIWRhdGEgJiYgb3B0aW9ucy50b2dnbGUgJiYgL3Nob3d8aGlkZS8udGVzdChvcHRpb24pKSBvcHRpb25zLnRvZ2dsZSA9IGZhbHNlXG4gICAgICBpZiAoIWRhdGEpICR0aGlzLmRhdGEoJ2JzLmNvbGxhcHNlJywgKGRhdGEgPSBuZXcgQ29sbGFwc2UodGhpcywgb3B0aW9ucykpKVxuICAgICAgaWYgKHR5cGVvZiBvcHRpb24gPT0gJ3N0cmluZycpIGRhdGFbb3B0aW9uXSgpXG4gICAgfSlcbiAgfVxuXG4gIHZhciBvbGQgPSAkLmZuLmNvbGxhcHNlXG5cbiAgJC5mbi5jb2xsYXBzZSAgICAgICAgICAgICA9IFBsdWdpblxuICAkLmZuLmNvbGxhcHNlLkNvbnN0cnVjdG9yID0gQ29sbGFwc2VcblxuXG4gIC8vIENPTExBUFNFIE5PIENPTkZMSUNUXG4gIC8vID09PT09PT09PT09PT09PT09PT09XG5cbiAgJC5mbi5jb2xsYXBzZS5ub0NvbmZsaWN0ID0gZnVuY3Rpb24gKCkge1xuICAgICQuZm4uY29sbGFwc2UgPSBvbGRcbiAgICByZXR1cm4gdGhpc1xuICB9XG5cblxuICAvLyBDT0xMQVBTRSBEQVRBLUFQSVxuICAvLyA9PT09PT09PT09PT09PT09PVxuXG4gICQoZG9jdW1lbnQpLm9uKCdjbGljay5icy5jb2xsYXBzZS5kYXRhLWFwaScsICdbZGF0YS10b2dnbGU9XCJjb2xsYXBzZVwiXScsIGZ1bmN0aW9uIChlKSB7XG4gICAgdmFyICR0aGlzICAgPSAkKHRoaXMpXG5cbiAgICBpZiAoISR0aGlzLmF0dHIoJ2RhdGEtdGFyZ2V0JykpIGUucHJldmVudERlZmF1bHQoKVxuXG4gICAgdmFyICR0YXJnZXQgPSBnZXRUYXJnZXRGcm9tVHJpZ2dlcigkdGhpcylcbiAgICB2YXIgZGF0YSAgICA9ICR0YXJnZXQuZGF0YSgnYnMuY29sbGFwc2UnKVxuICAgIHZhciBvcHRpb24gID0gZGF0YSA/ICd0b2dnbGUnIDogJHRoaXMuZGF0YSgpXG5cbiAgICBQbHVnaW4uY2FsbCgkdGFyZ2V0LCBvcHRpb24pXG4gIH0pXG5cbn0oalF1ZXJ5KTtcblxuLyogPT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09XG4gKiBCb290c3RyYXA6IGRyb3Bkb3duLmpzIHYzLjQuMVxuICogaHR0cHM6Ly9nZXRib290c3RyYXAuY29tL2RvY3MvMy40L2phdmFzY3JpcHQvI2Ryb3Bkb3duc1xuICogPT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09XG4gKiBDb3B5cmlnaHQgMjAxMS0yMDE5IFR3aXR0ZXIsIEluYy5cbiAqIExpY2Vuc2VkIHVuZGVyIE1JVCAoaHR0cHM6Ly9naXRodWIuY29tL3R3YnMvYm9vdHN0cmFwL2Jsb2IvbWFzdGVyL0xJQ0VOU0UpXG4gKiA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0gKi9cblxuXG4rZnVuY3Rpb24gKCQpIHtcbiAgJ3VzZSBzdHJpY3QnO1xuXG4gIC8vIERST1BET1dOIENMQVNTIERFRklOSVRJT05cbiAgLy8gPT09PT09PT09PT09PT09PT09PT09PT09PVxuXG4gIHZhciBiYWNrZHJvcCA9ICcuZHJvcGRvd24tYmFja2Ryb3AnXG4gIHZhciB0b2dnbGUgICA9ICdbZGF0YS10b2dnbGU9XCJkcm9wZG93blwiXSdcbiAgdmFyIERyb3Bkb3duID0gZnVuY3Rpb24gKGVsZW1lbnQpIHtcbiAgICAkKGVsZW1lbnQpLm9uKCdjbGljay5icy5kcm9wZG93bicsIHRoaXMudG9nZ2xlKVxuICB9XG5cbiAgRHJvcGRvd24uVkVSU0lPTiA9ICczLjQuMSdcblxuICBmdW5jdGlvbiBnZXRQYXJlbnQoJHRoaXMpIHtcbiAgICB2YXIgc2VsZWN0b3IgPSAkdGhpcy5hdHRyKCdkYXRhLXRhcmdldCcpXG5cbiAgICBpZiAoIXNlbGVjdG9yKSB7XG4gICAgICBzZWxlY3RvciA9ICR0aGlzLmF0dHIoJ2hyZWYnKVxuICAgICAgc2VsZWN0b3IgPSBzZWxlY3RvciAmJiAvI1tBLVphLXpdLy50ZXN0KHNlbGVjdG9yKSAmJiBzZWxlY3Rvci5yZXBsYWNlKC8uKig/PSNbXlxcc10qJCkvLCAnJykgLy8gc3RyaXAgZm9yIGllN1xuICAgIH1cblxuICAgIHZhciAkcGFyZW50ID0gc2VsZWN0b3IgIT09ICcjJyA/ICQoZG9jdW1lbnQpLmZpbmQoc2VsZWN0b3IpIDogbnVsbFxuXG4gICAgcmV0dXJuICRwYXJlbnQgJiYgJHBhcmVudC5sZW5ndGggPyAkcGFyZW50IDogJHRoaXMucGFyZW50KClcbiAgfVxuXG4gIGZ1bmN0aW9uIGNsZWFyTWVudXMoZSkge1xuICAgIGlmIChlICYmIGUud2hpY2ggPT09IDMpIHJldHVyblxuICAgICQoYmFja2Ryb3ApLnJlbW92ZSgpXG4gICAgJCh0b2dnbGUpLmVhY2goZnVuY3Rpb24gKCkge1xuICAgICAgdmFyICR0aGlzICAgICAgICAgPSAkKHRoaXMpXG4gICAgICB2YXIgJHBhcmVudCAgICAgICA9IGdldFBhcmVudCgkdGhpcylcbiAgICAgIHZhciByZWxhdGVkVGFyZ2V0ID0geyByZWxhdGVkVGFyZ2V0OiB0aGlzIH1cblxuICAgICAgaWYgKCEkcGFyZW50Lmhhc0NsYXNzKCdvcGVuJykpIHJldHVyblxuXG4gICAgICBpZiAoZSAmJiBlLnR5cGUgPT0gJ2NsaWNrJyAmJiAvaW5wdXR8dGV4dGFyZWEvaS50ZXN0KGUudGFyZ2V0LnRhZ05hbWUpICYmICQuY29udGFpbnMoJHBhcmVudFswXSwgZS50YXJnZXQpKSByZXR1cm5cblxuICAgICAgJHBhcmVudC50cmlnZ2VyKGUgPSAkLkV2ZW50KCdoaWRlLmJzLmRyb3Bkb3duJywgcmVsYXRlZFRhcmdldCkpXG5cbiAgICAgIGlmIChlLmlzRGVmYXVsdFByZXZlbnRlZCgpKSByZXR1cm5cblxuICAgICAgJHRoaXMuYXR0cignYXJpYS1leHBhbmRlZCcsICdmYWxzZScpXG4gICAgICAkcGFyZW50LnJlbW92ZUNsYXNzKCdvcGVuJykudHJpZ2dlcigkLkV2ZW50KCdoaWRkZW4uYnMuZHJvcGRvd24nLCByZWxhdGVkVGFyZ2V0KSlcbiAgICB9KVxuICB9XG5cbiAgRHJvcGRvd24ucHJvdG90eXBlLnRvZ2dsZSA9IGZ1bmN0aW9uIChlKSB7XG4gICAgdmFyICR0aGlzID0gJCh0aGlzKVxuXG4gICAgaWYgKCR0aGlzLmlzKCcuZGlzYWJsZWQsIDpkaXNhYmxlZCcpKSByZXR1cm5cblxuICAgIHZhciAkcGFyZW50ICA9IGdldFBhcmVudCgkdGhpcylcbiAgICB2YXIgaXNBY3RpdmUgPSAkcGFyZW50Lmhhc0NsYXNzKCdvcGVuJylcblxuICAgIGNsZWFyTWVudXMoKVxuXG4gICAgaWYgKCFpc0FjdGl2ZSkge1xuICAgICAgaWYgKCdvbnRvdWNoc3RhcnQnIGluIGRvY3VtZW50LmRvY3VtZW50RWxlbWVudCAmJiAhJHBhcmVudC5jbG9zZXN0KCcubmF2YmFyLW5hdicpLmxlbmd0aCkge1xuICAgICAgICAvLyBpZiBtb2JpbGUgd2UgdXNlIGEgYmFja2Ryb3AgYmVjYXVzZSBjbGljayBldmVudHMgZG9uJ3QgZGVsZWdhdGVcbiAgICAgICAgJChkb2N1bWVudC5jcmVhdGVFbGVtZW50KCdkaXYnKSlcbiAgICAgICAgICAuYWRkQ2xhc3MoJ2Ryb3Bkb3duLWJhY2tkcm9wJylcbiAgICAgICAgICAuaW5zZXJ0QWZ0ZXIoJCh0aGlzKSlcbiAgICAgICAgICAub24oJ2NsaWNrJywgY2xlYXJNZW51cylcbiAgICAgIH1cblxuICAgICAgdmFyIHJlbGF0ZWRUYXJnZXQgPSB7IHJlbGF0ZWRUYXJnZXQ6IHRoaXMgfVxuICAgICAgJHBhcmVudC50cmlnZ2VyKGUgPSAkLkV2ZW50KCdzaG93LmJzLmRyb3Bkb3duJywgcmVsYXRlZFRhcmdldCkpXG5cbiAgICAgIGlmIChlLmlzRGVmYXVsdFByZXZlbnRlZCgpKSByZXR1cm5cblxuICAgICAgJHRoaXNcbiAgICAgICAgLnRyaWdnZXIoJ2ZvY3VzJylcbiAgICAgICAgLmF0dHIoJ2FyaWEtZXhwYW5kZWQnLCAndHJ1ZScpXG5cbiAgICAgICRwYXJlbnRcbiAgICAgICAgLnRvZ2dsZUNsYXNzKCdvcGVuJylcbiAgICAgICAgLnRyaWdnZXIoJC5FdmVudCgnc2hvd24uYnMuZHJvcGRvd24nLCByZWxhdGVkVGFyZ2V0KSlcbiAgICB9XG5cbiAgICByZXR1cm4gZmFsc2VcbiAgfVxuXG4gIERyb3Bkb3duLnByb3RvdHlwZS5rZXlkb3duID0gZnVuY3Rpb24gKGUpIHtcbiAgICBpZiAoIS8oMzh8NDB8Mjd8MzIpLy50ZXN0KGUud2hpY2gpIHx8IC9pbnB1dHx0ZXh0YXJlYS9pLnRlc3QoZS50YXJnZXQudGFnTmFtZSkpIHJldHVyblxuXG4gICAgdmFyICR0aGlzID0gJCh0aGlzKVxuXG4gICAgZS5wcmV2ZW50RGVmYXVsdCgpXG4gICAgZS5zdG9wUHJvcGFnYXRpb24oKVxuXG4gICAgaWYgKCR0aGlzLmlzKCcuZGlzYWJsZWQsIDpkaXNhYmxlZCcpKSByZXR1cm5cblxuICAgIHZhciAkcGFyZW50ICA9IGdldFBhcmVudCgkdGhpcylcbiAgICB2YXIgaXNBY3RpdmUgPSAkcGFyZW50Lmhhc0NsYXNzKCdvcGVuJylcblxuICAgIGlmICghaXNBY3RpdmUgJiYgZS53aGljaCAhPSAyNyB8fCBpc0FjdGl2ZSAmJiBlLndoaWNoID09IDI3KSB7XG4gICAgICBpZiAoZS53aGljaCA9PSAyNykgJHBhcmVudC5maW5kKHRvZ2dsZSkudHJpZ2dlcignZm9jdXMnKVxuICAgICAgcmV0dXJuICR0aGlzLnRyaWdnZXIoJ2NsaWNrJylcbiAgICB9XG5cbiAgICB2YXIgZGVzYyA9ICcgbGk6bm90KC5kaXNhYmxlZCk6dmlzaWJsZSBhJ1xuICAgIHZhciAkaXRlbXMgPSAkcGFyZW50LmZpbmQoJy5kcm9wZG93bi1tZW51JyArIGRlc2MpXG5cbiAgICBpZiAoISRpdGVtcy5sZW5ndGgpIHJldHVyblxuXG4gICAgdmFyIGluZGV4ID0gJGl0ZW1zLmluZGV4KGUudGFyZ2V0KVxuXG4gICAgaWYgKGUud2hpY2ggPT0gMzggJiYgaW5kZXggPiAwKSAgICAgICAgICAgICAgICAgaW5kZXgtLSAgICAgICAgIC8vIHVwXG4gICAgaWYgKGUud2hpY2ggPT0gNDAgJiYgaW5kZXggPCAkaXRlbXMubGVuZ3RoIC0gMSkgaW5kZXgrKyAgICAgICAgIC8vIGRvd25cbiAgICBpZiAoIX5pbmRleCkgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICBpbmRleCA9IDBcblxuICAgICRpdGVtcy5lcShpbmRleCkudHJpZ2dlcignZm9jdXMnKVxuICB9XG5cblxuICAvLyBEUk9QRE9XTiBQTFVHSU4gREVGSU5JVElPTlxuICAvLyA9PT09PT09PT09PT09PT09PT09PT09PT09PVxuXG4gIGZ1bmN0aW9uIFBsdWdpbihvcHRpb24pIHtcbiAgICByZXR1cm4gdGhpcy5lYWNoKGZ1bmN0aW9uICgpIHtcbiAgICAgIHZhciAkdGhpcyA9ICQodGhpcylcbiAgICAgIHZhciBkYXRhICA9ICR0aGlzLmRhdGEoJ2JzLmRyb3Bkb3duJylcblxuICAgICAgaWYgKCFkYXRhKSAkdGhpcy5kYXRhKCdicy5kcm9wZG93bicsIChkYXRhID0gbmV3IERyb3Bkb3duKHRoaXMpKSlcbiAgICAgIGlmICh0eXBlb2Ygb3B0aW9uID09ICdzdHJpbmcnKSBkYXRhW29wdGlvbl0uY2FsbCgkdGhpcylcbiAgICB9KVxuICB9XG5cbiAgdmFyIG9sZCA9ICQuZm4uZHJvcGRvd25cblxuICAkLmZuLmRyb3Bkb3duICAgICAgICAgICAgID0gUGx1Z2luXG4gICQuZm4uZHJvcGRvd24uQ29uc3RydWN0b3IgPSBEcm9wZG93blxuXG5cbiAgLy8gRFJPUERPV04gTk8gQ09ORkxJQ1RcbiAgLy8gPT09PT09PT09PT09PT09PT09PT1cblxuICAkLmZuLmRyb3Bkb3duLm5vQ29uZmxpY3QgPSBmdW5jdGlvbiAoKSB7XG4gICAgJC5mbi5kcm9wZG93biA9IG9sZFxuICAgIHJldHVybiB0aGlzXG4gIH1cblxuXG4gIC8vIEFQUExZIFRPIFNUQU5EQVJEIERST1BET1dOIEVMRU1FTlRTXG4gIC8vID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09XG5cbiAgJChkb2N1bWVudClcbiAgICAub24oJ2NsaWNrLmJzLmRyb3Bkb3duLmRhdGEtYXBpJywgY2xlYXJNZW51cylcbiAgICAub24oJ2NsaWNrLmJzLmRyb3Bkb3duLmRhdGEtYXBpJywgJy5kcm9wZG93biBmb3JtJywgZnVuY3Rpb24gKGUpIHsgZS5zdG9wUHJvcGFnYXRpb24oKSB9KVxuICAgIC5vbignY2xpY2suYnMuZHJvcGRvd24uZGF0YS1hcGknLCB0b2dnbGUsIERyb3Bkb3duLnByb3RvdHlwZS50b2dnbGUpXG4gICAgLm9uKCdrZXlkb3duLmJzLmRyb3Bkb3duLmRhdGEtYXBpJywgdG9nZ2xlLCBEcm9wZG93bi5wcm90b3R5cGUua2V5ZG93bilcbiAgICAub24oJ2tleWRvd24uYnMuZHJvcGRvd24uZGF0YS1hcGknLCAnLmRyb3Bkb3duLW1lbnUnLCBEcm9wZG93bi5wcm90b3R5cGUua2V5ZG93bilcblxufShqUXVlcnkpO1xuXG4vKiA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT1cbiAqIEJvb3RzdHJhcDogbW9kYWwuanMgdjMuNC4xXG4gKiBodHRwczovL2dldGJvb3RzdHJhcC5jb20vZG9jcy8zLjQvamF2YXNjcmlwdC8jbW9kYWxzXG4gKiA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT1cbiAqIENvcHlyaWdodCAyMDExLTIwMTkgVHdpdHRlciwgSW5jLlxuICogTGljZW5zZWQgdW5kZXIgTUlUIChodHRwczovL2dpdGh1Yi5jb20vdHdicy9ib290c3RyYXAvYmxvYi9tYXN0ZXIvTElDRU5TRSlcbiAqID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PSAqL1xuXG5cbitmdW5jdGlvbiAoJCkge1xuICAndXNlIHN0cmljdCc7XG5cbiAgLy8gTU9EQUwgQ0xBU1MgREVGSU5JVElPTlxuICAvLyA9PT09PT09PT09PT09PT09PT09PT09XG5cbiAgdmFyIE1vZGFsID0gZnVuY3Rpb24gKGVsZW1lbnQsIG9wdGlvbnMpIHtcbiAgICB0aGlzLm9wdGlvbnMgPSBvcHRpb25zXG4gICAgdGhpcy4kYm9keSA9ICQoZG9jdW1lbnQuYm9keSlcbiAgICB0aGlzLiRlbGVtZW50ID0gJChlbGVtZW50KVxuICAgIHRoaXMuJGRpYWxvZyA9IHRoaXMuJGVsZW1lbnQuZmluZCgnLm1vZGFsLWRpYWxvZycpXG4gICAgdGhpcy4kYmFja2Ryb3AgPSBudWxsXG4gICAgdGhpcy5pc1Nob3duID0gbnVsbFxuICAgIHRoaXMub3JpZ2luYWxCb2R5UGFkID0gbnVsbFxuICAgIHRoaXMuc2Nyb2xsYmFyV2lkdGggPSAwXG4gICAgdGhpcy5pZ25vcmVCYWNrZHJvcENsaWNrID0gZmFsc2VcbiAgICB0aGlzLmZpeGVkQ29udGVudCA9ICcubmF2YmFyLWZpeGVkLXRvcCwgLm5hdmJhci1maXhlZC1ib3R0b20nXG5cbiAgICBpZiAodGhpcy5vcHRpb25zLnJlbW90ZSkge1xuICAgICAgdGhpcy4kZWxlbWVudFxuICAgICAgICAuZmluZCgnLm1vZGFsLWNvbnRlbnQnKVxuICAgICAgICAubG9hZCh0aGlzLm9wdGlvbnMucmVtb3RlLCAkLnByb3h5KGZ1bmN0aW9uICgpIHtcbiAgICAgICAgICB0aGlzLiRlbGVtZW50LnRyaWdnZXIoJ2xvYWRlZC5icy5tb2RhbCcpXG4gICAgICAgIH0sIHRoaXMpKVxuICAgIH1cbiAgfVxuXG4gIE1vZGFsLlZFUlNJT04gPSAnMy40LjEnXG5cbiAgTW9kYWwuVFJBTlNJVElPTl9EVVJBVElPTiA9IDMwMFxuICBNb2RhbC5CQUNLRFJPUF9UUkFOU0lUSU9OX0RVUkFUSU9OID0gMTUwXG5cbiAgTW9kYWwuREVGQVVMVFMgPSB7XG4gICAgYmFja2Ryb3A6IHRydWUsXG4gICAga2V5Ym9hcmQ6IHRydWUsXG4gICAgc2hvdzogdHJ1ZVxuICB9XG5cbiAgTW9kYWwucHJvdG90eXBlLnRvZ2dsZSA9IGZ1bmN0aW9uIChfcmVsYXRlZFRhcmdldCkge1xuICAgIHJldHVybiB0aGlzLmlzU2hvd24gPyB0aGlzLmhpZGUoKSA6IHRoaXMuc2hvdyhfcmVsYXRlZFRhcmdldClcbiAgfVxuXG4gIE1vZGFsLnByb3RvdHlwZS5zaG93ID0gZnVuY3Rpb24gKF9yZWxhdGVkVGFyZ2V0KSB7XG4gICAgdmFyIHRoYXQgPSB0aGlzXG4gICAgdmFyIGUgPSAkLkV2ZW50KCdzaG93LmJzLm1vZGFsJywgeyByZWxhdGVkVGFyZ2V0OiBfcmVsYXRlZFRhcmdldCB9KVxuXG4gICAgdGhpcy4kZWxlbWVudC50cmlnZ2VyKGUpXG5cbiAgICBpZiAodGhpcy5pc1Nob3duIHx8IGUuaXNEZWZhdWx0UHJldmVudGVkKCkpIHJldHVyblxuXG4gICAgdGhpcy5pc1Nob3duID0gdHJ1ZVxuXG4gICAgdGhpcy5jaGVja1Njcm9sbGJhcigpXG4gICAgdGhpcy5zZXRTY3JvbGxiYXIoKVxuICAgIHRoaXMuJGJvZHkuYWRkQ2xhc3MoJ21vZGFsLW9wZW4nKVxuXG4gICAgdGhpcy5lc2NhcGUoKVxuICAgIHRoaXMucmVzaXplKClcblxuICAgIHRoaXMuJGVsZW1lbnQub24oJ2NsaWNrLmRpc21pc3MuYnMubW9kYWwnLCAnW2RhdGEtZGlzbWlzcz1cIm1vZGFsXCJdJywgJC5wcm94eSh0aGlzLmhpZGUsIHRoaXMpKVxuXG4gICAgdGhpcy4kZGlhbG9nLm9uKCdtb3VzZWRvd24uZGlzbWlzcy5icy5tb2RhbCcsIGZ1bmN0aW9uICgpIHtcbiAgICAgIHRoYXQuJGVsZW1lbnQub25lKCdtb3VzZXVwLmRpc21pc3MuYnMubW9kYWwnLCBmdW5jdGlvbiAoZSkge1xuICAgICAgICBpZiAoJChlLnRhcmdldCkuaXModGhhdC4kZWxlbWVudCkpIHRoYXQuaWdub3JlQmFja2Ryb3BDbGljayA9IHRydWVcbiAgICAgIH0pXG4gICAgfSlcblxuICAgIHRoaXMuYmFja2Ryb3AoZnVuY3Rpb24gKCkge1xuICAgICAgdmFyIHRyYW5zaXRpb24gPSAkLnN1cHBvcnQudHJhbnNpdGlvbiAmJiB0aGF0LiRlbGVtZW50Lmhhc0NsYXNzKCdmYWRlJylcblxuICAgICAgaWYgKCF0aGF0LiRlbGVtZW50LnBhcmVudCgpLmxlbmd0aCkge1xuICAgICAgICB0aGF0LiRlbGVtZW50LmFwcGVuZFRvKHRoYXQuJGJvZHkpIC8vIGRvbid0IG1vdmUgbW9kYWxzIGRvbSBwb3NpdGlvblxuICAgICAgfVxuXG4gICAgICB0aGF0LiRlbGVtZW50XG4gICAgICAgIC5zaG93KClcbiAgICAgICAgLnNjcm9sbFRvcCgwKVxuXG4gICAgICB0aGF0LmFkanVzdERpYWxvZygpXG5cbiAgICAgIGlmICh0cmFuc2l0aW9uKSB7XG4gICAgICAgIHRoYXQuJGVsZW1lbnRbMF0ub2Zmc2V0V2lkdGggLy8gZm9yY2UgcmVmbG93XG4gICAgICB9XG5cbiAgICAgIHRoYXQuJGVsZW1lbnQuYWRkQ2xhc3MoJ2luJylcblxuICAgICAgdGhhdC5lbmZvcmNlRm9jdXMoKVxuXG4gICAgICB2YXIgZSA9ICQuRXZlbnQoJ3Nob3duLmJzLm1vZGFsJywgeyByZWxhdGVkVGFyZ2V0OiBfcmVsYXRlZFRhcmdldCB9KVxuXG4gICAgICB0cmFuc2l0aW9uID9cbiAgICAgICAgdGhhdC4kZGlhbG9nIC8vIHdhaXQgZm9yIG1vZGFsIHRvIHNsaWRlIGluXG4gICAgICAgICAgLm9uZSgnYnNUcmFuc2l0aW9uRW5kJywgZnVuY3Rpb24gKCkge1xuICAgICAgICAgICAgdGhhdC4kZWxlbWVudC50cmlnZ2VyKCdmb2N1cycpLnRyaWdnZXIoZSlcbiAgICAgICAgICB9KVxuICAgICAgICAgIC5lbXVsYXRlVHJhbnNpdGlvbkVuZChNb2RhbC5UUkFOU0lUSU9OX0RVUkFUSU9OKSA6XG4gICAgICAgIHRoYXQuJGVsZW1lbnQudHJpZ2dlcignZm9jdXMnKS50cmlnZ2VyKGUpXG4gICAgfSlcbiAgfVxuXG4gIE1vZGFsLnByb3RvdHlwZS5oaWRlID0gZnVuY3Rpb24gKGUpIHtcbiAgICBpZiAoZSkgZS5wcmV2ZW50RGVmYXVsdCgpXG5cbiAgICBlID0gJC5FdmVudCgnaGlkZS5icy5tb2RhbCcpXG5cbiAgICB0aGlzLiRlbGVtZW50LnRyaWdnZXIoZSlcblxuICAgIGlmICghdGhpcy5pc1Nob3duIHx8IGUuaXNEZWZhdWx0UHJldmVudGVkKCkpIHJldHVyblxuXG4gICAgdGhpcy5pc1Nob3duID0gZmFsc2VcblxuICAgIHRoaXMuZXNjYXBlKClcbiAgICB0aGlzLnJlc2l6ZSgpXG5cbiAgICAkKGRvY3VtZW50KS5vZmYoJ2ZvY3VzaW4uYnMubW9kYWwnKVxuXG4gICAgdGhpcy4kZWxlbWVudFxuICAgICAgLnJlbW92ZUNsYXNzKCdpbicpXG4gICAgICAub2ZmKCdjbGljay5kaXNtaXNzLmJzLm1vZGFsJylcbiAgICAgIC5vZmYoJ21vdXNldXAuZGlzbWlzcy5icy5tb2RhbCcpXG5cbiAgICB0aGlzLiRkaWFsb2cub2ZmKCdtb3VzZWRvd24uZGlzbWlzcy5icy5tb2RhbCcpXG5cbiAgICAkLnN1cHBvcnQudHJhbnNpdGlvbiAmJiB0aGlzLiRlbGVtZW50Lmhhc0NsYXNzKCdmYWRlJykgP1xuICAgICAgdGhpcy4kZWxlbWVudFxuICAgICAgICAub25lKCdic1RyYW5zaXRpb25FbmQnLCAkLnByb3h5KHRoaXMuaGlkZU1vZGFsLCB0aGlzKSlcbiAgICAgICAgLmVtdWxhdGVUcmFuc2l0aW9uRW5kKE1vZGFsLlRSQU5TSVRJT05fRFVSQVRJT04pIDpcbiAgICAgIHRoaXMuaGlkZU1vZGFsKClcbiAgfVxuXG4gIE1vZGFsLnByb3RvdHlwZS5lbmZvcmNlRm9jdXMgPSBmdW5jdGlvbiAoKSB7XG4gICAgJChkb2N1bWVudClcbiAgICAgIC5vZmYoJ2ZvY3VzaW4uYnMubW9kYWwnKSAvLyBndWFyZCBhZ2FpbnN0IGluZmluaXRlIGZvY3VzIGxvb3BcbiAgICAgIC5vbignZm9jdXNpbi5icy5tb2RhbCcsICQucHJveHkoZnVuY3Rpb24gKGUpIHtcbiAgICAgICAgaWYgKGRvY3VtZW50ICE9PSBlLnRhcmdldCAmJlxuICAgICAgICAgIHRoaXMuJGVsZW1lbnRbMF0gIT09IGUudGFyZ2V0ICYmXG4gICAgICAgICAgIXRoaXMuJGVsZW1lbnQuaGFzKGUudGFyZ2V0KS5sZW5ndGgpIHtcbiAgICAgICAgICB0aGlzLiRlbGVtZW50LnRyaWdnZXIoJ2ZvY3VzJylcbiAgICAgICAgfVxuICAgICAgfSwgdGhpcykpXG4gIH1cblxuICBNb2RhbC5wcm90b3R5cGUuZXNjYXBlID0gZnVuY3Rpb24gKCkge1xuICAgIGlmICh0aGlzLmlzU2hvd24gJiYgdGhpcy5vcHRpb25zLmtleWJvYXJkKSB7XG4gICAgICB0aGlzLiRlbGVtZW50Lm9uKCdrZXlkb3duLmRpc21pc3MuYnMubW9kYWwnLCAkLnByb3h5KGZ1bmN0aW9uIChlKSB7XG4gICAgICAgIGUud2hpY2ggPT0gMjcgJiYgdGhpcy5oaWRlKClcbiAgICAgIH0sIHRoaXMpKVxuICAgIH0gZWxzZSBpZiAoIXRoaXMuaXNTaG93bikge1xuICAgICAgdGhpcy4kZWxlbWVudC5vZmYoJ2tleWRvd24uZGlzbWlzcy5icy5tb2RhbCcpXG4gICAgfVxuICB9XG5cbiAgTW9kYWwucHJvdG90eXBlLnJlc2l6ZSA9IGZ1bmN0aW9uICgpIHtcbiAgICBpZiAodGhpcy5pc1Nob3duKSB7XG4gICAgICAkKHdpbmRvdykub24oJ3Jlc2l6ZS5icy5tb2RhbCcsICQucHJveHkodGhpcy5oYW5kbGVVcGRhdGUsIHRoaXMpKVxuICAgIH0gZWxzZSB7XG4gICAgICAkKHdpbmRvdykub2ZmKCdyZXNpemUuYnMubW9kYWwnKVxuICAgIH1cbiAgfVxuXG4gIE1vZGFsLnByb3RvdHlwZS5oaWRlTW9kYWwgPSBmdW5jdGlvbiAoKSB7XG4gICAgdmFyIHRoYXQgPSB0aGlzXG4gICAgdGhpcy4kZWxlbWVudC5oaWRlKClcbiAgICB0aGlzLmJhY2tkcm9wKGZ1bmN0aW9uICgpIHtcbiAgICAgIHRoYXQuJGJvZHkucmVtb3ZlQ2xhc3MoJ21vZGFsLW9wZW4nKVxuICAgICAgdGhhdC5yZXNldEFkanVzdG1lbnRzKClcbiAgICAgIHRoYXQucmVzZXRTY3JvbGxiYXIoKVxuICAgICAgdGhhdC4kZWxlbWVudC50cmlnZ2VyKCdoaWRkZW4uYnMubW9kYWwnKVxuICAgIH0pXG4gIH1cblxuICBNb2RhbC5wcm90b3R5cGUucmVtb3ZlQmFja2Ryb3AgPSBmdW5jdGlvbiAoKSB7XG4gICAgdGhpcy4kYmFja2Ryb3AgJiYgdGhpcy4kYmFja2Ryb3AucmVtb3ZlKClcbiAgICB0aGlzLiRiYWNrZHJvcCA9IG51bGxcbiAgfVxuXG4gIE1vZGFsLnByb3RvdHlwZS5iYWNrZHJvcCA9IGZ1bmN0aW9uIChjYWxsYmFjaykge1xuICAgIHZhciB0aGF0ID0gdGhpc1xuICAgIHZhciBhbmltYXRlID0gdGhpcy4kZWxlbWVudC5oYXNDbGFzcygnZmFkZScpID8gJ2ZhZGUnIDogJydcblxuICAgIGlmICh0aGlzLmlzU2hvd24gJiYgdGhpcy5vcHRpb25zLmJhY2tkcm9wKSB7XG4gICAgICB2YXIgZG9BbmltYXRlID0gJC5zdXBwb3J0LnRyYW5zaXRpb24gJiYgYW5pbWF0ZVxuXG4gICAgICB0aGlzLiRiYWNrZHJvcCA9ICQoZG9jdW1lbnQuY3JlYXRlRWxlbWVudCgnZGl2JykpXG4gICAgICAgIC5hZGRDbGFzcygnbW9kYWwtYmFja2Ryb3AgJyArIGFuaW1hdGUpXG4gICAgICAgIC5hcHBlbmRUbyh0aGlzLiRib2R5KVxuXG4gICAgICB0aGlzLiRlbGVtZW50Lm9uKCdjbGljay5kaXNtaXNzLmJzLm1vZGFsJywgJC5wcm94eShmdW5jdGlvbiAoZSkge1xuICAgICAgICBpZiAodGhpcy5pZ25vcmVCYWNrZHJvcENsaWNrKSB7XG4gICAgICAgICAgdGhpcy5pZ25vcmVCYWNrZHJvcENsaWNrID0gZmFsc2VcbiAgICAgICAgICByZXR1cm5cbiAgICAgICAgfVxuICAgICAgICBpZiAoZS50YXJnZXQgIT09IGUuY3VycmVudFRhcmdldCkgcmV0dXJuXG4gICAgICAgIHRoaXMub3B0aW9ucy5iYWNrZHJvcCA9PSAnc3RhdGljJ1xuICAgICAgICAgID8gdGhpcy4kZWxlbWVudFswXS5mb2N1cygpXG4gICAgICAgICAgOiB0aGlzLmhpZGUoKVxuICAgICAgfSwgdGhpcykpXG5cbiAgICAgIGlmIChkb0FuaW1hdGUpIHRoaXMuJGJhY2tkcm9wWzBdLm9mZnNldFdpZHRoIC8vIGZvcmNlIHJlZmxvd1xuXG4gICAgICB0aGlzLiRiYWNrZHJvcC5hZGRDbGFzcygnaW4nKVxuXG4gICAgICBpZiAoIWNhbGxiYWNrKSByZXR1cm5cblxuICAgICAgZG9BbmltYXRlID9cbiAgICAgICAgdGhpcy4kYmFja2Ryb3BcbiAgICAgICAgICAub25lKCdic1RyYW5zaXRpb25FbmQnLCBjYWxsYmFjaylcbiAgICAgICAgICAuZW11bGF0ZVRyYW5zaXRpb25FbmQoTW9kYWwuQkFDS0RST1BfVFJBTlNJVElPTl9EVVJBVElPTikgOlxuICAgICAgICBjYWxsYmFjaygpXG5cbiAgICB9IGVsc2UgaWYgKCF0aGlzLmlzU2hvd24gJiYgdGhpcy4kYmFja2Ryb3ApIHtcbiAgICAgIHRoaXMuJGJhY2tkcm9wLnJlbW92ZUNsYXNzKCdpbicpXG5cbiAgICAgIHZhciBjYWxsYmFja1JlbW92ZSA9IGZ1bmN0aW9uICgpIHtcbiAgICAgICAgdGhhdC5yZW1vdmVCYWNrZHJvcCgpXG4gICAgICAgIGNhbGxiYWNrICYmIGNhbGxiYWNrKClcbiAgICAgIH1cbiAgICAgICQuc3VwcG9ydC50cmFuc2l0aW9uICYmIHRoaXMuJGVsZW1lbnQuaGFzQ2xhc3MoJ2ZhZGUnKSA/XG4gICAgICAgIHRoaXMuJGJhY2tkcm9wXG4gICAgICAgICAgLm9uZSgnYnNUcmFuc2l0aW9uRW5kJywgY2FsbGJhY2tSZW1vdmUpXG4gICAgICAgICAgLmVtdWxhdGVUcmFuc2l0aW9uRW5kKE1vZGFsLkJBQ0tEUk9QX1RSQU5TSVRJT05fRFVSQVRJT04pIDpcbiAgICAgICAgY2FsbGJhY2tSZW1vdmUoKVxuXG4gICAgfSBlbHNlIGlmIChjYWxsYmFjaykge1xuICAgICAgY2FsbGJhY2soKVxuICAgIH1cbiAgfVxuXG4gIC8vIHRoZXNlIGZvbGxvd2luZyBtZXRob2RzIGFyZSB1c2VkIHRvIGhhbmRsZSBvdmVyZmxvd2luZyBtb2RhbHNcblxuICBNb2RhbC5wcm90b3R5cGUuaGFuZGxlVXBkYXRlID0gZnVuY3Rpb24gKCkge1xuICAgIHRoaXMuYWRqdXN0RGlhbG9nKClcbiAgfVxuXG4gIE1vZGFsLnByb3RvdHlwZS5hZGp1c3REaWFsb2cgPSBmdW5jdGlvbiAoKSB7XG4gICAgdmFyIG1vZGFsSXNPdmVyZmxvd2luZyA9IHRoaXMuJGVsZW1lbnRbMF0uc2Nyb2xsSGVpZ2h0ID4gZG9jdW1lbnQuZG9jdW1lbnRFbGVtZW50LmNsaWVudEhlaWdodFxuXG4gICAgdGhpcy4kZWxlbWVudC5jc3Moe1xuICAgICAgcGFkZGluZ0xlZnQ6ICF0aGlzLmJvZHlJc092ZXJmbG93aW5nICYmIG1vZGFsSXNPdmVyZmxvd2luZyA/IHRoaXMuc2Nyb2xsYmFyV2lkdGggOiAnJyxcbiAgICAgIHBhZGRpbmdSaWdodDogdGhpcy5ib2R5SXNPdmVyZmxvd2luZyAmJiAhbW9kYWxJc092ZXJmbG93aW5nID8gdGhpcy5zY3JvbGxiYXJXaWR0aCA6ICcnXG4gICAgfSlcbiAgfVxuXG4gIE1vZGFsLnByb3RvdHlwZS5yZXNldEFkanVzdG1lbnRzID0gZnVuY3Rpb24gKCkge1xuICAgIHRoaXMuJGVsZW1lbnQuY3NzKHtcbiAgICAgIHBhZGRpbmdMZWZ0OiAnJyxcbiAgICAgIHBhZGRpbmdSaWdodDogJydcbiAgICB9KVxuICB9XG5cbiAgTW9kYWwucHJvdG90eXBlLmNoZWNrU2Nyb2xsYmFyID0gZnVuY3Rpb24gKCkge1xuICAgIHZhciBmdWxsV2luZG93V2lkdGggPSB3aW5kb3cuaW5uZXJXaWR0aFxuICAgIGlmICghZnVsbFdpbmRvd1dpZHRoKSB7IC8vIHdvcmthcm91bmQgZm9yIG1pc3Npbmcgd2luZG93LmlubmVyV2lkdGggaW4gSUU4XG4gICAgICB2YXIgZG9jdW1lbnRFbGVtZW50UmVjdCA9IGRvY3VtZW50LmRvY3VtZW50RWxlbWVudC5nZXRCb3VuZGluZ0NsaWVudFJlY3QoKVxuICAgICAgZnVsbFdpbmRvd1dpZHRoID0gZG9jdW1lbnRFbGVtZW50UmVjdC5yaWdodCAtIE1hdGguYWJzKGRvY3VtZW50RWxlbWVudFJlY3QubGVmdClcbiAgICB9XG4gICAgdGhpcy5ib2R5SXNPdmVyZmxvd2luZyA9IGRvY3VtZW50LmJvZHkuY2xpZW50V2lkdGggPCBmdWxsV2luZG93V2lkdGhcbiAgICB0aGlzLnNjcm9sbGJhcldpZHRoID0gdGhpcy5tZWFzdXJlU2Nyb2xsYmFyKClcbiAgfVxuXG4gIE1vZGFsLnByb3RvdHlwZS5zZXRTY3JvbGxiYXIgPSBmdW5jdGlvbiAoKSB7XG4gICAgdmFyIGJvZHlQYWQgPSBwYXJzZUludCgodGhpcy4kYm9keS5jc3MoJ3BhZGRpbmctcmlnaHQnKSB8fCAwKSwgMTApXG4gICAgdGhpcy5vcmlnaW5hbEJvZHlQYWQgPSBkb2N1bWVudC5ib2R5LnN0eWxlLnBhZGRpbmdSaWdodCB8fCAnJ1xuICAgIHZhciBzY3JvbGxiYXJXaWR0aCA9IHRoaXMuc2Nyb2xsYmFyV2lkdGhcbiAgICBpZiAodGhpcy5ib2R5SXNPdmVyZmxvd2luZykge1xuICAgICAgdGhpcy4kYm9keS5jc3MoJ3BhZGRpbmctcmlnaHQnLCBib2R5UGFkICsgc2Nyb2xsYmFyV2lkdGgpXG4gICAgICAkKHRoaXMuZml4ZWRDb250ZW50KS5lYWNoKGZ1bmN0aW9uIChpbmRleCwgZWxlbWVudCkge1xuICAgICAgICB2YXIgYWN0dWFsUGFkZGluZyA9IGVsZW1lbnQuc3R5bGUucGFkZGluZ1JpZ2h0XG4gICAgICAgIHZhciBjYWxjdWxhdGVkUGFkZGluZyA9ICQoZWxlbWVudCkuY3NzKCdwYWRkaW5nLXJpZ2h0JylcbiAgICAgICAgJChlbGVtZW50KVxuICAgICAgICAgIC5kYXRhKCdwYWRkaW5nLXJpZ2h0JywgYWN0dWFsUGFkZGluZylcbiAgICAgICAgICAuY3NzKCdwYWRkaW5nLXJpZ2h0JywgcGFyc2VGbG9hdChjYWxjdWxhdGVkUGFkZGluZykgKyBzY3JvbGxiYXJXaWR0aCArICdweCcpXG4gICAgICB9KVxuICAgIH1cbiAgfVxuXG4gIE1vZGFsLnByb3RvdHlwZS5yZXNldFNjcm9sbGJhciA9IGZ1bmN0aW9uICgpIHtcbiAgICB0aGlzLiRib2R5LmNzcygncGFkZGluZy1yaWdodCcsIHRoaXMub3JpZ2luYWxCb2R5UGFkKVxuICAgICQodGhpcy5maXhlZENvbnRlbnQpLmVhY2goZnVuY3Rpb24gKGluZGV4LCBlbGVtZW50KSB7XG4gICAgICB2YXIgcGFkZGluZyA9ICQoZWxlbWVudCkuZGF0YSgncGFkZGluZy1yaWdodCcpXG4gICAgICAkKGVsZW1lbnQpLnJlbW92ZURhdGEoJ3BhZGRpbmctcmlnaHQnKVxuICAgICAgZWxlbWVudC5zdHlsZS5wYWRkaW5nUmlnaHQgPSBwYWRkaW5nID8gcGFkZGluZyA6ICcnXG4gICAgfSlcbiAgfVxuXG4gIE1vZGFsLnByb3RvdHlwZS5tZWFzdXJlU2Nyb2xsYmFyID0gZnVuY3Rpb24gKCkgeyAvLyB0aHggd2Fsc2hcbiAgICB2YXIgc2Nyb2xsRGl2ID0gZG9jdW1lbnQuY3JlYXRlRWxlbWVudCgnZGl2JylcbiAgICBzY3JvbGxEaXYuY2xhc3NOYW1lID0gJ21vZGFsLXNjcm9sbGJhci1tZWFzdXJlJ1xuICAgIHRoaXMuJGJvZHkuYXBwZW5kKHNjcm9sbERpdilcbiAgICB2YXIgc2Nyb2xsYmFyV2lkdGggPSBzY3JvbGxEaXYub2Zmc2V0V2lkdGggLSBzY3JvbGxEaXYuY2xpZW50V2lkdGhcbiAgICB0aGlzLiRib2R5WzBdLnJlbW92ZUNoaWxkKHNjcm9sbERpdilcbiAgICByZXR1cm4gc2Nyb2xsYmFyV2lkdGhcbiAgfVxuXG5cbiAgLy8gTU9EQUwgUExVR0lOIERFRklOSVRJT05cbiAgLy8gPT09PT09PT09PT09PT09PT09PT09PT1cblxuICBmdW5jdGlvbiBQbHVnaW4ob3B0aW9uLCBfcmVsYXRlZFRhcmdldCkge1xuICAgIHJldHVybiB0aGlzLmVhY2goZnVuY3Rpb24gKCkge1xuICAgICAgdmFyICR0aGlzID0gJCh0aGlzKVxuICAgICAgdmFyIGRhdGEgPSAkdGhpcy5kYXRhKCdicy5tb2RhbCcpXG4gICAgICB2YXIgb3B0aW9ucyA9ICQuZXh0ZW5kKHt9LCBNb2RhbC5ERUZBVUxUUywgJHRoaXMuZGF0YSgpLCB0eXBlb2Ygb3B0aW9uID09ICdvYmplY3QnICYmIG9wdGlvbilcblxuICAgICAgaWYgKCFkYXRhKSAkdGhpcy5kYXRhKCdicy5tb2RhbCcsIChkYXRhID0gbmV3IE1vZGFsKHRoaXMsIG9wdGlvbnMpKSlcbiAgICAgIGlmICh0eXBlb2Ygb3B0aW9uID09ICdzdHJpbmcnKSBkYXRhW29wdGlvbl0oX3JlbGF0ZWRUYXJnZXQpXG4gICAgICBlbHNlIGlmIChvcHRpb25zLnNob3cpIGRhdGEuc2hvdyhfcmVsYXRlZFRhcmdldClcbiAgICB9KVxuICB9XG5cbiAgdmFyIG9sZCA9ICQuZm4ubW9kYWxcblxuICAkLmZuLm1vZGFsID0gUGx1Z2luXG4gICQuZm4ubW9kYWwuQ29uc3RydWN0b3IgPSBNb2RhbFxuXG5cbiAgLy8gTU9EQUwgTk8gQ09ORkxJQ1RcbiAgLy8gPT09PT09PT09PT09PT09PT1cblxuICAkLmZuLm1vZGFsLm5vQ29uZmxpY3QgPSBmdW5jdGlvbiAoKSB7XG4gICAgJC5mbi5tb2RhbCA9IG9sZFxuICAgIHJldHVybiB0aGlzXG4gIH1cblxuXG4gIC8vIE1PREFMIERBVEEtQVBJXG4gIC8vID09PT09PT09PT09PT09XG5cbiAgJChkb2N1bWVudCkub24oJ2NsaWNrLmJzLm1vZGFsLmRhdGEtYXBpJywgJ1tkYXRhLXRvZ2dsZT1cIm1vZGFsXCJdJywgZnVuY3Rpb24gKGUpIHtcbiAgICB2YXIgJHRoaXMgPSAkKHRoaXMpXG4gICAgdmFyIGhyZWYgPSAkdGhpcy5hdHRyKCdocmVmJylcbiAgICB2YXIgdGFyZ2V0ID0gJHRoaXMuYXR0cignZGF0YS10YXJnZXQnKSB8fFxuICAgICAgKGhyZWYgJiYgaHJlZi5yZXBsYWNlKC8uKig/PSNbXlxcc10rJCkvLCAnJykpIC8vIHN0cmlwIGZvciBpZTdcblxuICAgIHZhciAkdGFyZ2V0ID0gJChkb2N1bWVudCkuZmluZCh0YXJnZXQpXG4gICAgdmFyIG9wdGlvbiA9ICR0YXJnZXQuZGF0YSgnYnMubW9kYWwnKSA/ICd0b2dnbGUnIDogJC5leHRlbmQoeyByZW1vdGU6ICEvIy8udGVzdChocmVmKSAmJiBocmVmIH0sICR0YXJnZXQuZGF0YSgpLCAkdGhpcy5kYXRhKCkpXG5cbiAgICBpZiAoJHRoaXMuaXMoJ2EnKSkgZS5wcmV2ZW50RGVmYXVsdCgpXG5cbiAgICAkdGFyZ2V0Lm9uZSgnc2hvdy5icy5tb2RhbCcsIGZ1bmN0aW9uIChzaG93RXZlbnQpIHtcbiAgICAgIGlmIChzaG93RXZlbnQuaXNEZWZhdWx0UHJldmVudGVkKCkpIHJldHVybiAvLyBvbmx5IHJlZ2lzdGVyIGZvY3VzIHJlc3RvcmVyIGlmIG1vZGFsIHdpbGwgYWN0dWFsbHkgZ2V0IHNob3duXG4gICAgICAkdGFyZ2V0Lm9uZSgnaGlkZGVuLmJzLm1vZGFsJywgZnVuY3Rpb24gKCkge1xuICAgICAgICAkdGhpcy5pcygnOnZpc2libGUnKSAmJiAkdGhpcy50cmlnZ2VyKCdmb2N1cycpXG4gICAgICB9KVxuICAgIH0pXG4gICAgUGx1Z2luLmNhbGwoJHRhcmdldCwgb3B0aW9uLCB0aGlzKVxuICB9KVxuXG59KGpRdWVyeSk7XG5cbi8qID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PVxuICogQm9vdHN0cmFwOiB0b29sdGlwLmpzIHYzLjQuMVxuICogaHR0cHM6Ly9nZXRib290c3RyYXAuY29tL2RvY3MvMy40L2phdmFzY3JpcHQvI3Rvb2x0aXBcbiAqIEluc3BpcmVkIGJ5IHRoZSBvcmlnaW5hbCBqUXVlcnkudGlwc3kgYnkgSmFzb24gRnJhbWVcbiAqID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PVxuICogQ29weXJpZ2h0IDIwMTEtMjAxOSBUd2l0dGVyLCBJbmMuXG4gKiBMaWNlbnNlZCB1bmRlciBNSVQgKGh0dHBzOi8vZ2l0aHViLmNvbS90d2JzL2Jvb3RzdHJhcC9ibG9iL21hc3Rlci9MSUNFTlNFKVxuICogPT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09ICovXG5cbitmdW5jdGlvbiAoJCkge1xuICAndXNlIHN0cmljdCc7XG5cbiAgdmFyIERJU0FMTE9XRURfQVRUUklCVVRFUyA9IFsnc2FuaXRpemUnLCAnd2hpdGVMaXN0JywgJ3Nhbml0aXplRm4nXVxuXG4gIHZhciB1cmlBdHRycyA9IFtcbiAgICAnYmFja2dyb3VuZCcsXG4gICAgJ2NpdGUnLFxuICAgICdocmVmJyxcbiAgICAnaXRlbXR5cGUnLFxuICAgICdsb25nZGVzYycsXG4gICAgJ3Bvc3RlcicsXG4gICAgJ3NyYycsXG4gICAgJ3hsaW5rOmhyZWYnXG4gIF1cblxuICB2YXIgQVJJQV9BVFRSSUJVVEVfUEFUVEVSTiA9IC9eYXJpYS1bXFx3LV0qJC9pXG5cbiAgdmFyIERlZmF1bHRXaGl0ZWxpc3QgPSB7XG4gICAgLy8gR2xvYmFsIGF0dHJpYnV0ZXMgYWxsb3dlZCBvbiBhbnkgc3VwcGxpZWQgZWxlbWVudCBiZWxvdy5cbiAgICAnKic6IFsnY2xhc3MnLCAnZGlyJywgJ2lkJywgJ2xhbmcnLCAncm9sZScsIEFSSUFfQVRUUklCVVRFX1BBVFRFUk5dLFxuICAgIGE6IFsndGFyZ2V0JywgJ2hyZWYnLCAndGl0bGUnLCAncmVsJ10sXG4gICAgYXJlYTogW10sXG4gICAgYjogW10sXG4gICAgYnI6IFtdLFxuICAgIGNvbDogW10sXG4gICAgY29kZTogW10sXG4gICAgZGl2OiBbXSxcbiAgICBlbTogW10sXG4gICAgaHI6IFtdLFxuICAgIGgxOiBbXSxcbiAgICBoMjogW10sXG4gICAgaDM6IFtdLFxuICAgIGg0OiBbXSxcbiAgICBoNTogW10sXG4gICAgaDY6IFtdLFxuICAgIGk6IFtdLFxuICAgIGltZzogWydzcmMnLCAnYWx0JywgJ3RpdGxlJywgJ3dpZHRoJywgJ2hlaWdodCddLFxuICAgIGxpOiBbXSxcbiAgICBvbDogW10sXG4gICAgcDogW10sXG4gICAgcHJlOiBbXSxcbiAgICBzOiBbXSxcbiAgICBzbWFsbDogW10sXG4gICAgc3BhbjogW10sXG4gICAgc3ViOiBbXSxcbiAgICBzdXA6IFtdLFxuICAgIHN0cm9uZzogW10sXG4gICAgdTogW10sXG4gICAgdWw6IFtdXG4gIH1cblxuICAvKipcbiAgICogQSBwYXR0ZXJuIHRoYXQgcmVjb2duaXplcyBhIGNvbW1vbmx5IHVzZWZ1bCBzdWJzZXQgb2YgVVJMcyB0aGF0IGFyZSBzYWZlLlxuICAgKlxuICAgKiBTaG91dG91dCB0byBBbmd1bGFyIDcgaHR0cHM6Ly9naXRodWIuY29tL2FuZ3VsYXIvYW5ndWxhci9ibG9iLzcuMi40L3BhY2thZ2VzL2NvcmUvc3JjL3Nhbml0aXphdGlvbi91cmxfc2FuaXRpemVyLnRzXG4gICAqL1xuICB2YXIgU0FGRV9VUkxfUEFUVEVSTiA9IC9eKD86KD86aHR0cHM/fG1haWx0b3xmdHB8dGVsfGZpbGUpOnxbXiY6Lz8jXSooPzpbLz8jXXwkKSkvZ2lcblxuICAvKipcbiAgICogQSBwYXR0ZXJuIHRoYXQgbWF0Y2hlcyBzYWZlIGRhdGEgVVJMcy4gT25seSBtYXRjaGVzIGltYWdlLCB2aWRlbyBhbmQgYXVkaW8gdHlwZXMuXG4gICAqXG4gICAqIFNob3V0b3V0IHRvIEFuZ3VsYXIgNyBodHRwczovL2dpdGh1Yi5jb20vYW5ndWxhci9hbmd1bGFyL2Jsb2IvNy4yLjQvcGFja2FnZXMvY29yZS9zcmMvc2FuaXRpemF0aW9uL3VybF9zYW5pdGl6ZXIudHNcbiAgICovXG4gIHZhciBEQVRBX1VSTF9QQVRURVJOID0gL15kYXRhOig/OmltYWdlXFwvKD86Ym1wfGdpZnxqcGVnfGpwZ3xwbmd8dGlmZnx3ZWJwKXx2aWRlb1xcLyg/Om1wZWd8bXA0fG9nZ3x3ZWJtKXxhdWRpb1xcLyg/Om1wM3xvZ2F8b2dnfG9wdXMpKTtiYXNlNjQsW2EtejAtOSsvXSs9KiQvaVxuXG4gIGZ1bmN0aW9uIGFsbG93ZWRBdHRyaWJ1dGUoYXR0ciwgYWxsb3dlZEF0dHJpYnV0ZUxpc3QpIHtcbiAgICB2YXIgYXR0ck5hbWUgPSBhdHRyLm5vZGVOYW1lLnRvTG93ZXJDYXNlKClcblxuICAgIGlmICgkLmluQXJyYXkoYXR0ck5hbWUsIGFsbG93ZWRBdHRyaWJ1dGVMaXN0KSAhPT0gLTEpIHtcbiAgICAgIGlmICgkLmluQXJyYXkoYXR0ck5hbWUsIHVyaUF0dHJzKSAhPT0gLTEpIHtcbiAgICAgICAgcmV0dXJuIEJvb2xlYW4oYXR0ci5ub2RlVmFsdWUubWF0Y2goU0FGRV9VUkxfUEFUVEVSTikgfHwgYXR0ci5ub2RlVmFsdWUubWF0Y2goREFUQV9VUkxfUEFUVEVSTikpXG4gICAgICB9XG5cbiAgICAgIHJldHVybiB0cnVlXG4gICAgfVxuXG4gICAgdmFyIHJlZ0V4cCA9ICQoYWxsb3dlZEF0dHJpYnV0ZUxpc3QpLmZpbHRlcihmdW5jdGlvbiAoaW5kZXgsIHZhbHVlKSB7XG4gICAgICByZXR1cm4gdmFsdWUgaW5zdGFuY2VvZiBSZWdFeHBcbiAgICB9KVxuXG4gICAgLy8gQ2hlY2sgaWYgYSByZWd1bGFyIGV4cHJlc3Npb24gdmFsaWRhdGVzIHRoZSBhdHRyaWJ1dGUuXG4gICAgZm9yICh2YXIgaSA9IDAsIGwgPSByZWdFeHAubGVuZ3RoOyBpIDwgbDsgaSsrKSB7XG4gICAgICBpZiAoYXR0ck5hbWUubWF0Y2gocmVnRXhwW2ldKSkge1xuICAgICAgICByZXR1cm4gdHJ1ZVxuICAgICAgfVxuICAgIH1cblxuICAgIHJldHVybiBmYWxzZVxuICB9XG5cbiAgZnVuY3Rpb24gc2FuaXRpemVIdG1sKHVuc2FmZUh0bWwsIHdoaXRlTGlzdCwgc2FuaXRpemVGbikge1xuICAgIGlmICh1bnNhZmVIdG1sLmxlbmd0aCA9PT0gMCkge1xuICAgICAgcmV0dXJuIHVuc2FmZUh0bWxcbiAgICB9XG5cbiAgICBpZiAoc2FuaXRpemVGbiAmJiB0eXBlb2Ygc2FuaXRpemVGbiA9PT0gJ2Z1bmN0aW9uJykge1xuICAgICAgcmV0dXJuIHNhbml0aXplRm4odW5zYWZlSHRtbClcbiAgICB9XG5cbiAgICAvLyBJRSA4IGFuZCBiZWxvdyBkb24ndCBzdXBwb3J0IGNyZWF0ZUhUTUxEb2N1bWVudFxuICAgIGlmICghZG9jdW1lbnQuaW1wbGVtZW50YXRpb24gfHwgIWRvY3VtZW50LmltcGxlbWVudGF0aW9uLmNyZWF0ZUhUTUxEb2N1bWVudCkge1xuICAgICAgcmV0dXJuIHVuc2FmZUh0bWxcbiAgICB9XG5cbiAgICB2YXIgY3JlYXRlZERvY3VtZW50ID0gZG9jdW1lbnQuaW1wbGVtZW50YXRpb24uY3JlYXRlSFRNTERvY3VtZW50KCdzYW5pdGl6YXRpb24nKVxuICAgIGNyZWF0ZWREb2N1bWVudC5ib2R5LmlubmVySFRNTCA9IHVuc2FmZUh0bWxcblxuICAgIHZhciB3aGl0ZWxpc3RLZXlzID0gJC5tYXAod2hpdGVMaXN0LCBmdW5jdGlvbiAoZWwsIGkpIHsgcmV0dXJuIGkgfSlcbiAgICB2YXIgZWxlbWVudHMgPSAkKGNyZWF0ZWREb2N1bWVudC5ib2R5KS5maW5kKCcqJylcblxuICAgIGZvciAodmFyIGkgPSAwLCBsZW4gPSBlbGVtZW50cy5sZW5ndGg7IGkgPCBsZW47IGkrKykge1xuICAgICAgdmFyIGVsID0gZWxlbWVudHNbaV1cbiAgICAgIHZhciBlbE5hbWUgPSBlbC5ub2RlTmFtZS50b0xvd2VyQ2FzZSgpXG5cbiAgICAgIGlmICgkLmluQXJyYXkoZWxOYW1lLCB3aGl0ZWxpc3RLZXlzKSA9PT0gLTEpIHtcbiAgICAgICAgZWwucGFyZW50Tm9kZS5yZW1vdmVDaGlsZChlbClcblxuICAgICAgICBjb250aW51ZVxuICAgICAgfVxuXG4gICAgICB2YXIgYXR0cmlidXRlTGlzdCA9ICQubWFwKGVsLmF0dHJpYnV0ZXMsIGZ1bmN0aW9uIChlbCkgeyByZXR1cm4gZWwgfSlcbiAgICAgIHZhciB3aGl0ZWxpc3RlZEF0dHJpYnV0ZXMgPSBbXS5jb25jYXQod2hpdGVMaXN0WycqJ10gfHwgW10sIHdoaXRlTGlzdFtlbE5hbWVdIHx8IFtdKVxuXG4gICAgICBmb3IgKHZhciBqID0gMCwgbGVuMiA9IGF0dHJpYnV0ZUxpc3QubGVuZ3RoOyBqIDwgbGVuMjsgaisrKSB7XG4gICAgICAgIGlmICghYWxsb3dlZEF0dHJpYnV0ZShhdHRyaWJ1dGVMaXN0W2pdLCB3aGl0ZWxpc3RlZEF0dHJpYnV0ZXMpKSB7XG4gICAgICAgICAgZWwucmVtb3ZlQXR0cmlidXRlKGF0dHJpYnV0ZUxpc3Rbal0ubm9kZU5hbWUpXG4gICAgICAgIH1cbiAgICAgIH1cbiAgICB9XG5cbiAgICByZXR1cm4gY3JlYXRlZERvY3VtZW50LmJvZHkuaW5uZXJIVE1MXG4gIH1cblxuICAvLyBUT09MVElQIFBVQkxJQyBDTEFTUyBERUZJTklUSU9OXG4gIC8vID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT1cblxuICB2YXIgVG9vbHRpcCA9IGZ1bmN0aW9uIChlbGVtZW50LCBvcHRpb25zKSB7XG4gICAgdGhpcy50eXBlICAgICAgID0gbnVsbFxuICAgIHRoaXMub3B0aW9ucyAgICA9IG51bGxcbiAgICB0aGlzLmVuYWJsZWQgICAgPSBudWxsXG4gICAgdGhpcy50aW1lb3V0ICAgID0gbnVsbFxuICAgIHRoaXMuaG92ZXJTdGF0ZSA9IG51bGxcbiAgICB0aGlzLiRlbGVtZW50ICAgPSBudWxsXG4gICAgdGhpcy5pblN0YXRlICAgID0gbnVsbFxuXG4gICAgdGhpcy5pbml0KCd0b29sdGlwJywgZWxlbWVudCwgb3B0aW9ucylcbiAgfVxuXG4gIFRvb2x0aXAuVkVSU0lPTiAgPSAnMy40LjEnXG5cbiAgVG9vbHRpcC5UUkFOU0lUSU9OX0RVUkFUSU9OID0gMTUwXG5cbiAgVG9vbHRpcC5ERUZBVUxUUyA9IHtcbiAgICBhbmltYXRpb246IHRydWUsXG4gICAgcGxhY2VtZW50OiAndG9wJyxcbiAgICBzZWxlY3RvcjogZmFsc2UsXG4gICAgdGVtcGxhdGU6ICc8ZGl2IGNsYXNzPVwidG9vbHRpcFwiIHJvbGU9XCJ0b29sdGlwXCI+PGRpdiBjbGFzcz1cInRvb2x0aXAtYXJyb3dcIj48L2Rpdj48ZGl2IGNsYXNzPVwidG9vbHRpcC1pbm5lclwiPjwvZGl2PjwvZGl2PicsXG4gICAgdHJpZ2dlcjogJ2hvdmVyIGZvY3VzJyxcbiAgICB0aXRsZTogJycsXG4gICAgZGVsYXk6IDAsXG4gICAgaHRtbDogZmFsc2UsXG4gICAgY29udGFpbmVyOiBmYWxzZSxcbiAgICB2aWV3cG9ydDoge1xuICAgICAgc2VsZWN0b3I6ICdib2R5JyxcbiAgICAgIHBhZGRpbmc6IDBcbiAgICB9LFxuICAgIHNhbml0aXplIDogdHJ1ZSxcbiAgICBzYW5pdGl6ZUZuIDogbnVsbCxcbiAgICB3aGl0ZUxpc3QgOiBEZWZhdWx0V2hpdGVsaXN0XG4gIH1cblxuICBUb29sdGlwLnByb3RvdHlwZS5pbml0ID0gZnVuY3Rpb24gKHR5cGUsIGVsZW1lbnQsIG9wdGlvbnMpIHtcbiAgICB0aGlzLmVuYWJsZWQgICA9IHRydWVcbiAgICB0aGlzLnR5cGUgICAgICA9IHR5cGVcbiAgICB0aGlzLiRlbGVtZW50ICA9ICQoZWxlbWVudClcbiAgICB0aGlzLm9wdGlvbnMgICA9IHRoaXMuZ2V0T3B0aW9ucyhvcHRpb25zKVxuICAgIHRoaXMuJHZpZXdwb3J0ID0gdGhpcy5vcHRpb25zLnZpZXdwb3J0ICYmICQoZG9jdW1lbnQpLmZpbmQoJC5pc0Z1bmN0aW9uKHRoaXMub3B0aW9ucy52aWV3cG9ydCkgPyB0aGlzLm9wdGlvbnMudmlld3BvcnQuY2FsbCh0aGlzLCB0aGlzLiRlbGVtZW50KSA6ICh0aGlzLm9wdGlvbnMudmlld3BvcnQuc2VsZWN0b3IgfHwgdGhpcy5vcHRpb25zLnZpZXdwb3J0KSlcbiAgICB0aGlzLmluU3RhdGUgICA9IHsgY2xpY2s6IGZhbHNlLCBob3ZlcjogZmFsc2UsIGZvY3VzOiBmYWxzZSB9XG5cbiAgICBpZiAodGhpcy4kZWxlbWVudFswXSBpbnN0YW5jZW9mIGRvY3VtZW50LmNvbnN0cnVjdG9yICYmICF0aGlzLm9wdGlvbnMuc2VsZWN0b3IpIHtcbiAgICAgIHRocm93IG5ldyBFcnJvcignYHNlbGVjdG9yYCBvcHRpb24gbXVzdCBiZSBzcGVjaWZpZWQgd2hlbiBpbml0aWFsaXppbmcgJyArIHRoaXMudHlwZSArICcgb24gdGhlIHdpbmRvdy5kb2N1bWVudCBvYmplY3QhJylcbiAgICB9XG5cbiAgICB2YXIgdHJpZ2dlcnMgPSB0aGlzLm9wdGlvbnMudHJpZ2dlci5zcGxpdCgnICcpXG5cbiAgICBmb3IgKHZhciBpID0gdHJpZ2dlcnMubGVuZ3RoOyBpLS07KSB7XG4gICAgICB2YXIgdHJpZ2dlciA9IHRyaWdnZXJzW2ldXG5cbiAgICAgIGlmICh0cmlnZ2VyID09ICdjbGljaycpIHtcbiAgICAgICAgdGhpcy4kZWxlbWVudC5vbignY2xpY2suJyArIHRoaXMudHlwZSwgdGhpcy5vcHRpb25zLnNlbGVjdG9yLCAkLnByb3h5KHRoaXMudG9nZ2xlLCB0aGlzKSlcbiAgICAgIH0gZWxzZSBpZiAodHJpZ2dlciAhPSAnbWFudWFsJykge1xuICAgICAgICB2YXIgZXZlbnRJbiAgPSB0cmlnZ2VyID09ICdob3ZlcicgPyAnbW91c2VlbnRlcicgOiAnZm9jdXNpbidcbiAgICAgICAgdmFyIGV2ZW50T3V0ID0gdHJpZ2dlciA9PSAnaG92ZXInID8gJ21vdXNlbGVhdmUnIDogJ2ZvY3Vzb3V0J1xuXG4gICAgICAgIHRoaXMuJGVsZW1lbnQub24oZXZlbnRJbiAgKyAnLicgKyB0aGlzLnR5cGUsIHRoaXMub3B0aW9ucy5zZWxlY3RvciwgJC5wcm94eSh0aGlzLmVudGVyLCB0aGlzKSlcbiAgICAgICAgdGhpcy4kZWxlbWVudC5vbihldmVudE91dCArICcuJyArIHRoaXMudHlwZSwgdGhpcy5vcHRpb25zLnNlbGVjdG9yLCAkLnByb3h5KHRoaXMubGVhdmUsIHRoaXMpKVxuICAgICAgfVxuICAgIH1cblxuICAgIHRoaXMub3B0aW9ucy5zZWxlY3RvciA/XG4gICAgICAodGhpcy5fb3B0aW9ucyA9ICQuZXh0ZW5kKHt9LCB0aGlzLm9wdGlvbnMsIHsgdHJpZ2dlcjogJ21hbnVhbCcsIHNlbGVjdG9yOiAnJyB9KSkgOlxuICAgICAgdGhpcy5maXhUaXRsZSgpXG4gIH1cblxuICBUb29sdGlwLnByb3RvdHlwZS5nZXREZWZhdWx0cyA9IGZ1bmN0aW9uICgpIHtcbiAgICByZXR1cm4gVG9vbHRpcC5ERUZBVUxUU1xuICB9XG5cbiAgVG9vbHRpcC5wcm90b3R5cGUuZ2V0T3B0aW9ucyA9IGZ1bmN0aW9uIChvcHRpb25zKSB7XG4gICAgdmFyIGRhdGFBdHRyaWJ1dGVzID0gdGhpcy4kZWxlbWVudC5kYXRhKClcblxuICAgIGZvciAodmFyIGRhdGFBdHRyIGluIGRhdGFBdHRyaWJ1dGVzKSB7XG4gICAgICBpZiAoZGF0YUF0dHJpYnV0ZXMuaGFzT3duUHJvcGVydHkoZGF0YUF0dHIpICYmICQuaW5BcnJheShkYXRhQXR0ciwgRElTQUxMT1dFRF9BVFRSSUJVVEVTKSAhPT0gLTEpIHtcbiAgICAgICAgZGVsZXRlIGRhdGFBdHRyaWJ1dGVzW2RhdGFBdHRyXVxuICAgICAgfVxuICAgIH1cblxuICAgIG9wdGlvbnMgPSAkLmV4dGVuZCh7fSwgdGhpcy5nZXREZWZhdWx0cygpLCBkYXRhQXR0cmlidXRlcywgb3B0aW9ucylcblxuICAgIGlmIChvcHRpb25zLmRlbGF5ICYmIHR5cGVvZiBvcHRpb25zLmRlbGF5ID09ICdudW1iZXInKSB7XG4gICAgICBvcHRpb25zLmRlbGF5ID0ge1xuICAgICAgICBzaG93OiBvcHRpb25zLmRlbGF5LFxuICAgICAgICBoaWRlOiBvcHRpb25zLmRlbGF5XG4gICAgICB9XG4gICAgfVxuXG4gICAgaWYgKG9wdGlvbnMuc2FuaXRpemUpIHtcbiAgICAgIG9wdGlvbnMudGVtcGxhdGUgPSBzYW5pdGl6ZUh0bWwob3B0aW9ucy50ZW1wbGF0ZSwgb3B0aW9ucy53aGl0ZUxpc3QsIG9wdGlvbnMuc2FuaXRpemVGbilcbiAgICB9XG5cbiAgICByZXR1cm4gb3B0aW9uc1xuICB9XG5cbiAgVG9vbHRpcC5wcm90b3R5cGUuZ2V0RGVsZWdhdGVPcHRpb25zID0gZnVuY3Rpb24gKCkge1xuICAgIHZhciBvcHRpb25zICA9IHt9XG4gICAgdmFyIGRlZmF1bHRzID0gdGhpcy5nZXREZWZhdWx0cygpXG5cbiAgICB0aGlzLl9vcHRpb25zICYmICQuZWFjaCh0aGlzLl9vcHRpb25zLCBmdW5jdGlvbiAoa2V5LCB2YWx1ZSkge1xuICAgICAgaWYgKGRlZmF1bHRzW2tleV0gIT0gdmFsdWUpIG9wdGlvbnNba2V5XSA9IHZhbHVlXG4gICAgfSlcblxuICAgIHJldHVybiBvcHRpb25zXG4gIH1cblxuICBUb29sdGlwLnByb3RvdHlwZS5lbnRlciA9IGZ1bmN0aW9uIChvYmopIHtcbiAgICB2YXIgc2VsZiA9IG9iaiBpbnN0YW5jZW9mIHRoaXMuY29uc3RydWN0b3IgP1xuICAgICAgb2JqIDogJChvYmouY3VycmVudFRhcmdldCkuZGF0YSgnYnMuJyArIHRoaXMudHlwZSlcblxuICAgIGlmICghc2VsZikge1xuICAgICAgc2VsZiA9IG5ldyB0aGlzLmNvbnN0cnVjdG9yKG9iai5jdXJyZW50VGFyZ2V0LCB0aGlzLmdldERlbGVnYXRlT3B0aW9ucygpKVxuICAgICAgJChvYmouY3VycmVudFRhcmdldCkuZGF0YSgnYnMuJyArIHRoaXMudHlwZSwgc2VsZilcbiAgICB9XG5cbiAgICBpZiAob2JqIGluc3RhbmNlb2YgJC5FdmVudCkge1xuICAgICAgc2VsZi5pblN0YXRlW29iai50eXBlID09ICdmb2N1c2luJyA/ICdmb2N1cycgOiAnaG92ZXInXSA9IHRydWVcbiAgICB9XG5cbiAgICBpZiAoc2VsZi50aXAoKS5oYXNDbGFzcygnaW4nKSB8fCBzZWxmLmhvdmVyU3RhdGUgPT0gJ2luJykge1xuICAgICAgc2VsZi5ob3ZlclN0YXRlID0gJ2luJ1xuICAgICAgcmV0dXJuXG4gICAgfVxuXG4gICAgY2xlYXJUaW1lb3V0KHNlbGYudGltZW91dClcblxuICAgIHNlbGYuaG92ZXJTdGF0ZSA9ICdpbidcblxuICAgIGlmICghc2VsZi5vcHRpb25zLmRlbGF5IHx8ICFzZWxmLm9wdGlvbnMuZGVsYXkuc2hvdykgcmV0dXJuIHNlbGYuc2hvdygpXG5cbiAgICBzZWxmLnRpbWVvdXQgPSBzZXRUaW1lb3V0KGZ1bmN0aW9uICgpIHtcbiAgICAgIGlmIChzZWxmLmhvdmVyU3RhdGUgPT0gJ2luJykgc2VsZi5zaG93KClcbiAgICB9LCBzZWxmLm9wdGlvbnMuZGVsYXkuc2hvdylcbiAgfVxuXG4gIFRvb2x0aXAucHJvdG90eXBlLmlzSW5TdGF0ZVRydWUgPSBmdW5jdGlvbiAoKSB7XG4gICAgZm9yICh2YXIga2V5IGluIHRoaXMuaW5TdGF0ZSkge1xuICAgICAgaWYgKHRoaXMuaW5TdGF0ZVtrZXldKSByZXR1cm4gdHJ1ZVxuICAgIH1cblxuICAgIHJldHVybiBmYWxzZVxuICB9XG5cbiAgVG9vbHRpcC5wcm90b3R5cGUubGVhdmUgPSBmdW5jdGlvbiAob2JqKSB7XG4gICAgdmFyIHNlbGYgPSBvYmogaW5zdGFuY2VvZiB0aGlzLmNvbnN0cnVjdG9yID9cbiAgICAgIG9iaiA6ICQob2JqLmN1cnJlbnRUYXJnZXQpLmRhdGEoJ2JzLicgKyB0aGlzLnR5cGUpXG5cbiAgICBpZiAoIXNlbGYpIHtcbiAgICAgIHNlbGYgPSBuZXcgdGhpcy5jb25zdHJ1Y3RvcihvYmouY3VycmVudFRhcmdldCwgdGhpcy5nZXREZWxlZ2F0ZU9wdGlvbnMoKSlcbiAgICAgICQob2JqLmN1cnJlbnRUYXJnZXQpLmRhdGEoJ2JzLicgKyB0aGlzLnR5cGUsIHNlbGYpXG4gICAgfVxuXG4gICAgaWYgKG9iaiBpbnN0YW5jZW9mICQuRXZlbnQpIHtcbiAgICAgIHNlbGYuaW5TdGF0ZVtvYmoudHlwZSA9PSAnZm9jdXNvdXQnID8gJ2ZvY3VzJyA6ICdob3ZlciddID0gZmFsc2VcbiAgICB9XG5cbiAgICBpZiAoc2VsZi5pc0luU3RhdGVUcnVlKCkpIHJldHVyblxuXG4gICAgY2xlYXJUaW1lb3V0KHNlbGYudGltZW91dClcblxuICAgIHNlbGYuaG92ZXJTdGF0ZSA9ICdvdXQnXG5cbiAgICBpZiAoIXNlbGYub3B0aW9ucy5kZWxheSB8fCAhc2VsZi5vcHRpb25zLmRlbGF5LmhpZGUpIHJldHVybiBzZWxmLmhpZGUoKVxuXG4gICAgc2VsZi50aW1lb3V0ID0gc2V0VGltZW91dChmdW5jdGlvbiAoKSB7XG4gICAgICBpZiAoc2VsZi5ob3ZlclN0YXRlID09ICdvdXQnKSBzZWxmLmhpZGUoKVxuICAgIH0sIHNlbGYub3B0aW9ucy5kZWxheS5oaWRlKVxuICB9XG5cbiAgVG9vbHRpcC5wcm90b3R5cGUuc2hvdyA9IGZ1bmN0aW9uICgpIHtcbiAgICB2YXIgZSA9ICQuRXZlbnQoJ3Nob3cuYnMuJyArIHRoaXMudHlwZSlcblxuICAgIGlmICh0aGlzLmhhc0NvbnRlbnQoKSAmJiB0aGlzLmVuYWJsZWQpIHtcbiAgICAgIHRoaXMuJGVsZW1lbnQudHJpZ2dlcihlKVxuXG4gICAgICB2YXIgaW5Eb20gPSAkLmNvbnRhaW5zKHRoaXMuJGVsZW1lbnRbMF0ub3duZXJEb2N1bWVudC5kb2N1bWVudEVsZW1lbnQsIHRoaXMuJGVsZW1lbnRbMF0pXG4gICAgICBpZiAoZS5pc0RlZmF1bHRQcmV2ZW50ZWQoKSB8fCAhaW5Eb20pIHJldHVyblxuICAgICAgdmFyIHRoYXQgPSB0aGlzXG5cbiAgICAgIHZhciAkdGlwID0gdGhpcy50aXAoKVxuXG4gICAgICB2YXIgdGlwSWQgPSB0aGlzLmdldFVJRCh0aGlzLnR5cGUpXG5cbiAgICAgIHRoaXMuc2V0Q29udGVudCgpXG4gICAgICAkdGlwLmF0dHIoJ2lkJywgdGlwSWQpXG4gICAgICB0aGlzLiRlbGVtZW50LmF0dHIoJ2FyaWEtZGVzY3JpYmVkYnknLCB0aXBJZClcblxuICAgICAgaWYgKHRoaXMub3B0aW9ucy5hbmltYXRpb24pICR0aXAuYWRkQ2xhc3MoJ2ZhZGUnKVxuXG4gICAgICB2YXIgcGxhY2VtZW50ID0gdHlwZW9mIHRoaXMub3B0aW9ucy5wbGFjZW1lbnQgPT0gJ2Z1bmN0aW9uJyA/XG4gICAgICAgIHRoaXMub3B0aW9ucy5wbGFjZW1lbnQuY2FsbCh0aGlzLCAkdGlwWzBdLCB0aGlzLiRlbGVtZW50WzBdKSA6XG4gICAgICAgIHRoaXMub3B0aW9ucy5wbGFjZW1lbnRcblxuICAgICAgdmFyIGF1dG9Ub2tlbiA9IC9cXHM/YXV0bz9cXHM/L2lcbiAgICAgIHZhciBhdXRvUGxhY2UgPSBhdXRvVG9rZW4udGVzdChwbGFjZW1lbnQpXG4gICAgICBpZiAoYXV0b1BsYWNlKSBwbGFjZW1lbnQgPSBwbGFjZW1lbnQucmVwbGFjZShhdXRvVG9rZW4sICcnKSB8fCAndG9wJ1xuXG4gICAgICAkdGlwXG4gICAgICAgIC5kZXRhY2goKVxuICAgICAgICAuY3NzKHsgdG9wOiAwLCBsZWZ0OiAwLCBkaXNwbGF5OiAnYmxvY2snIH0pXG4gICAgICAgIC5hZGRDbGFzcyhwbGFjZW1lbnQpXG4gICAgICAgIC5kYXRhKCdicy4nICsgdGhpcy50eXBlLCB0aGlzKVxuXG4gICAgICB0aGlzLm9wdGlvbnMuY29udGFpbmVyID8gJHRpcC5hcHBlbmRUbygkKGRvY3VtZW50KS5maW5kKHRoaXMub3B0aW9ucy5jb250YWluZXIpKSA6ICR0aXAuaW5zZXJ0QWZ0ZXIodGhpcy4kZWxlbWVudClcbiAgICAgIHRoaXMuJGVsZW1lbnQudHJpZ2dlcignaW5zZXJ0ZWQuYnMuJyArIHRoaXMudHlwZSlcblxuICAgICAgdmFyIHBvcyAgICAgICAgICA9IHRoaXMuZ2V0UG9zaXRpb24oKVxuICAgICAgdmFyIGFjdHVhbFdpZHRoICA9ICR0aXBbMF0ub2Zmc2V0V2lkdGhcbiAgICAgIHZhciBhY3R1YWxIZWlnaHQgPSAkdGlwWzBdLm9mZnNldEhlaWdodFxuXG4gICAgICBpZiAoYXV0b1BsYWNlKSB7XG4gICAgICAgIHZhciBvcmdQbGFjZW1lbnQgPSBwbGFjZW1lbnRcbiAgICAgICAgdmFyIHZpZXdwb3J0RGltID0gdGhpcy5nZXRQb3NpdGlvbih0aGlzLiR2aWV3cG9ydClcblxuICAgICAgICBwbGFjZW1lbnQgPSBwbGFjZW1lbnQgPT0gJ2JvdHRvbScgJiYgcG9zLmJvdHRvbSArIGFjdHVhbEhlaWdodCA+IHZpZXdwb3J0RGltLmJvdHRvbSA/ICd0b3AnICAgIDpcbiAgICAgICAgICAgICAgICAgICAgcGxhY2VtZW50ID09ICd0b3AnICAgICYmIHBvcy50b3AgICAgLSBhY3R1YWxIZWlnaHQgPCB2aWV3cG9ydERpbS50b3AgICAgPyAnYm90dG9tJyA6XG4gICAgICAgICAgICAgICAgICAgIHBsYWNlbWVudCA9PSAncmlnaHQnICAmJiBwb3MucmlnaHQgICsgYWN0dWFsV2lkdGggID4gdmlld3BvcnREaW0ud2lkdGggID8gJ2xlZnQnICAgOlxuICAgICAgICAgICAgICAgICAgICBwbGFjZW1lbnQgPT0gJ2xlZnQnICAgJiYgcG9zLmxlZnQgICAtIGFjdHVhbFdpZHRoICA8IHZpZXdwb3J0RGltLmxlZnQgICA/ICdyaWdodCcgIDpcbiAgICAgICAgICAgICAgICAgICAgcGxhY2VtZW50XG5cbiAgICAgICAgJHRpcFxuICAgICAgICAgIC5yZW1vdmVDbGFzcyhvcmdQbGFjZW1lbnQpXG4gICAgICAgICAgLmFkZENsYXNzKHBsYWNlbWVudClcbiAgICAgIH1cblxuICAgICAgdmFyIGNhbGN1bGF0ZWRPZmZzZXQgPSB0aGlzLmdldENhbGN1bGF0ZWRPZmZzZXQocGxhY2VtZW50LCBwb3MsIGFjdHVhbFdpZHRoLCBhY3R1YWxIZWlnaHQpXG5cbiAgICAgIHRoaXMuYXBwbHlQbGFjZW1lbnQoY2FsY3VsYXRlZE9mZnNldCwgcGxhY2VtZW50KVxuXG4gICAgICB2YXIgY29tcGxldGUgPSBmdW5jdGlvbiAoKSB7XG4gICAgICAgIHZhciBwcmV2SG92ZXJTdGF0ZSA9IHRoYXQuaG92ZXJTdGF0ZVxuICAgICAgICB0aGF0LiRlbGVtZW50LnRyaWdnZXIoJ3Nob3duLmJzLicgKyB0aGF0LnR5cGUpXG4gICAgICAgIHRoYXQuaG92ZXJTdGF0ZSA9IG51bGxcblxuICAgICAgICBpZiAocHJldkhvdmVyU3RhdGUgPT0gJ291dCcpIHRoYXQubGVhdmUodGhhdClcbiAgICAgIH1cblxuICAgICAgJC5zdXBwb3J0LnRyYW5zaXRpb24gJiYgdGhpcy4kdGlwLmhhc0NsYXNzKCdmYWRlJykgP1xuICAgICAgICAkdGlwXG4gICAgICAgICAgLm9uZSgnYnNUcmFuc2l0aW9uRW5kJywgY29tcGxldGUpXG4gICAgICAgICAgLmVtdWxhdGVUcmFuc2l0aW9uRW5kKFRvb2x0aXAuVFJBTlNJVElPTl9EVVJBVElPTikgOlxuICAgICAgICBjb21wbGV0ZSgpXG4gICAgfVxuICB9XG5cbiAgVG9vbHRpcC5wcm90b3R5cGUuYXBwbHlQbGFjZW1lbnQgPSBmdW5jdGlvbiAob2Zmc2V0LCBwbGFjZW1lbnQpIHtcbiAgICB2YXIgJHRpcCAgID0gdGhpcy50aXAoKVxuICAgIHZhciB3aWR0aCAgPSAkdGlwWzBdLm9mZnNldFdpZHRoXG4gICAgdmFyIGhlaWdodCA9ICR0aXBbMF0ub2Zmc2V0SGVpZ2h0XG5cbiAgICAvLyBtYW51YWxseSByZWFkIG1hcmdpbnMgYmVjYXVzZSBnZXRCb3VuZGluZ0NsaWVudFJlY3QgaW5jbHVkZXMgZGlmZmVyZW5jZVxuICAgIHZhciBtYXJnaW5Ub3AgPSBwYXJzZUludCgkdGlwLmNzcygnbWFyZ2luLXRvcCcpLCAxMClcbiAgICB2YXIgbWFyZ2luTGVmdCA9IHBhcnNlSW50KCR0aXAuY3NzKCdtYXJnaW4tbGVmdCcpLCAxMClcblxuICAgIC8vIHdlIG11c3QgY2hlY2sgZm9yIE5hTiBmb3IgaWUgOC85XG4gICAgaWYgKGlzTmFOKG1hcmdpblRvcCkpICBtYXJnaW5Ub3AgID0gMFxuICAgIGlmIChpc05hTihtYXJnaW5MZWZ0KSkgbWFyZ2luTGVmdCA9IDBcblxuICAgIG9mZnNldC50b3AgICs9IG1hcmdpblRvcFxuICAgIG9mZnNldC5sZWZ0ICs9IG1hcmdpbkxlZnRcblxuICAgIC8vICQuZm4ub2Zmc2V0IGRvZXNuJ3Qgcm91bmQgcGl4ZWwgdmFsdWVzXG4gICAgLy8gc28gd2UgdXNlIHNldE9mZnNldCBkaXJlY3RseSB3aXRoIG91ciBvd24gZnVuY3Rpb24gQi0wXG4gICAgJC5vZmZzZXQuc2V0T2Zmc2V0KCR0aXBbMF0sICQuZXh0ZW5kKHtcbiAgICAgIHVzaW5nOiBmdW5jdGlvbiAocHJvcHMpIHtcbiAgICAgICAgJHRpcC5jc3Moe1xuICAgICAgICAgIHRvcDogTWF0aC5yb3VuZChwcm9wcy50b3ApLFxuICAgICAgICAgIGxlZnQ6IE1hdGgucm91bmQocHJvcHMubGVmdClcbiAgICAgICAgfSlcbiAgICAgIH1cbiAgICB9LCBvZmZzZXQpLCAwKVxuXG4gICAgJHRpcC5hZGRDbGFzcygnaW4nKVxuXG4gICAgLy8gY2hlY2sgdG8gc2VlIGlmIHBsYWNpbmcgdGlwIGluIG5ldyBvZmZzZXQgY2F1c2VkIHRoZSB0aXAgdG8gcmVzaXplIGl0c2VsZlxuICAgIHZhciBhY3R1YWxXaWR0aCAgPSAkdGlwWzBdLm9mZnNldFdpZHRoXG4gICAgdmFyIGFjdHVhbEhlaWdodCA9ICR0aXBbMF0ub2Zmc2V0SGVpZ2h0XG5cbiAgICBpZiAocGxhY2VtZW50ID09ICd0b3AnICYmIGFjdHVhbEhlaWdodCAhPSBoZWlnaHQpIHtcbiAgICAgIG9mZnNldC50b3AgPSBvZmZzZXQudG9wICsgaGVpZ2h0IC0gYWN0dWFsSGVpZ2h0XG4gICAgfVxuXG4gICAgdmFyIGRlbHRhID0gdGhpcy5nZXRWaWV3cG9ydEFkanVzdGVkRGVsdGEocGxhY2VtZW50LCBvZmZzZXQsIGFjdHVhbFdpZHRoLCBhY3R1YWxIZWlnaHQpXG5cbiAgICBpZiAoZGVsdGEubGVmdCkgb2Zmc2V0LmxlZnQgKz0gZGVsdGEubGVmdFxuICAgIGVsc2Ugb2Zmc2V0LnRvcCArPSBkZWx0YS50b3BcblxuICAgIHZhciBpc1ZlcnRpY2FsICAgICAgICAgID0gL3RvcHxib3R0b20vLnRlc3QocGxhY2VtZW50KVxuICAgIHZhciBhcnJvd0RlbHRhICAgICAgICAgID0gaXNWZXJ0aWNhbCA/IGRlbHRhLmxlZnQgKiAyIC0gd2lkdGggKyBhY3R1YWxXaWR0aCA6IGRlbHRhLnRvcCAqIDIgLSBoZWlnaHQgKyBhY3R1YWxIZWlnaHRcbiAgICB2YXIgYXJyb3dPZmZzZXRQb3NpdGlvbiA9IGlzVmVydGljYWwgPyAnb2Zmc2V0V2lkdGgnIDogJ29mZnNldEhlaWdodCdcblxuICAgICR0aXAub2Zmc2V0KG9mZnNldClcbiAgICB0aGlzLnJlcGxhY2VBcnJvdyhhcnJvd0RlbHRhLCAkdGlwWzBdW2Fycm93T2Zmc2V0UG9zaXRpb25dLCBpc1ZlcnRpY2FsKVxuICB9XG5cbiAgVG9vbHRpcC5wcm90b3R5cGUucmVwbGFjZUFycm93ID0gZnVuY3Rpb24gKGRlbHRhLCBkaW1lbnNpb24sIGlzVmVydGljYWwpIHtcbiAgICB0aGlzLmFycm93KClcbiAgICAgIC5jc3MoaXNWZXJ0aWNhbCA/ICdsZWZ0JyA6ICd0b3AnLCA1MCAqICgxIC0gZGVsdGEgLyBkaW1lbnNpb24pICsgJyUnKVxuICAgICAgLmNzcyhpc1ZlcnRpY2FsID8gJ3RvcCcgOiAnbGVmdCcsICcnKVxuICB9XG5cbiAgVG9vbHRpcC5wcm90b3R5cGUuc2V0Q29udGVudCA9IGZ1bmN0aW9uICgpIHtcbiAgICB2YXIgJHRpcCAgPSB0aGlzLnRpcCgpXG4gICAgdmFyIHRpdGxlID0gdGhpcy5nZXRUaXRsZSgpXG5cbiAgICBpZiAodGhpcy5vcHRpb25zLmh0bWwpIHtcbiAgICAgIGlmICh0aGlzLm9wdGlvbnMuc2FuaXRpemUpIHtcbiAgICAgICAgdGl0bGUgPSBzYW5pdGl6ZUh0bWwodGl0bGUsIHRoaXMub3B0aW9ucy53aGl0ZUxpc3QsIHRoaXMub3B0aW9ucy5zYW5pdGl6ZUZuKVxuICAgICAgfVxuXG4gICAgICAkdGlwLmZpbmQoJy50b29sdGlwLWlubmVyJykuaHRtbCh0aXRsZSlcbiAgICB9IGVsc2Uge1xuICAgICAgJHRpcC5maW5kKCcudG9vbHRpcC1pbm5lcicpLnRleHQodGl0bGUpXG4gICAgfVxuXG4gICAgJHRpcC5yZW1vdmVDbGFzcygnZmFkZSBpbiB0b3AgYm90dG9tIGxlZnQgcmlnaHQnKVxuICB9XG5cbiAgVG9vbHRpcC5wcm90b3R5cGUuaGlkZSA9IGZ1bmN0aW9uIChjYWxsYmFjaykge1xuICAgIHZhciB0aGF0ID0gdGhpc1xuICAgIHZhciAkdGlwID0gJCh0aGlzLiR0aXApXG4gICAgdmFyIGUgICAgPSAkLkV2ZW50KCdoaWRlLmJzLicgKyB0aGlzLnR5cGUpXG5cbiAgICBmdW5jdGlvbiBjb21wbGV0ZSgpIHtcbiAgICAgIGlmICh0aGF0LmhvdmVyU3RhdGUgIT0gJ2luJykgJHRpcC5kZXRhY2goKVxuICAgICAgaWYgKHRoYXQuJGVsZW1lbnQpIHsgLy8gVE9ETzogQ2hlY2sgd2hldGhlciBndWFyZGluZyB0aGlzIGNvZGUgd2l0aCB0aGlzIGBpZmAgaXMgcmVhbGx5IG5lY2Vzc2FyeS5cbiAgICAgICAgdGhhdC4kZWxlbWVudFxuICAgICAgICAgIC5yZW1vdmVBdHRyKCdhcmlhLWRlc2NyaWJlZGJ5JylcbiAgICAgICAgICAudHJpZ2dlcignaGlkZGVuLmJzLicgKyB0aGF0LnR5cGUpXG4gICAgICB9XG4gICAgICBjYWxsYmFjayAmJiBjYWxsYmFjaygpXG4gICAgfVxuXG4gICAgdGhpcy4kZWxlbWVudC50cmlnZ2VyKGUpXG5cbiAgICBpZiAoZS5pc0RlZmF1bHRQcmV2ZW50ZWQoKSkgcmV0dXJuXG5cbiAgICAkdGlwLnJlbW92ZUNsYXNzKCdpbicpXG5cbiAgICAkLnN1cHBvcnQudHJhbnNpdGlvbiAmJiAkdGlwLmhhc0NsYXNzKCdmYWRlJykgP1xuICAgICAgJHRpcFxuICAgICAgICAub25lKCdic1RyYW5zaXRpb25FbmQnLCBjb21wbGV0ZSlcbiAgICAgICAgLmVtdWxhdGVUcmFuc2l0aW9uRW5kKFRvb2x0aXAuVFJBTlNJVElPTl9EVVJBVElPTikgOlxuICAgICAgY29tcGxldGUoKVxuXG4gICAgdGhpcy5ob3ZlclN0YXRlID0gbnVsbFxuXG4gICAgcmV0dXJuIHRoaXNcbiAgfVxuXG4gIFRvb2x0aXAucHJvdG90eXBlLmZpeFRpdGxlID0gZnVuY3Rpb24gKCkge1xuICAgIHZhciAkZSA9IHRoaXMuJGVsZW1lbnRcbiAgICBpZiAoJGUuYXR0cigndGl0bGUnKSB8fCB0eXBlb2YgJGUuYXR0cignZGF0YS1vcmlnaW5hbC10aXRsZScpICE9ICdzdHJpbmcnKSB7XG4gICAgICAkZS5hdHRyKCdkYXRhLW9yaWdpbmFsLXRpdGxlJywgJGUuYXR0cigndGl0bGUnKSB8fCAnJykuYXR0cigndGl0bGUnLCAnJylcbiAgICB9XG4gIH1cblxuICBUb29sdGlwLnByb3RvdHlwZS5oYXNDb250ZW50ID0gZnVuY3Rpb24gKCkge1xuICAgIHJldHVybiB0aGlzLmdldFRpdGxlKClcbiAgfVxuXG4gIFRvb2x0aXAucHJvdG90eXBlLmdldFBvc2l0aW9uID0gZnVuY3Rpb24gKCRlbGVtZW50KSB7XG4gICAgJGVsZW1lbnQgICA9ICRlbGVtZW50IHx8IHRoaXMuJGVsZW1lbnRcblxuICAgIHZhciBlbCAgICAgPSAkZWxlbWVudFswXVxuICAgIHZhciBpc0JvZHkgPSBlbC50YWdOYW1lID09ICdCT0RZJ1xuXG4gICAgdmFyIGVsUmVjdCAgICA9IGVsLmdldEJvdW5kaW5nQ2xpZW50UmVjdCgpXG4gICAgaWYgKGVsUmVjdC53aWR0aCA9PSBudWxsKSB7XG4gICAgICAvLyB3aWR0aCBhbmQgaGVpZ2h0IGFyZSBtaXNzaW5nIGluIElFOCwgc28gY29tcHV0ZSB0aGVtIG1hbnVhbGx5OyBzZWUgaHR0cHM6Ly9naXRodWIuY29tL3R3YnMvYm9vdHN0cmFwL2lzc3Vlcy8xNDA5M1xuICAgICAgZWxSZWN0ID0gJC5leHRlbmQoe30sIGVsUmVjdCwgeyB3aWR0aDogZWxSZWN0LnJpZ2h0IC0gZWxSZWN0LmxlZnQsIGhlaWdodDogZWxSZWN0LmJvdHRvbSAtIGVsUmVjdC50b3AgfSlcbiAgICB9XG4gICAgdmFyIGlzU3ZnID0gd2luZG93LlNWR0VsZW1lbnQgJiYgZWwgaW5zdGFuY2VvZiB3aW5kb3cuU1ZHRWxlbWVudFxuICAgIC8vIEF2b2lkIHVzaW5nICQub2Zmc2V0KCkgb24gU1ZHcyBzaW5jZSBpdCBnaXZlcyBpbmNvcnJlY3QgcmVzdWx0cyBpbiBqUXVlcnkgMy5cbiAgICAvLyBTZWUgaHR0cHM6Ly9naXRodWIuY29tL3R3YnMvYm9vdHN0cmFwL2lzc3Vlcy8yMDI4MFxuICAgIHZhciBlbE9mZnNldCAgPSBpc0JvZHkgPyB7IHRvcDogMCwgbGVmdDogMCB9IDogKGlzU3ZnID8gbnVsbCA6ICRlbGVtZW50Lm9mZnNldCgpKVxuICAgIHZhciBzY3JvbGwgICAgPSB7IHNjcm9sbDogaXNCb2R5ID8gZG9jdW1lbnQuZG9jdW1lbnRFbGVtZW50LnNjcm9sbFRvcCB8fCBkb2N1bWVudC5ib2R5LnNjcm9sbFRvcCA6ICRlbGVtZW50LnNjcm9sbFRvcCgpIH1cbiAgICB2YXIgb3V0ZXJEaW1zID0gaXNCb2R5ID8geyB3aWR0aDogJCh3aW5kb3cpLndpZHRoKCksIGhlaWdodDogJCh3aW5kb3cpLmhlaWdodCgpIH0gOiBudWxsXG5cbiAgICByZXR1cm4gJC5leHRlbmQoe30sIGVsUmVjdCwgc2Nyb2xsLCBvdXRlckRpbXMsIGVsT2Zmc2V0KVxuICB9XG5cbiAgVG9vbHRpcC5wcm90b3R5cGUuZ2V0Q2FsY3VsYXRlZE9mZnNldCA9IGZ1bmN0aW9uIChwbGFjZW1lbnQsIHBvcywgYWN0dWFsV2lkdGgsIGFjdHVhbEhlaWdodCkge1xuICAgIHJldHVybiBwbGFjZW1lbnQgPT0gJ2JvdHRvbScgPyB7IHRvcDogcG9zLnRvcCArIHBvcy5oZWlnaHQsICAgbGVmdDogcG9zLmxlZnQgKyBwb3Mud2lkdGggLyAyIC0gYWN0dWFsV2lkdGggLyAyIH0gOlxuICAgICAgICAgICBwbGFjZW1lbnQgPT0gJ3RvcCcgICAgPyB7IHRvcDogcG9zLnRvcCAtIGFjdHVhbEhlaWdodCwgbGVmdDogcG9zLmxlZnQgKyBwb3Mud2lkdGggLyAyIC0gYWN0dWFsV2lkdGggLyAyIH0gOlxuICAgICAgICAgICBwbGFjZW1lbnQgPT0gJ2xlZnQnICAgPyB7IHRvcDogcG9zLnRvcCArIHBvcy5oZWlnaHQgLyAyIC0gYWN0dWFsSGVpZ2h0IC8gMiwgbGVmdDogcG9zLmxlZnQgLSBhY3R1YWxXaWR0aCB9IDpcbiAgICAgICAgLyogcGxhY2VtZW50ID09ICdyaWdodCcgKi8geyB0b3A6IHBvcy50b3AgKyBwb3MuaGVpZ2h0IC8gMiAtIGFjdHVhbEhlaWdodCAvIDIsIGxlZnQ6IHBvcy5sZWZ0ICsgcG9zLndpZHRoIH1cblxuICB9XG5cbiAgVG9vbHRpcC5wcm90b3R5cGUuZ2V0Vmlld3BvcnRBZGp1c3RlZERlbHRhID0gZnVuY3Rpb24gKHBsYWNlbWVudCwgcG9zLCBhY3R1YWxXaWR0aCwgYWN0dWFsSGVpZ2h0KSB7XG4gICAgdmFyIGRlbHRhID0geyB0b3A6IDAsIGxlZnQ6IDAgfVxuICAgIGlmICghdGhpcy4kdmlld3BvcnQpIHJldHVybiBkZWx0YVxuXG4gICAgdmFyIHZpZXdwb3J0UGFkZGluZyA9IHRoaXMub3B0aW9ucy52aWV3cG9ydCAmJiB0aGlzLm9wdGlvbnMudmlld3BvcnQucGFkZGluZyB8fCAwXG4gICAgdmFyIHZpZXdwb3J0RGltZW5zaW9ucyA9IHRoaXMuZ2V0UG9zaXRpb24odGhpcy4kdmlld3BvcnQpXG5cbiAgICBpZiAoL3JpZ2h0fGxlZnQvLnRlc3QocGxhY2VtZW50KSkge1xuICAgICAgdmFyIHRvcEVkZ2VPZmZzZXQgICAgPSBwb3MudG9wIC0gdmlld3BvcnRQYWRkaW5nIC0gdmlld3BvcnREaW1lbnNpb25zLnNjcm9sbFxuICAgICAgdmFyIGJvdHRvbUVkZ2VPZmZzZXQgPSBwb3MudG9wICsgdmlld3BvcnRQYWRkaW5nIC0gdmlld3BvcnREaW1lbnNpb25zLnNjcm9sbCArIGFjdHVhbEhlaWdodFxuICAgICAgaWYgKHRvcEVkZ2VPZmZzZXQgPCB2aWV3cG9ydERpbWVuc2lvbnMudG9wKSB7IC8vIHRvcCBvdmVyZmxvd1xuICAgICAgICBkZWx0YS50b3AgPSB2aWV3cG9ydERpbWVuc2lvbnMudG9wIC0gdG9wRWRnZU9mZnNldFxuICAgICAgfSBlbHNlIGlmIChib3R0b21FZGdlT2Zmc2V0ID4gdmlld3BvcnREaW1lbnNpb25zLnRvcCArIHZpZXdwb3J0RGltZW5zaW9ucy5oZWlnaHQpIHsgLy8gYm90dG9tIG92ZXJmbG93XG4gICAgICAgIGRlbHRhLnRvcCA9IHZpZXdwb3J0RGltZW5zaW9ucy50b3AgKyB2aWV3cG9ydERpbWVuc2lvbnMuaGVpZ2h0IC0gYm90dG9tRWRnZU9mZnNldFxuICAgICAgfVxuICAgIH0gZWxzZSB7XG4gICAgICB2YXIgbGVmdEVkZ2VPZmZzZXQgID0gcG9zLmxlZnQgLSB2aWV3cG9ydFBhZGRpbmdcbiAgICAgIHZhciByaWdodEVkZ2VPZmZzZXQgPSBwb3MubGVmdCArIHZpZXdwb3J0UGFkZGluZyArIGFjdHVhbFdpZHRoXG4gICAgICBpZiAobGVmdEVkZ2VPZmZzZXQgPCB2aWV3cG9ydERpbWVuc2lvbnMubGVmdCkgeyAvLyBsZWZ0IG92ZXJmbG93XG4gICAgICAgIGRlbHRhLmxlZnQgPSB2aWV3cG9ydERpbWVuc2lvbnMubGVmdCAtIGxlZnRFZGdlT2Zmc2V0XG4gICAgICB9IGVsc2UgaWYgKHJpZ2h0RWRnZU9mZnNldCA+IHZpZXdwb3J0RGltZW5zaW9ucy5yaWdodCkgeyAvLyByaWdodCBvdmVyZmxvd1xuICAgICAgICBkZWx0YS5sZWZ0ID0gdmlld3BvcnREaW1lbnNpb25zLmxlZnQgKyB2aWV3cG9ydERpbWVuc2lvbnMud2lkdGggLSByaWdodEVkZ2VPZmZzZXRcbiAgICAgIH1cbiAgICB9XG5cbiAgICByZXR1cm4gZGVsdGFcbiAgfVxuXG4gIFRvb2x0aXAucHJvdG90eXBlLmdldFRpdGxlID0gZnVuY3Rpb24gKCkge1xuICAgIHZhciB0aXRsZVxuICAgIHZhciAkZSA9IHRoaXMuJGVsZW1lbnRcbiAgICB2YXIgbyAgPSB0aGlzLm9wdGlvbnNcblxuICAgIHRpdGxlID0gJGUuYXR0cignZGF0YS1vcmlnaW5hbC10aXRsZScpXG4gICAgICB8fCAodHlwZW9mIG8udGl0bGUgPT0gJ2Z1bmN0aW9uJyA/IG8udGl0bGUuY2FsbCgkZVswXSkgOiAgby50aXRsZSlcblxuICAgIHJldHVybiB0aXRsZVxuICB9XG5cbiAgVG9vbHRpcC5wcm90b3R5cGUuZ2V0VUlEID0gZnVuY3Rpb24gKHByZWZpeCkge1xuICAgIGRvIHByZWZpeCArPSB+fihNYXRoLnJhbmRvbSgpICogMTAwMDAwMClcbiAgICB3aGlsZSAoZG9jdW1lbnQuZ2V0RWxlbWVudEJ5SWQocHJlZml4KSlcbiAgICByZXR1cm4gcHJlZml4XG4gIH1cblxuICBUb29sdGlwLnByb3RvdHlwZS50aXAgPSBmdW5jdGlvbiAoKSB7XG4gICAgaWYgKCF0aGlzLiR0aXApIHtcbiAgICAgIHRoaXMuJHRpcCA9ICQodGhpcy5vcHRpb25zLnRlbXBsYXRlKVxuICAgICAgaWYgKHRoaXMuJHRpcC5sZW5ndGggIT0gMSkge1xuICAgICAgICB0aHJvdyBuZXcgRXJyb3IodGhpcy50eXBlICsgJyBgdGVtcGxhdGVgIG9wdGlvbiBtdXN0IGNvbnNpc3Qgb2YgZXhhY3RseSAxIHRvcC1sZXZlbCBlbGVtZW50IScpXG4gICAgICB9XG4gICAgfVxuICAgIHJldHVybiB0aGlzLiR0aXBcbiAgfVxuXG4gIFRvb2x0aXAucHJvdG90eXBlLmFycm93ID0gZnVuY3Rpb24gKCkge1xuICAgIHJldHVybiAodGhpcy4kYXJyb3cgPSB0aGlzLiRhcnJvdyB8fCB0aGlzLnRpcCgpLmZpbmQoJy50b29sdGlwLWFycm93JykpXG4gIH1cblxuICBUb29sdGlwLnByb3RvdHlwZS5lbmFibGUgPSBmdW5jdGlvbiAoKSB7XG4gICAgdGhpcy5lbmFibGVkID0gdHJ1ZVxuICB9XG5cbiAgVG9vbHRpcC5wcm90b3R5cGUuZGlzYWJsZSA9IGZ1bmN0aW9uICgpIHtcbiAgICB0aGlzLmVuYWJsZWQgPSBmYWxzZVxuICB9XG5cbiAgVG9vbHRpcC5wcm90b3R5cGUudG9nZ2xlRW5hYmxlZCA9IGZ1bmN0aW9uICgpIHtcbiAgICB0aGlzLmVuYWJsZWQgPSAhdGhpcy5lbmFibGVkXG4gIH1cblxuICBUb29sdGlwLnByb3RvdHlwZS50b2dnbGUgPSBmdW5jdGlvbiAoZSkge1xuICAgIHZhciBzZWxmID0gdGhpc1xuICAgIGlmIChlKSB7XG4gICAgICBzZWxmID0gJChlLmN1cnJlbnRUYXJnZXQpLmRhdGEoJ2JzLicgKyB0aGlzLnR5cGUpXG4gICAgICBpZiAoIXNlbGYpIHtcbiAgICAgICAgc2VsZiA9IG5ldyB0aGlzLmNvbnN0cnVjdG9yKGUuY3VycmVudFRhcmdldCwgdGhpcy5nZXREZWxlZ2F0ZU9wdGlvbnMoKSlcbiAgICAgICAgJChlLmN1cnJlbnRUYXJnZXQpLmRhdGEoJ2JzLicgKyB0aGlzLnR5cGUsIHNlbGYpXG4gICAgICB9XG4gICAgfVxuXG4gICAgaWYgKGUpIHtcbiAgICAgIHNlbGYuaW5TdGF0ZS5jbGljayA9ICFzZWxmLmluU3RhdGUuY2xpY2tcbiAgICAgIGlmIChzZWxmLmlzSW5TdGF0ZVRydWUoKSkgc2VsZi5lbnRlcihzZWxmKVxuICAgICAgZWxzZSBzZWxmLmxlYXZlKHNlbGYpXG4gICAgfSBlbHNlIHtcbiAgICAgIHNlbGYudGlwKCkuaGFzQ2xhc3MoJ2luJykgPyBzZWxmLmxlYXZlKHNlbGYpIDogc2VsZi5lbnRlcihzZWxmKVxuICAgIH1cbiAgfVxuXG4gIFRvb2x0aXAucHJvdG90eXBlLmRlc3Ryb3kgPSBmdW5jdGlvbiAoKSB7XG4gICAgdmFyIHRoYXQgPSB0aGlzXG4gICAgY2xlYXJUaW1lb3V0KHRoaXMudGltZW91dClcbiAgICB0aGlzLmhpZGUoZnVuY3Rpb24gKCkge1xuICAgICAgdGhhdC4kZWxlbWVudC5vZmYoJy4nICsgdGhhdC50eXBlKS5yZW1vdmVEYXRhKCdicy4nICsgdGhhdC50eXBlKVxuICAgICAgaWYgKHRoYXQuJHRpcCkge1xuICAgICAgICB0aGF0LiR0aXAuZGV0YWNoKClcbiAgICAgIH1cbiAgICAgIHRoYXQuJHRpcCA9IG51bGxcbiAgICAgIHRoYXQuJGFycm93ID0gbnVsbFxuICAgICAgdGhhdC4kdmlld3BvcnQgPSBudWxsXG4gICAgICB0aGF0LiRlbGVtZW50ID0gbnVsbFxuICAgIH0pXG4gIH1cblxuICBUb29sdGlwLnByb3RvdHlwZS5zYW5pdGl6ZUh0bWwgPSBmdW5jdGlvbiAodW5zYWZlSHRtbCkge1xuICAgIHJldHVybiBzYW5pdGl6ZUh0bWwodW5zYWZlSHRtbCwgdGhpcy5vcHRpb25zLndoaXRlTGlzdCwgdGhpcy5vcHRpb25zLnNhbml0aXplRm4pXG4gIH1cblxuICAvLyBUT09MVElQIFBMVUdJTiBERUZJTklUSU9OXG4gIC8vID09PT09PT09PT09PT09PT09PT09PT09PT1cblxuICBmdW5jdGlvbiBQbHVnaW4ob3B0aW9uKSB7XG4gICAgcmV0dXJuIHRoaXMuZWFjaChmdW5jdGlvbiAoKSB7XG4gICAgICB2YXIgJHRoaXMgICA9ICQodGhpcylcbiAgICAgIHZhciBkYXRhICAgID0gJHRoaXMuZGF0YSgnYnMudG9vbHRpcCcpXG4gICAgICB2YXIgb3B0aW9ucyA9IHR5cGVvZiBvcHRpb24gPT0gJ29iamVjdCcgJiYgb3B0aW9uXG5cbiAgICAgIGlmICghZGF0YSAmJiAvZGVzdHJveXxoaWRlLy50ZXN0KG9wdGlvbikpIHJldHVyblxuICAgICAgaWYgKCFkYXRhKSAkdGhpcy5kYXRhKCdicy50b29sdGlwJywgKGRhdGEgPSBuZXcgVG9vbHRpcCh0aGlzLCBvcHRpb25zKSkpXG4gICAgICBpZiAodHlwZW9mIG9wdGlvbiA9PSAnc3RyaW5nJykgZGF0YVtvcHRpb25dKClcbiAgICB9KVxuICB9XG5cbiAgdmFyIG9sZCA9ICQuZm4udG9vbHRpcFxuXG4gICQuZm4udG9vbHRpcCAgICAgICAgICAgICA9IFBsdWdpblxuICAkLmZuLnRvb2x0aXAuQ29uc3RydWN0b3IgPSBUb29sdGlwXG5cblxuICAvLyBUT09MVElQIE5PIENPTkZMSUNUXG4gIC8vID09PT09PT09PT09PT09PT09PT1cblxuICAkLmZuLnRvb2x0aXAubm9Db25mbGljdCA9IGZ1bmN0aW9uICgpIHtcbiAgICAkLmZuLnRvb2x0aXAgPSBvbGRcbiAgICByZXR1cm4gdGhpc1xuICB9XG5cbn0oalF1ZXJ5KTtcblxuLyogPT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09XG4gKiBCb290c3RyYXA6IHBvcG92ZXIuanMgdjMuNC4xXG4gKiBodHRwczovL2dldGJvb3RzdHJhcC5jb20vZG9jcy8zLjQvamF2YXNjcmlwdC8jcG9wb3ZlcnNcbiAqID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PVxuICogQ29weXJpZ2h0IDIwMTEtMjAxOSBUd2l0dGVyLCBJbmMuXG4gKiBMaWNlbnNlZCB1bmRlciBNSVQgKGh0dHBzOi8vZ2l0aHViLmNvbS90d2JzL2Jvb3RzdHJhcC9ibG9iL21hc3Rlci9MSUNFTlNFKVxuICogPT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09ICovXG5cblxuK2Z1bmN0aW9uICgkKSB7XG4gICd1c2Ugc3RyaWN0JztcblxuICAvLyBQT1BPVkVSIFBVQkxJQyBDTEFTUyBERUZJTklUSU9OXG4gIC8vID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT1cblxuICB2YXIgUG9wb3ZlciA9IGZ1bmN0aW9uIChlbGVtZW50LCBvcHRpb25zKSB7XG4gICAgdGhpcy5pbml0KCdwb3BvdmVyJywgZWxlbWVudCwgb3B0aW9ucylcbiAgfVxuXG4gIGlmICghJC5mbi50b29sdGlwKSB0aHJvdyBuZXcgRXJyb3IoJ1BvcG92ZXIgcmVxdWlyZXMgdG9vbHRpcC5qcycpXG5cbiAgUG9wb3Zlci5WRVJTSU9OICA9ICczLjQuMSdcblxuICBQb3BvdmVyLkRFRkFVTFRTID0gJC5leHRlbmQoe30sICQuZm4udG9vbHRpcC5Db25zdHJ1Y3Rvci5ERUZBVUxUUywge1xuICAgIHBsYWNlbWVudDogJ3JpZ2h0JyxcbiAgICB0cmlnZ2VyOiAnY2xpY2snLFxuICAgIGNvbnRlbnQ6ICcnLFxuICAgIHRlbXBsYXRlOiAnPGRpdiBjbGFzcz1cInBvcG92ZXJcIiByb2xlPVwidG9vbHRpcFwiPjxkaXYgY2xhc3M9XCJhcnJvd1wiPjwvZGl2PjxoMyBjbGFzcz1cInBvcG92ZXItdGl0bGVcIj48L2gzPjxkaXYgY2xhc3M9XCJwb3BvdmVyLWNvbnRlbnRcIj48L2Rpdj48L2Rpdj4nXG4gIH0pXG5cblxuICAvLyBOT1RFOiBQT1BPVkVSIEVYVEVORFMgdG9vbHRpcC5qc1xuICAvLyA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PVxuXG4gIFBvcG92ZXIucHJvdG90eXBlID0gJC5leHRlbmQoe30sICQuZm4udG9vbHRpcC5Db25zdHJ1Y3Rvci5wcm90b3R5cGUpXG5cbiAgUG9wb3Zlci5wcm90b3R5cGUuY29uc3RydWN0b3IgPSBQb3BvdmVyXG5cbiAgUG9wb3Zlci5wcm90b3R5cGUuZ2V0RGVmYXVsdHMgPSBmdW5jdGlvbiAoKSB7XG4gICAgcmV0dXJuIFBvcG92ZXIuREVGQVVMVFNcbiAgfVxuXG4gIFBvcG92ZXIucHJvdG90eXBlLnNldENvbnRlbnQgPSBmdW5jdGlvbiAoKSB7XG4gICAgdmFyICR0aXAgICAgPSB0aGlzLnRpcCgpXG4gICAgdmFyIHRpdGxlICAgPSB0aGlzLmdldFRpdGxlKClcbiAgICB2YXIgY29udGVudCA9IHRoaXMuZ2V0Q29udGVudCgpXG5cbiAgICBpZiAodGhpcy5vcHRpb25zLmh0bWwpIHtcbiAgICAgIHZhciB0eXBlQ29udGVudCA9IHR5cGVvZiBjb250ZW50XG5cbiAgICAgIGlmICh0aGlzLm9wdGlvbnMuc2FuaXRpemUpIHtcbiAgICAgICAgdGl0bGUgPSB0aGlzLnNhbml0aXplSHRtbCh0aXRsZSlcblxuICAgICAgICBpZiAodHlwZUNvbnRlbnQgPT09ICdzdHJpbmcnKSB7XG4gICAgICAgICAgY29udGVudCA9IHRoaXMuc2FuaXRpemVIdG1sKGNvbnRlbnQpXG4gICAgICAgIH1cbiAgICAgIH1cblxuICAgICAgJHRpcC5maW5kKCcucG9wb3Zlci10aXRsZScpLmh0bWwodGl0bGUpXG4gICAgICAkdGlwLmZpbmQoJy5wb3BvdmVyLWNvbnRlbnQnKS5jaGlsZHJlbigpLmRldGFjaCgpLmVuZCgpW1xuICAgICAgICB0eXBlQ29udGVudCA9PT0gJ3N0cmluZycgPyAnaHRtbCcgOiAnYXBwZW5kJ1xuICAgICAgXShjb250ZW50KVxuICAgIH0gZWxzZSB7XG4gICAgICAkdGlwLmZpbmQoJy5wb3BvdmVyLXRpdGxlJykudGV4dCh0aXRsZSlcbiAgICAgICR0aXAuZmluZCgnLnBvcG92ZXItY29udGVudCcpLmNoaWxkcmVuKCkuZGV0YWNoKCkuZW5kKCkudGV4dChjb250ZW50KVxuICAgIH1cblxuICAgICR0aXAucmVtb3ZlQ2xhc3MoJ2ZhZGUgdG9wIGJvdHRvbSBsZWZ0IHJpZ2h0IGluJylcblxuICAgIC8vIElFOCBkb2Vzbid0IGFjY2VwdCBoaWRpbmcgdmlhIHRoZSBgOmVtcHR5YCBwc2V1ZG8gc2VsZWN0b3IsIHdlIGhhdmUgdG8gZG9cbiAgICAvLyB0aGlzIG1hbnVhbGx5IGJ5IGNoZWNraW5nIHRoZSBjb250ZW50cy5cbiAgICBpZiAoISR0aXAuZmluZCgnLnBvcG92ZXItdGl0bGUnKS5odG1sKCkpICR0aXAuZmluZCgnLnBvcG92ZXItdGl0bGUnKS5oaWRlKClcbiAgfVxuXG4gIFBvcG92ZXIucHJvdG90eXBlLmhhc0NvbnRlbnQgPSBmdW5jdGlvbiAoKSB7XG4gICAgcmV0dXJuIHRoaXMuZ2V0VGl0bGUoKSB8fCB0aGlzLmdldENvbnRlbnQoKVxuICB9XG5cbiAgUG9wb3Zlci5wcm90b3R5cGUuZ2V0Q29udGVudCA9IGZ1bmN0aW9uICgpIHtcbiAgICB2YXIgJGUgPSB0aGlzLiRlbGVtZW50XG4gICAgdmFyIG8gID0gdGhpcy5vcHRpb25zXG5cbiAgICByZXR1cm4gJGUuYXR0cignZGF0YS1jb250ZW50JylcbiAgICAgIHx8ICh0eXBlb2Ygby5jb250ZW50ID09ICdmdW5jdGlvbicgP1xuICAgICAgICBvLmNvbnRlbnQuY2FsbCgkZVswXSkgOlxuICAgICAgICBvLmNvbnRlbnQpXG4gIH1cblxuICBQb3BvdmVyLnByb3RvdHlwZS5hcnJvdyA9IGZ1bmN0aW9uICgpIHtcbiAgICByZXR1cm4gKHRoaXMuJGFycm93ID0gdGhpcy4kYXJyb3cgfHwgdGhpcy50aXAoKS5maW5kKCcuYXJyb3cnKSlcbiAgfVxuXG5cbiAgLy8gUE9QT1ZFUiBQTFVHSU4gREVGSU5JVElPTlxuICAvLyA9PT09PT09PT09PT09PT09PT09PT09PT09XG5cbiAgZnVuY3Rpb24gUGx1Z2luKG9wdGlvbikge1xuICAgIHJldHVybiB0aGlzLmVhY2goZnVuY3Rpb24gKCkge1xuICAgICAgdmFyICR0aGlzICAgPSAkKHRoaXMpXG4gICAgICB2YXIgZGF0YSAgICA9ICR0aGlzLmRhdGEoJ2JzLnBvcG92ZXInKVxuICAgICAgdmFyIG9wdGlvbnMgPSB0eXBlb2Ygb3B0aW9uID09ICdvYmplY3QnICYmIG9wdGlvblxuXG4gICAgICBpZiAoIWRhdGEgJiYgL2Rlc3Ryb3l8aGlkZS8udGVzdChvcHRpb24pKSByZXR1cm5cbiAgICAgIGlmICghZGF0YSkgJHRoaXMuZGF0YSgnYnMucG9wb3ZlcicsIChkYXRhID0gbmV3IFBvcG92ZXIodGhpcywgb3B0aW9ucykpKVxuICAgICAgaWYgKHR5cGVvZiBvcHRpb24gPT0gJ3N0cmluZycpIGRhdGFbb3B0aW9uXSgpXG4gICAgfSlcbiAgfVxuXG4gIHZhciBvbGQgPSAkLmZuLnBvcG92ZXJcblxuICAkLmZuLnBvcG92ZXIgICAgICAgICAgICAgPSBQbHVnaW5cbiAgJC5mbi5wb3BvdmVyLkNvbnN0cnVjdG9yID0gUG9wb3ZlclxuXG5cbiAgLy8gUE9QT1ZFUiBOTyBDT05GTElDVFxuICAvLyA9PT09PT09PT09PT09PT09PT09XG5cbiAgJC5mbi5wb3BvdmVyLm5vQ29uZmxpY3QgPSBmdW5jdGlvbiAoKSB7XG4gICAgJC5mbi5wb3BvdmVyID0gb2xkXG4gICAgcmV0dXJuIHRoaXNcbiAgfVxuXG59KGpRdWVyeSk7XG5cbi8qID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PVxuICogQm9vdHN0cmFwOiBzY3JvbGxzcHkuanMgdjMuNC4xXG4gKiBodHRwczovL2dldGJvb3RzdHJhcC5jb20vZG9jcy8zLjQvamF2YXNjcmlwdC8jc2Nyb2xsc3B5XG4gKiA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT1cbiAqIENvcHlyaWdodCAyMDExLTIwMTkgVHdpdHRlciwgSW5jLlxuICogTGljZW5zZWQgdW5kZXIgTUlUIChodHRwczovL2dpdGh1Yi5jb20vdHdicy9ib290c3RyYXAvYmxvYi9tYXN0ZXIvTElDRU5TRSlcbiAqID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PSAqL1xuXG5cbitmdW5jdGlvbiAoJCkge1xuICAndXNlIHN0cmljdCc7XG5cbiAgLy8gU0NST0xMU1BZIENMQVNTIERFRklOSVRJT05cbiAgLy8gPT09PT09PT09PT09PT09PT09PT09PT09PT1cblxuICBmdW5jdGlvbiBTY3JvbGxTcHkoZWxlbWVudCwgb3B0aW9ucykge1xuICAgIHRoaXMuJGJvZHkgICAgICAgICAgPSAkKGRvY3VtZW50LmJvZHkpXG4gICAgdGhpcy4kc2Nyb2xsRWxlbWVudCA9ICQoZWxlbWVudCkuaXMoZG9jdW1lbnQuYm9keSkgPyAkKHdpbmRvdykgOiAkKGVsZW1lbnQpXG4gICAgdGhpcy5vcHRpb25zICAgICAgICA9ICQuZXh0ZW5kKHt9LCBTY3JvbGxTcHkuREVGQVVMVFMsIG9wdGlvbnMpXG4gICAgdGhpcy5zZWxlY3RvciAgICAgICA9ICh0aGlzLm9wdGlvbnMudGFyZ2V0IHx8ICcnKSArICcgLm5hdiBsaSA+IGEnXG4gICAgdGhpcy5vZmZzZXRzICAgICAgICA9IFtdXG4gICAgdGhpcy50YXJnZXRzICAgICAgICA9IFtdXG4gICAgdGhpcy5hY3RpdmVUYXJnZXQgICA9IG51bGxcbiAgICB0aGlzLnNjcm9sbEhlaWdodCAgID0gMFxuXG4gICAgdGhpcy4kc2Nyb2xsRWxlbWVudC5vbignc2Nyb2xsLmJzLnNjcm9sbHNweScsICQucHJveHkodGhpcy5wcm9jZXNzLCB0aGlzKSlcbiAgICB0aGlzLnJlZnJlc2goKVxuICAgIHRoaXMucHJvY2VzcygpXG4gIH1cblxuICBTY3JvbGxTcHkuVkVSU0lPTiAgPSAnMy40LjEnXG5cbiAgU2Nyb2xsU3B5LkRFRkFVTFRTID0ge1xuICAgIG9mZnNldDogMTBcbiAgfVxuXG4gIFNjcm9sbFNweS5wcm90b3R5cGUuZ2V0U2Nyb2xsSGVpZ2h0ID0gZnVuY3Rpb24gKCkge1xuICAgIHJldHVybiB0aGlzLiRzY3JvbGxFbGVtZW50WzBdLnNjcm9sbEhlaWdodCB8fCBNYXRoLm1heCh0aGlzLiRib2R5WzBdLnNjcm9sbEhlaWdodCwgZG9jdW1lbnQuZG9jdW1lbnRFbGVtZW50LnNjcm9sbEhlaWdodClcbiAgfVxuXG4gIFNjcm9sbFNweS5wcm90b3R5cGUucmVmcmVzaCA9IGZ1bmN0aW9uICgpIHtcbiAgICB2YXIgdGhhdCAgICAgICAgICA9IHRoaXNcbiAgICB2YXIgb2Zmc2V0TWV0aG9kICA9ICdvZmZzZXQnXG4gICAgdmFyIG9mZnNldEJhc2UgICAgPSAwXG5cbiAgICB0aGlzLm9mZnNldHMgICAgICA9IFtdXG4gICAgdGhpcy50YXJnZXRzICAgICAgPSBbXVxuICAgIHRoaXMuc2Nyb2xsSGVpZ2h0ID0gdGhpcy5nZXRTY3JvbGxIZWlnaHQoKVxuXG4gICAgaWYgKCEkLmlzV2luZG93KHRoaXMuJHNjcm9sbEVsZW1lbnRbMF0pKSB7XG4gICAgICBvZmZzZXRNZXRob2QgPSAncG9zaXRpb24nXG4gICAgICBvZmZzZXRCYXNlICAgPSB0aGlzLiRzY3JvbGxFbGVtZW50LnNjcm9sbFRvcCgpXG4gICAgfVxuXG4gICAgdGhpcy4kYm9keVxuICAgICAgLmZpbmQodGhpcy5zZWxlY3RvcilcbiAgICAgIC5tYXAoZnVuY3Rpb24gKCkge1xuICAgICAgICB2YXIgJGVsICAgPSAkKHRoaXMpXG4gICAgICAgIHZhciBocmVmICA9ICRlbC5kYXRhKCd0YXJnZXQnKSB8fCAkZWwuYXR0cignaHJlZicpXG4gICAgICAgIHZhciAkaHJlZiA9IC9eIy4vLnRlc3QoaHJlZikgJiYgJChocmVmKVxuXG4gICAgICAgIHJldHVybiAoJGhyZWZcbiAgICAgICAgICAmJiAkaHJlZi5sZW5ndGhcbiAgICAgICAgICAmJiAkaHJlZi5pcygnOnZpc2libGUnKVxuICAgICAgICAgICYmIFtbJGhyZWZbb2Zmc2V0TWV0aG9kXSgpLnRvcCArIG9mZnNldEJhc2UsIGhyZWZdXSkgfHwgbnVsbFxuICAgICAgfSlcbiAgICAgIC5zb3J0KGZ1bmN0aW9uIChhLCBiKSB7IHJldHVybiBhWzBdIC0gYlswXSB9KVxuICAgICAgLmVhY2goZnVuY3Rpb24gKCkge1xuICAgICAgICB0aGF0Lm9mZnNldHMucHVzaCh0aGlzWzBdKVxuICAgICAgICB0aGF0LnRhcmdldHMucHVzaCh0aGlzWzFdKVxuICAgICAgfSlcbiAgfVxuXG4gIFNjcm9sbFNweS5wcm90b3R5cGUucHJvY2VzcyA9IGZ1bmN0aW9uICgpIHtcbiAgICB2YXIgc2Nyb2xsVG9wICAgID0gdGhpcy4kc2Nyb2xsRWxlbWVudC5zY3JvbGxUb3AoKSArIHRoaXMub3B0aW9ucy5vZmZzZXRcbiAgICB2YXIgc2Nyb2xsSGVpZ2h0ID0gdGhpcy5nZXRTY3JvbGxIZWlnaHQoKVxuICAgIHZhciBtYXhTY3JvbGwgICAgPSB0aGlzLm9wdGlvbnMub2Zmc2V0ICsgc2Nyb2xsSGVpZ2h0IC0gdGhpcy4kc2Nyb2xsRWxlbWVudC5oZWlnaHQoKVxuICAgIHZhciBvZmZzZXRzICAgICAgPSB0aGlzLm9mZnNldHNcbiAgICB2YXIgdGFyZ2V0cyAgICAgID0gdGhpcy50YXJnZXRzXG4gICAgdmFyIGFjdGl2ZVRhcmdldCA9IHRoaXMuYWN0aXZlVGFyZ2V0XG4gICAgdmFyIGlcblxuICAgIGlmICh0aGlzLnNjcm9sbEhlaWdodCAhPSBzY3JvbGxIZWlnaHQpIHtcbiAgICAgIHRoaXMucmVmcmVzaCgpXG4gICAgfVxuXG4gICAgaWYgKHNjcm9sbFRvcCA+PSBtYXhTY3JvbGwpIHtcbiAgICAgIHJldHVybiBhY3RpdmVUYXJnZXQgIT0gKGkgPSB0YXJnZXRzW3RhcmdldHMubGVuZ3RoIC0gMV0pICYmIHRoaXMuYWN0aXZhdGUoaSlcbiAgICB9XG5cbiAgICBpZiAoYWN0aXZlVGFyZ2V0ICYmIHNjcm9sbFRvcCA8IG9mZnNldHNbMF0pIHtcbiAgICAgIHRoaXMuYWN0aXZlVGFyZ2V0ID0gbnVsbFxuICAgICAgcmV0dXJuIHRoaXMuY2xlYXIoKVxuICAgIH1cblxuICAgIGZvciAoaSA9IG9mZnNldHMubGVuZ3RoOyBpLS07KSB7XG4gICAgICBhY3RpdmVUYXJnZXQgIT0gdGFyZ2V0c1tpXVxuICAgICAgICAmJiBzY3JvbGxUb3AgPj0gb2Zmc2V0c1tpXVxuICAgICAgICAmJiAob2Zmc2V0c1tpICsgMV0gPT09IHVuZGVmaW5lZCB8fCBzY3JvbGxUb3AgPCBvZmZzZXRzW2kgKyAxXSlcbiAgICAgICAgJiYgdGhpcy5hY3RpdmF0ZSh0YXJnZXRzW2ldKVxuICAgIH1cbiAgfVxuXG4gIFNjcm9sbFNweS5wcm90b3R5cGUuYWN0aXZhdGUgPSBmdW5jdGlvbiAodGFyZ2V0KSB7XG4gICAgdGhpcy5hY3RpdmVUYXJnZXQgPSB0YXJnZXRcblxuICAgIHRoaXMuY2xlYXIoKVxuXG4gICAgdmFyIHNlbGVjdG9yID0gdGhpcy5zZWxlY3RvciArXG4gICAgICAnW2RhdGEtdGFyZ2V0PVwiJyArIHRhcmdldCArICdcIl0sJyArXG4gICAgICB0aGlzLnNlbGVjdG9yICsgJ1tocmVmPVwiJyArIHRhcmdldCArICdcIl0nXG5cbiAgICB2YXIgYWN0aXZlID0gJChzZWxlY3RvcilcbiAgICAgIC5wYXJlbnRzKCdsaScpXG4gICAgICAuYWRkQ2xhc3MoJ2FjdGl2ZScpXG5cbiAgICBpZiAoYWN0aXZlLnBhcmVudCgnLmRyb3Bkb3duLW1lbnUnKS5sZW5ndGgpIHtcbiAgICAgIGFjdGl2ZSA9IGFjdGl2ZVxuICAgICAgICAuY2xvc2VzdCgnbGkuZHJvcGRvd24nKVxuICAgICAgICAuYWRkQ2xhc3MoJ2FjdGl2ZScpXG4gICAgfVxuXG4gICAgYWN0aXZlLnRyaWdnZXIoJ2FjdGl2YXRlLmJzLnNjcm9sbHNweScpXG4gIH1cblxuICBTY3JvbGxTcHkucHJvdG90eXBlLmNsZWFyID0gZnVuY3Rpb24gKCkge1xuICAgICQodGhpcy5zZWxlY3RvcilcbiAgICAgIC5wYXJlbnRzVW50aWwodGhpcy5vcHRpb25zLnRhcmdldCwgJy5hY3RpdmUnKVxuICAgICAgLnJlbW92ZUNsYXNzKCdhY3RpdmUnKVxuICB9XG5cblxuICAvLyBTQ1JPTExTUFkgUExVR0lOIERFRklOSVRJT05cbiAgLy8gPT09PT09PT09PT09PT09PT09PT09PT09PT09XG5cbiAgZnVuY3Rpb24gUGx1Z2luKG9wdGlvbikge1xuICAgIHJldHVybiB0aGlzLmVhY2goZnVuY3Rpb24gKCkge1xuICAgICAgdmFyICR0aGlzICAgPSAkKHRoaXMpXG4gICAgICB2YXIgZGF0YSAgICA9ICR0aGlzLmRhdGEoJ2JzLnNjcm9sbHNweScpXG4gICAgICB2YXIgb3B0aW9ucyA9IHR5cGVvZiBvcHRpb24gPT0gJ29iamVjdCcgJiYgb3B0aW9uXG5cbiAgICAgIGlmICghZGF0YSkgJHRoaXMuZGF0YSgnYnMuc2Nyb2xsc3B5JywgKGRhdGEgPSBuZXcgU2Nyb2xsU3B5KHRoaXMsIG9wdGlvbnMpKSlcbiAgICAgIGlmICh0eXBlb2Ygb3B0aW9uID09ICdzdHJpbmcnKSBkYXRhW29wdGlvbl0oKVxuICAgIH0pXG4gIH1cblxuICB2YXIgb2xkID0gJC5mbi5zY3JvbGxzcHlcblxuICAkLmZuLnNjcm9sbHNweSAgICAgICAgICAgICA9IFBsdWdpblxuICAkLmZuLnNjcm9sbHNweS5Db25zdHJ1Y3RvciA9IFNjcm9sbFNweVxuXG5cbiAgLy8gU0NST0xMU1BZIE5PIENPTkZMSUNUXG4gIC8vID09PT09PT09PT09PT09PT09PT09PVxuXG4gICQuZm4uc2Nyb2xsc3B5Lm5vQ29uZmxpY3QgPSBmdW5jdGlvbiAoKSB7XG4gICAgJC5mbi5zY3JvbGxzcHkgPSBvbGRcbiAgICByZXR1cm4gdGhpc1xuICB9XG5cblxuICAvLyBTQ1JPTExTUFkgREFUQS1BUElcbiAgLy8gPT09PT09PT09PT09PT09PT09XG5cbiAgJCh3aW5kb3cpLm9uKCdsb2FkLmJzLnNjcm9sbHNweS5kYXRhLWFwaScsIGZ1bmN0aW9uICgpIHtcbiAgICAkKCdbZGF0YS1zcHk9XCJzY3JvbGxcIl0nKS5lYWNoKGZ1bmN0aW9uICgpIHtcbiAgICAgIHZhciAkc3B5ID0gJCh0aGlzKVxuICAgICAgUGx1Z2luLmNhbGwoJHNweSwgJHNweS5kYXRhKCkpXG4gICAgfSlcbiAgfSlcblxufShqUXVlcnkpO1xuXG4vKiA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT1cbiAqIEJvb3RzdHJhcDogdGFiLmpzIHYzLjQuMVxuICogaHR0cHM6Ly9nZXRib290c3RyYXAuY29tL2RvY3MvMy40L2phdmFzY3JpcHQvI3RhYnNcbiAqID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PVxuICogQ29weXJpZ2h0IDIwMTEtMjAxOSBUd2l0dGVyLCBJbmMuXG4gKiBMaWNlbnNlZCB1bmRlciBNSVQgKGh0dHBzOi8vZ2l0aHViLmNvbS90d2JzL2Jvb3RzdHJhcC9ibG9iL21hc3Rlci9MSUNFTlNFKVxuICogPT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09ICovXG5cblxuK2Z1bmN0aW9uICgkKSB7XG4gICd1c2Ugc3RyaWN0JztcblxuICAvLyBUQUIgQ0xBU1MgREVGSU5JVElPTlxuICAvLyA9PT09PT09PT09PT09PT09PT09PVxuXG4gIHZhciBUYWIgPSBmdW5jdGlvbiAoZWxlbWVudCkge1xuICAgIC8vIGpzY3M6ZGlzYWJsZSByZXF1aXJlRG9sbGFyQmVmb3JlalF1ZXJ5QXNzaWdubWVudFxuICAgIHRoaXMuZWxlbWVudCA9ICQoZWxlbWVudClcbiAgICAvLyBqc2NzOmVuYWJsZSByZXF1aXJlRG9sbGFyQmVmb3JlalF1ZXJ5QXNzaWdubWVudFxuICB9XG5cbiAgVGFiLlZFUlNJT04gPSAnMy40LjEnXG5cbiAgVGFiLlRSQU5TSVRJT05fRFVSQVRJT04gPSAxNTBcblxuICBUYWIucHJvdG90eXBlLnNob3cgPSBmdW5jdGlvbiAoKSB7XG4gICAgdmFyICR0aGlzICAgID0gdGhpcy5lbGVtZW50XG4gICAgdmFyICR1bCAgICAgID0gJHRoaXMuY2xvc2VzdCgndWw6bm90KC5kcm9wZG93bi1tZW51KScpXG4gICAgdmFyIHNlbGVjdG9yID0gJHRoaXMuZGF0YSgndGFyZ2V0JylcblxuICAgIGlmICghc2VsZWN0b3IpIHtcbiAgICAgIHNlbGVjdG9yID0gJHRoaXMuYXR0cignaHJlZicpXG4gICAgICBzZWxlY3RvciA9IHNlbGVjdG9yICYmIHNlbGVjdG9yLnJlcGxhY2UoLy4qKD89I1teXFxzXSokKS8sICcnKSAvLyBzdHJpcCBmb3IgaWU3XG4gICAgfVxuXG4gICAgaWYgKCR0aGlzLnBhcmVudCgnbGknKS5oYXNDbGFzcygnYWN0aXZlJykpIHJldHVyblxuXG4gICAgdmFyICRwcmV2aW91cyA9ICR1bC5maW5kKCcuYWN0aXZlOmxhc3QgYScpXG4gICAgdmFyIGhpZGVFdmVudCA9ICQuRXZlbnQoJ2hpZGUuYnMudGFiJywge1xuICAgICAgcmVsYXRlZFRhcmdldDogJHRoaXNbMF1cbiAgICB9KVxuICAgIHZhciBzaG93RXZlbnQgPSAkLkV2ZW50KCdzaG93LmJzLnRhYicsIHtcbiAgICAgIHJlbGF0ZWRUYXJnZXQ6ICRwcmV2aW91c1swXVxuICAgIH0pXG5cbiAgICAkcHJldmlvdXMudHJpZ2dlcihoaWRlRXZlbnQpXG4gICAgJHRoaXMudHJpZ2dlcihzaG93RXZlbnQpXG5cbiAgICBpZiAoc2hvd0V2ZW50LmlzRGVmYXVsdFByZXZlbnRlZCgpIHx8IGhpZGVFdmVudC5pc0RlZmF1bHRQcmV2ZW50ZWQoKSkgcmV0dXJuXG5cbiAgICB2YXIgJHRhcmdldCA9ICQoZG9jdW1lbnQpLmZpbmQoc2VsZWN0b3IpXG5cbiAgICB0aGlzLmFjdGl2YXRlKCR0aGlzLmNsb3Nlc3QoJ2xpJyksICR1bClcbiAgICB0aGlzLmFjdGl2YXRlKCR0YXJnZXQsICR0YXJnZXQucGFyZW50KCksIGZ1bmN0aW9uICgpIHtcbiAgICAgICRwcmV2aW91cy50cmlnZ2VyKHtcbiAgICAgICAgdHlwZTogJ2hpZGRlbi5icy50YWInLFxuICAgICAgICByZWxhdGVkVGFyZ2V0OiAkdGhpc1swXVxuICAgICAgfSlcbiAgICAgICR0aGlzLnRyaWdnZXIoe1xuICAgICAgICB0eXBlOiAnc2hvd24uYnMudGFiJyxcbiAgICAgICAgcmVsYXRlZFRhcmdldDogJHByZXZpb3VzWzBdXG4gICAgICB9KVxuICAgIH0pXG4gIH1cblxuICBUYWIucHJvdG90eXBlLmFjdGl2YXRlID0gZnVuY3Rpb24gKGVsZW1lbnQsIGNvbnRhaW5lciwgY2FsbGJhY2spIHtcbiAgICB2YXIgJGFjdGl2ZSAgICA9IGNvbnRhaW5lci5maW5kKCc+IC5hY3RpdmUnKVxuICAgIHZhciB0cmFuc2l0aW9uID0gY2FsbGJhY2tcbiAgICAgICYmICQuc3VwcG9ydC50cmFuc2l0aW9uXG4gICAgICAmJiAoJGFjdGl2ZS5sZW5ndGggJiYgJGFjdGl2ZS5oYXNDbGFzcygnZmFkZScpIHx8ICEhY29udGFpbmVyLmZpbmQoJz4gLmZhZGUnKS5sZW5ndGgpXG5cbiAgICBmdW5jdGlvbiBuZXh0KCkge1xuICAgICAgJGFjdGl2ZVxuICAgICAgICAucmVtb3ZlQ2xhc3MoJ2FjdGl2ZScpXG4gICAgICAgIC5maW5kKCc+IC5kcm9wZG93bi1tZW51ID4gLmFjdGl2ZScpXG4gICAgICAgIC5yZW1vdmVDbGFzcygnYWN0aXZlJylcbiAgICAgICAgLmVuZCgpXG4gICAgICAgIC5maW5kKCdbZGF0YS10b2dnbGU9XCJ0YWJcIl0nKVxuICAgICAgICAuYXR0cignYXJpYS1leHBhbmRlZCcsIGZhbHNlKVxuXG4gICAgICBlbGVtZW50XG4gICAgICAgIC5hZGRDbGFzcygnYWN0aXZlJylcbiAgICAgICAgLmZpbmQoJ1tkYXRhLXRvZ2dsZT1cInRhYlwiXScpXG4gICAgICAgIC5hdHRyKCdhcmlhLWV4cGFuZGVkJywgdHJ1ZSlcblxuICAgICAgaWYgKHRyYW5zaXRpb24pIHtcbiAgICAgICAgZWxlbWVudFswXS5vZmZzZXRXaWR0aCAvLyByZWZsb3cgZm9yIHRyYW5zaXRpb25cbiAgICAgICAgZWxlbWVudC5hZGRDbGFzcygnaW4nKVxuICAgICAgfSBlbHNlIHtcbiAgICAgICAgZWxlbWVudC5yZW1vdmVDbGFzcygnZmFkZScpXG4gICAgICB9XG5cbiAgICAgIGlmIChlbGVtZW50LnBhcmVudCgnLmRyb3Bkb3duLW1lbnUnKS5sZW5ndGgpIHtcbiAgICAgICAgZWxlbWVudFxuICAgICAgICAgIC5jbG9zZXN0KCdsaS5kcm9wZG93bicpXG4gICAgICAgICAgLmFkZENsYXNzKCdhY3RpdmUnKVxuICAgICAgICAgIC5lbmQoKVxuICAgICAgICAgIC5maW5kKCdbZGF0YS10b2dnbGU9XCJ0YWJcIl0nKVxuICAgICAgICAgIC5hdHRyKCdhcmlhLWV4cGFuZGVkJywgdHJ1ZSlcbiAgICAgIH1cblxuICAgICAgY2FsbGJhY2sgJiYgY2FsbGJhY2soKVxuICAgIH1cblxuICAgICRhY3RpdmUubGVuZ3RoICYmIHRyYW5zaXRpb24gP1xuICAgICAgJGFjdGl2ZVxuICAgICAgICAub25lKCdic1RyYW5zaXRpb25FbmQnLCBuZXh0KVxuICAgICAgICAuZW11bGF0ZVRyYW5zaXRpb25FbmQoVGFiLlRSQU5TSVRJT05fRFVSQVRJT04pIDpcbiAgICAgIG5leHQoKVxuXG4gICAgJGFjdGl2ZS5yZW1vdmVDbGFzcygnaW4nKVxuICB9XG5cblxuICAvLyBUQUIgUExVR0lOIERFRklOSVRJT05cbiAgLy8gPT09PT09PT09PT09PT09PT09PT09XG5cbiAgZnVuY3Rpb24gUGx1Z2luKG9wdGlvbikge1xuICAgIHJldHVybiB0aGlzLmVhY2goZnVuY3Rpb24gKCkge1xuICAgICAgdmFyICR0aGlzID0gJCh0aGlzKVxuICAgICAgdmFyIGRhdGEgID0gJHRoaXMuZGF0YSgnYnMudGFiJylcblxuICAgICAgaWYgKCFkYXRhKSAkdGhpcy5kYXRhKCdicy50YWInLCAoZGF0YSA9IG5ldyBUYWIodGhpcykpKVxuICAgICAgaWYgKHR5cGVvZiBvcHRpb24gPT0gJ3N0cmluZycpIGRhdGFbb3B0aW9uXSgpXG4gICAgfSlcbiAgfVxuXG4gIHZhciBvbGQgPSAkLmZuLnRhYlxuXG4gICQuZm4udGFiICAgICAgICAgICAgID0gUGx1Z2luXG4gICQuZm4udGFiLkNvbnN0cnVjdG9yID0gVGFiXG5cblxuICAvLyBUQUIgTk8gQ09ORkxJQ1RcbiAgLy8gPT09PT09PT09PT09PT09XG5cbiAgJC5mbi50YWIubm9Db25mbGljdCA9IGZ1bmN0aW9uICgpIHtcbiAgICAkLmZuLnRhYiA9IG9sZFxuICAgIHJldHVybiB0aGlzXG4gIH1cblxuXG4gIC8vIFRBQiBEQVRBLUFQSVxuICAvLyA9PT09PT09PT09PT1cblxuICB2YXIgY2xpY2tIYW5kbGVyID0gZnVuY3Rpb24gKGUpIHtcbiAgICBlLnByZXZlbnREZWZhdWx0KClcbiAgICBQbHVnaW4uY2FsbCgkKHRoaXMpLCAnc2hvdycpXG4gIH1cblxuICAkKGRvY3VtZW50KVxuICAgIC5vbignY2xpY2suYnMudGFiLmRhdGEtYXBpJywgJ1tkYXRhLXRvZ2dsZT1cInRhYlwiXScsIGNsaWNrSGFuZGxlcilcbiAgICAub24oJ2NsaWNrLmJzLnRhYi5kYXRhLWFwaScsICdbZGF0YS10b2dnbGU9XCJwaWxsXCJdJywgY2xpY2tIYW5kbGVyKVxuXG59KGpRdWVyeSk7XG5cbi8qID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PVxuICogQm9vdHN0cmFwOiBhZmZpeC5qcyB2My40LjFcbiAqIGh0dHBzOi8vZ2V0Ym9vdHN0cmFwLmNvbS9kb2NzLzMuNC9qYXZhc2NyaXB0LyNhZmZpeFxuICogPT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09XG4gKiBDb3B5cmlnaHQgMjAxMS0yMDE5IFR3aXR0ZXIsIEluYy5cbiAqIExpY2Vuc2VkIHVuZGVyIE1JVCAoaHR0cHM6Ly9naXRodWIuY29tL3R3YnMvYm9vdHN0cmFwL2Jsb2IvbWFzdGVyL0xJQ0VOU0UpXG4gKiA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0gKi9cblxuXG4rZnVuY3Rpb24gKCQpIHtcbiAgJ3VzZSBzdHJpY3QnO1xuXG4gIC8vIEFGRklYIENMQVNTIERFRklOSVRJT05cbiAgLy8gPT09PT09PT09PT09PT09PT09PT09PVxuXG4gIHZhciBBZmZpeCA9IGZ1bmN0aW9uIChlbGVtZW50LCBvcHRpb25zKSB7XG4gICAgdGhpcy5vcHRpb25zID0gJC5leHRlbmQoe30sIEFmZml4LkRFRkFVTFRTLCBvcHRpb25zKVxuXG4gICAgdmFyIHRhcmdldCA9IHRoaXMub3B0aW9ucy50YXJnZXQgPT09IEFmZml4LkRFRkFVTFRTLnRhcmdldCA/ICQodGhpcy5vcHRpb25zLnRhcmdldCkgOiAkKGRvY3VtZW50KS5maW5kKHRoaXMub3B0aW9ucy50YXJnZXQpXG5cbiAgICB0aGlzLiR0YXJnZXQgPSB0YXJnZXRcbiAgICAgIC5vbignc2Nyb2xsLmJzLmFmZml4LmRhdGEtYXBpJywgJC5wcm94eSh0aGlzLmNoZWNrUG9zaXRpb24sIHRoaXMpKVxuICAgICAgLm9uKCdjbGljay5icy5hZmZpeC5kYXRhLWFwaScsICAkLnByb3h5KHRoaXMuY2hlY2tQb3NpdGlvbldpdGhFdmVudExvb3AsIHRoaXMpKVxuXG4gICAgdGhpcy4kZWxlbWVudCAgICAgPSAkKGVsZW1lbnQpXG4gICAgdGhpcy5hZmZpeGVkICAgICAgPSBudWxsXG4gICAgdGhpcy51bnBpbiAgICAgICAgPSBudWxsXG4gICAgdGhpcy5waW5uZWRPZmZzZXQgPSBudWxsXG5cbiAgICB0aGlzLmNoZWNrUG9zaXRpb24oKVxuICB9XG5cbiAgQWZmaXguVkVSU0lPTiAgPSAnMy40LjEnXG5cbiAgQWZmaXguUkVTRVQgICAgPSAnYWZmaXggYWZmaXgtdG9wIGFmZml4LWJvdHRvbSdcblxuICBBZmZpeC5ERUZBVUxUUyA9IHtcbiAgICBvZmZzZXQ6IDAsXG4gICAgdGFyZ2V0OiB3aW5kb3dcbiAgfVxuXG4gIEFmZml4LnByb3RvdHlwZS5nZXRTdGF0ZSA9IGZ1bmN0aW9uIChzY3JvbGxIZWlnaHQsIGhlaWdodCwgb2Zmc2V0VG9wLCBvZmZzZXRCb3R0b20pIHtcbiAgICB2YXIgc2Nyb2xsVG9wICAgID0gdGhpcy4kdGFyZ2V0LnNjcm9sbFRvcCgpXG4gICAgdmFyIHBvc2l0aW9uICAgICA9IHRoaXMuJGVsZW1lbnQub2Zmc2V0KClcbiAgICB2YXIgdGFyZ2V0SGVpZ2h0ID0gdGhpcy4kdGFyZ2V0LmhlaWdodCgpXG5cbiAgICBpZiAob2Zmc2V0VG9wICE9IG51bGwgJiYgdGhpcy5hZmZpeGVkID09ICd0b3AnKSByZXR1cm4gc2Nyb2xsVG9wIDwgb2Zmc2V0VG9wID8gJ3RvcCcgOiBmYWxzZVxuXG4gICAgaWYgKHRoaXMuYWZmaXhlZCA9PSAnYm90dG9tJykge1xuICAgICAgaWYgKG9mZnNldFRvcCAhPSBudWxsKSByZXR1cm4gKHNjcm9sbFRvcCArIHRoaXMudW5waW4gPD0gcG9zaXRpb24udG9wKSA/IGZhbHNlIDogJ2JvdHRvbSdcbiAgICAgIHJldHVybiAoc2Nyb2xsVG9wICsgdGFyZ2V0SGVpZ2h0IDw9IHNjcm9sbEhlaWdodCAtIG9mZnNldEJvdHRvbSkgPyBmYWxzZSA6ICdib3R0b20nXG4gICAgfVxuXG4gICAgdmFyIGluaXRpYWxpemluZyAgID0gdGhpcy5hZmZpeGVkID09IG51bGxcbiAgICB2YXIgY29sbGlkZXJUb3AgICAgPSBpbml0aWFsaXppbmcgPyBzY3JvbGxUb3AgOiBwb3NpdGlvbi50b3BcbiAgICB2YXIgY29sbGlkZXJIZWlnaHQgPSBpbml0aWFsaXppbmcgPyB0YXJnZXRIZWlnaHQgOiBoZWlnaHRcblxuICAgIGlmIChvZmZzZXRUb3AgIT0gbnVsbCAmJiBzY3JvbGxUb3AgPD0gb2Zmc2V0VG9wKSByZXR1cm4gJ3RvcCdcbiAgICBpZiAob2Zmc2V0Qm90dG9tICE9IG51bGwgJiYgKGNvbGxpZGVyVG9wICsgY29sbGlkZXJIZWlnaHQgPj0gc2Nyb2xsSGVpZ2h0IC0gb2Zmc2V0Qm90dG9tKSkgcmV0dXJuICdib3R0b20nXG5cbiAgICByZXR1cm4gZmFsc2VcbiAgfVxuXG4gIEFmZml4LnByb3RvdHlwZS5nZXRQaW5uZWRPZmZzZXQgPSBmdW5jdGlvbiAoKSB7XG4gICAgaWYgKHRoaXMucGlubmVkT2Zmc2V0KSByZXR1cm4gdGhpcy5waW5uZWRPZmZzZXRcbiAgICB0aGlzLiRlbGVtZW50LnJlbW92ZUNsYXNzKEFmZml4LlJFU0VUKS5hZGRDbGFzcygnYWZmaXgnKVxuICAgIHZhciBzY3JvbGxUb3AgPSB0aGlzLiR0YXJnZXQuc2Nyb2xsVG9wKClcbiAgICB2YXIgcG9zaXRpb24gID0gdGhpcy4kZWxlbWVudC5vZmZzZXQoKVxuICAgIHJldHVybiAodGhpcy5waW5uZWRPZmZzZXQgPSBwb3NpdGlvbi50b3AgLSBzY3JvbGxUb3ApXG4gIH1cblxuICBBZmZpeC5wcm90b3R5cGUuY2hlY2tQb3NpdGlvbldpdGhFdmVudExvb3AgPSBmdW5jdGlvbiAoKSB7XG4gICAgc2V0VGltZW91dCgkLnByb3h5KHRoaXMuY2hlY2tQb3NpdGlvbiwgdGhpcyksIDEpXG4gIH1cblxuICBBZmZpeC5wcm90b3R5cGUuY2hlY2tQb3NpdGlvbiA9IGZ1bmN0aW9uICgpIHtcbiAgICBpZiAoIXRoaXMuJGVsZW1lbnQuaXMoJzp2aXNpYmxlJykpIHJldHVyblxuXG4gICAgdmFyIGhlaWdodCAgICAgICA9IHRoaXMuJGVsZW1lbnQuaGVpZ2h0KClcbiAgICB2YXIgb2Zmc2V0ICAgICAgID0gdGhpcy5vcHRpb25zLm9mZnNldFxuICAgIHZhciBvZmZzZXRUb3AgICAgPSBvZmZzZXQudG9wXG4gICAgdmFyIG9mZnNldEJvdHRvbSA9IG9mZnNldC5ib3R0b21cbiAgICB2YXIgc2Nyb2xsSGVpZ2h0ID0gTWF0aC5tYXgoJChkb2N1bWVudCkuaGVpZ2h0KCksICQoZG9jdW1lbnQuYm9keSkuaGVpZ2h0KCkpXG5cbiAgICBpZiAodHlwZW9mIG9mZnNldCAhPSAnb2JqZWN0JykgICAgICAgICBvZmZzZXRCb3R0b20gPSBvZmZzZXRUb3AgPSBvZmZzZXRcbiAgICBpZiAodHlwZW9mIG9mZnNldFRvcCA9PSAnZnVuY3Rpb24nKSAgICBvZmZzZXRUb3AgICAgPSBvZmZzZXQudG9wKHRoaXMuJGVsZW1lbnQpXG4gICAgaWYgKHR5cGVvZiBvZmZzZXRCb3R0b20gPT0gJ2Z1bmN0aW9uJykgb2Zmc2V0Qm90dG9tID0gb2Zmc2V0LmJvdHRvbSh0aGlzLiRlbGVtZW50KVxuXG4gICAgdmFyIGFmZml4ID0gdGhpcy5nZXRTdGF0ZShzY3JvbGxIZWlnaHQsIGhlaWdodCwgb2Zmc2V0VG9wLCBvZmZzZXRCb3R0b20pXG5cbiAgICBpZiAodGhpcy5hZmZpeGVkICE9IGFmZml4KSB7XG4gICAgICBpZiAodGhpcy51bnBpbiAhPSBudWxsKSB0aGlzLiRlbGVtZW50LmNzcygndG9wJywgJycpXG5cbiAgICAgIHZhciBhZmZpeFR5cGUgPSAnYWZmaXgnICsgKGFmZml4ID8gJy0nICsgYWZmaXggOiAnJylcbiAgICAgIHZhciBlICAgICAgICAgPSAkLkV2ZW50KGFmZml4VHlwZSArICcuYnMuYWZmaXgnKVxuXG4gICAgICB0aGlzLiRlbGVtZW50LnRyaWdnZXIoZSlcblxuICAgICAgaWYgKGUuaXNEZWZhdWx0UHJldmVudGVkKCkpIHJldHVyblxuXG4gICAgICB0aGlzLmFmZml4ZWQgPSBhZmZpeFxuICAgICAgdGhpcy51bnBpbiA9IGFmZml4ID09ICdib3R0b20nID8gdGhpcy5nZXRQaW5uZWRPZmZzZXQoKSA6IG51bGxcblxuICAgICAgdGhpcy4kZWxlbWVudFxuICAgICAgICAucmVtb3ZlQ2xhc3MoQWZmaXguUkVTRVQpXG4gICAgICAgIC5hZGRDbGFzcyhhZmZpeFR5cGUpXG4gICAgICAgIC50cmlnZ2VyKGFmZml4VHlwZS5yZXBsYWNlKCdhZmZpeCcsICdhZmZpeGVkJykgKyAnLmJzLmFmZml4JylcbiAgICB9XG5cbiAgICBpZiAoYWZmaXggPT0gJ2JvdHRvbScpIHtcbiAgICAgIHRoaXMuJGVsZW1lbnQub2Zmc2V0KHtcbiAgICAgICAgdG9wOiBzY3JvbGxIZWlnaHQgLSBoZWlnaHQgLSBvZmZzZXRCb3R0b21cbiAgICAgIH0pXG4gICAgfVxuICB9XG5cblxuICAvLyBBRkZJWCBQTFVHSU4gREVGSU5JVElPTlxuICAvLyA9PT09PT09PT09PT09PT09PT09PT09PVxuXG4gIGZ1bmN0aW9uIFBsdWdpbihvcHRpb24pIHtcbiAgICByZXR1cm4gdGhpcy5lYWNoKGZ1bmN0aW9uICgpIHtcbiAgICAgIHZhciAkdGhpcyAgID0gJCh0aGlzKVxuICAgICAgdmFyIGRhdGEgICAgPSAkdGhpcy5kYXRhKCdicy5hZmZpeCcpXG4gICAgICB2YXIgb3B0aW9ucyA9IHR5cGVvZiBvcHRpb24gPT0gJ29iamVjdCcgJiYgb3B0aW9uXG5cbiAgICAgIGlmICghZGF0YSkgJHRoaXMuZGF0YSgnYnMuYWZmaXgnLCAoZGF0YSA9IG5ldyBBZmZpeCh0aGlzLCBvcHRpb25zKSkpXG4gICAgICBpZiAodHlwZW9mIG9wdGlvbiA9PSAnc3RyaW5nJykgZGF0YVtvcHRpb25dKClcbiAgICB9KVxuICB9XG5cbiAgdmFyIG9sZCA9ICQuZm4uYWZmaXhcblxuICAkLmZuLmFmZml4ICAgICAgICAgICAgID0gUGx1Z2luXG4gICQuZm4uYWZmaXguQ29uc3RydWN0b3IgPSBBZmZpeFxuXG5cbiAgLy8gQUZGSVggTk8gQ09ORkxJQ1RcbiAgLy8gPT09PT09PT09PT09PT09PT1cblxuICAkLmZuLmFmZml4Lm5vQ29uZmxpY3QgPSBmdW5jdGlvbiAoKSB7XG4gICAgJC5mbi5hZmZpeCA9IG9sZFxuICAgIHJldHVybiB0aGlzXG4gIH1cblxuXG4gIC8vIEFGRklYIERBVEEtQVBJXG4gIC8vID09PT09PT09PT09PT09XG5cbiAgJCh3aW5kb3cpLm9uKCdsb2FkJywgZnVuY3Rpb24gKCkge1xuICAgICQoJ1tkYXRhLXNweT1cImFmZml4XCJdJykuZWFjaChmdW5jdGlvbiAoKSB7XG4gICAgICB2YXIgJHNweSA9ICQodGhpcylcbiAgICAgIHZhciBkYXRhID0gJHNweS5kYXRhKClcblxuICAgICAgZGF0YS5vZmZzZXQgPSBkYXRhLm9mZnNldCB8fCB7fVxuXG4gICAgICBpZiAoZGF0YS5vZmZzZXRCb3R0b20gIT0gbnVsbCkgZGF0YS5vZmZzZXQuYm90dG9tID0gZGF0YS5vZmZzZXRCb3R0b21cbiAgICAgIGlmIChkYXRhLm9mZnNldFRvcCAgICAhPSBudWxsKSBkYXRhLm9mZnNldC50b3AgICAgPSBkYXRhLm9mZnNldFRvcFxuXG4gICAgICBQbHVnaW4uY2FsbCgkc3B5LCBkYXRhKVxuICAgIH0pXG4gIH0pXG5cbn0oalF1ZXJ5KTtcbiIsImV4cG9ydHMgPSBtb2R1bGUuZXhwb3J0cyA9IHJlcXVpcmUoXCIuLi8uLi8uLi9ub2RlX21vZHVsZXMvY3NzLWxvYWRlci9saWIvY3NzLWJhc2UuanNcIikoZmFsc2UpO1xuLy8gaW1wb3J0c1xuXG5cbi8vIG1vZHVsZVxuZXhwb3J0cy5wdXNoKFttb2R1bGUuaWQsIFwiLmZvcm0taG9yaXpvbnRhbCAuZm9ybS1ncm91cCB7XFxuICAgIG1hcmdpbi1yaWdodDogYXV0bztcXG4gICAgbWFyZ2luLWxlZnQ6IGF1dG87XFxufVxcbi5wYi1wYWxldHRle1xcbiAgICB3aWR0aDoyOTVweDtcXG4gICAgZmxvYXQ6bGVmdDtcXG4gICAgbWluLWhlaWdodDogMzAwcHg7XFxuICAgIGJvcmRlcjpzb2xpZCAxcHggI2RkZGRkZDtcXG4gICAgYmFja2dyb3VuZDogI2ZmZmZmZjtcXG4gICAgbWFyZ2luLWxlZnQ6MTBweDtcXG4gICAgcG9zaXRpb246IGFic29sdXRlO1xcbiAgICBwYWRkaW5nLWJvdHRvbTogMjBweDtcXG59XFxuLnBiLWhhc0ZvY3Vze1xcbiAgICBib3JkZXI6MXB4IHNvbGlkICM5QkJERDggIWltcG9ydGFudDtcXG59XFxuLnBiLWNvbXBvbmVudHtcXG4gICAgYmFja2dyb3VuZDogdHJhbnNwYXJlbnQ7XFxuICAgIGZvbnQtc2l6ZTogMTJweDtcXG4gICAgcGFkZGluZzogNXB4O1xcbiAgICBjdXJzb3I6IG1vdmU7XFxuICAgIGJvcmRlcjogMXB4IHNvbGlkIHRyYW5zcGFyZW50O1xcbiAgICBib3JkZXItcmFkaXVzOiAyLjVweCAyLjVweCAyLjVweCAyLjVweDtcXG4gICAgY29sb3I6ICM1MjVDNjY7XFxuICAgIHRyYW5zaXRpb24tZHVyYXRpb246IDE1MG1zO1xcbiAgICB0cmFuc2l0aW9uLXByb3BlcnR5OiBiYWNrZ3JvdW5kLWNvbG9yLCBib3JkZXItY29sb3IsIGJveC1zaGFkb3c7XFxuICAgIHdoaXRlLXNwYWNlOiBub3JtYWw7XFxuICAgIG1pbi13aWR0aDogMTAwcHg7XFxufVxcbi5wYi1jb21wb25lbnQ6aG92ZXJ7XFxuICAgIGJvcmRlcjogMXB4IHNvbGlkICNkZGQgIWltcG9ydGFudDtcXG4gICAgYmFja2dyb3VuZC1jb2xvcjogcmdiYSgzLCAxNCwgMjcsIDAuMDMpO1xcbn1cXG4ucGItZWxlbWVudHtcXG4gICAgYm9yZGVyOiAxcHggc29saWQgdHJhbnNwYXJlbnQ7XFxuICAgIGJhY2tncm91bmQ6IHRyYW5zcGFyZW50O1xcbn1cXG4ucGItZWxlbWVudC1ob3ZlcntcXG4gICAgYm9yZGVyOiAxcHggc29saWQgIzlCQkREOCAhaW1wb3J0YW50O1xcbn1cXG4ucGItc2hhZG93e1xcbiAgICBib3JkZXI6ICNkZGQgc29saWQgMXB4O1xcbiAgICBtYXJnaW46IDIwcHg7XFxuICAgIGJhY2tncm91bmQtY29sb3I6ICNmZmZmZmY7XFxuICAgIHBhZGRpbmctbGVmdDoyMHB4O1xcbiAgICBwYWRkaW5nLXJpZ2h0OjIwcHg7XFxufVxcbi5wYi1kcm9wYWJsZS1ncmlke1xcbiAgICBwYWRkaW5nOiA0cHg7XFxuICAgIG1pbi1oZWlnaHQ6IDgwcHg7XFxuICAgIGhlaWdodDogYXV0byAhaW1wb3J0YW50O1xcbiAgICBiYWNrZ3JvdW5kLWNvbG9yOiAjZmZmO1xcbiAgICBib3JkZXI6IDFweCBkb3R0ZWQgI2RkZGRkZDtcXG59XFxuLnBiLXRhYi1ncmlke1xcbiAgICBwYWRkaW5nOiA0cHg7XFxuICAgIG1pbi1oZWlnaHQ6IDgwcHg7XFxuICAgIGhlaWdodDogYXV0byAhaW1wb3J0YW50O1xcbiAgICBiYWNrZ3JvdW5kLWNvbG9yOiAjZmZmO1xcbn1cXG4ucGItY2Fyb3VzZWwtY29udGFpbmVye1xcbiAgICBtaW4taGVpZ2h0OiAyMDBweDtcXG59XFxuLnBiLXNvcnRhYmxlLXBsYWNlaG9sZGVyIHtcXG4gICAgZGlzcGxheTogYmxvY2s7XFxuICAgIGJvcmRlcjogMXB4IHNvbGlkICNkZGQ7XFxuICAgIG1pbi1oZWlnaHQ6IDYwcHg7XFxuICAgIGJhY2tncm91bmQ6ICNmZGZkZmQ7XFxuICAgIGhlaWdodDogNjBweDtcXG4gICAgd2lkdGg6IDEwMCU7XFxufVxcbi5wYi1jYW52YXMtY29udGFpbmVye1xcbiAgICBtaW4taGVpZ2h0OiAxMDBweDtcXG4gICAgaGVpZ2h0OiBhdXRvICFpbXBvcnRhbnQ7XFxuICAgIGJhY2tncm91bmQtY29sb3I6ICNmZmY7XFxuICAgIGJhY2tncm91bmQ6ICNmZmY7XFxuICAgIGJvcmRlcjogMXB4IHNvbGlkICNmZmY7XFxuICAgIHBhZGRpbmc6IDJweDtcXG59XFxuLnBiLXRhYi1pY29uIHtcXG4gICAgcG9zaXRpb246IHJlbGF0aXZlO1xcbiAgICB0b3A6IDFweDtcXG4gICAgZGlzcGxheTogaW5saW5lLWJsb2NrO1xcbiAgICBmb250LWZhbWlseTogJ0dseXBoaWNvbnMgSGFsZmxpbmdzJztcXG4gICAgZm9udC1zdHlsZTogbm9ybWFsO1xcbiAgICBmb250LXdlaWdodDogbm9ybWFsO1xcbiAgICBsaW5lLWhlaWdodDogMTtcXG4gICAgLXdlYmtpdC1mb250LXNtb290aGluZzogYW50aWFsaWFzZWQ7XFxufVxcbi5wYi10YWItdG9vbGJhciB7XFxuICAgICBmbG9hdDpyaWdodDtcXG4gICAgIG1hcmdpbi1yaWdodDogM3B4O1xcbiAgICAgdG9wOiA1cHg7XFxuICAgICByaWdodDogNXB4O1xcbiAgICAgbWFyZ2luLXRvcDogMHB4O1xcbiAgICAgY3Vyc29yOiBwb2ludGVyO1xcbiAgICAgY29sb3I6IzAwN2ZmZjtcXG4gfVxcbi5wYi1pY29uLWFkZCB7XFxuICAgIGN1cnNvcjogcG9pbnRlcjtcXG4gICAgY29sb3I6ICMwMDdmZmY7XFxufVxcbi5wYi1pY29uLWRlbGV0ZSB7XFxuICAgIGN1cnNvcjogcG9pbnRlcjtcXG4gICAgY29sb3I6IHJlZDtcXG59XFxuLnBiLXRvb2xiYXJ7XFxuICAgIGJhY2tncm91bmQtY29sb3I6ICNmZmZmZmY7XFxuICAgIG1hcmdpbi1sZWZ0OiAxMHB4O1xcbiAgICBtYXJnaW4tcmlnaHQ6IDMwcHg7XFxuICAgIG1hcmdpbi10b3A6IDVweDtcXG59XFxuLnBkLWRhdGFsYWJlbHtcXG4gICAgYm9yZGVyLWJvdHRvbTogc29saWQgMXB4ICNhZGFkYWQ7XFxuICAgIG1pbi13aWR0aDogMTIwcHg7XFxuICAgIG1pbi1oZWlnaHQ6IDI2cHg7XFxuICAgIGRpc3BsYXk6IGlubGluZS1ibG9jaztcXG4gICAgdGV4dC1hbGlnbjogY2VudGVyO1xcbn1cXG4uc2xpZGVyLWJhci1sZWZ0e1xcbiAgICB3aWR0aDogMzEwcHg7XFxuICAgIHRvcDogMDtcXG4gICAgYm90dG9tOiAwO1xcbiAgICAvKiBoZWlnaHQ6IGF1dG87ICovXFxuICAgIG1hcmdpbi1sZWZ0OiAwcHg7XFxuICAgIGJvcmRlci1jb2xvcjogI2Y1ZjVmNTtcXG4gICAgYm9yZGVyLXJpZ2h0OiAxcHggc29saWQgI2RkZCAhaW1wb3J0YW50O1xcbiAgICBiYWNrZ3JvdW5kLWNvbG9yOiAjZmZmZmZmO1xcbn1cIiwgXCJcIl0pO1xuXG4vLyBleHBvcnRzXG4iLCJ2YXIgZXNjYXBlID0gcmVxdWlyZShcIi4uLy4uLy4uL25vZGVfbW9kdWxlcy9jc3MtbG9hZGVyL2xpYi91cmwvZXNjYXBlLmpzXCIpO1xuZXhwb3J0cyA9IG1vZHVsZS5leHBvcnRzID0gcmVxdWlyZShcIi4uLy4uLy4uL25vZGVfbW9kdWxlcy9jc3MtbG9hZGVyL2xpYi9jc3MtYmFzZS5qc1wiKShmYWxzZSk7XG4vLyBpbXBvcnRzXG5cblxuLy8gbW9kdWxlXG5leHBvcnRzLnB1c2goW21vZHVsZS5pZCwgXCJcXG5AZm9udC1mYWNlIHtmb250LWZhbWlseTogXFxcImZvcm1cXFwiO1xcbiAgc3JjOiB1cmwoXCIgKyBlc2NhcGUocmVxdWlyZShcIi4vaWNvbmZvbnQuZW90XCIpKSArIFwiKTsgLyogSUU5Ki9cXG4gIHNyYzogdXJsKFwiICsgZXNjYXBlKHJlcXVpcmUoXCIuL2ljb25mb250LnR0ZlwiKSkgKyBcIikgZm9ybWF0KCd0cnVldHlwZScpO1xcbn1cXG5cXG4uZm9ybSB7XFxuICBmb250LWZhbWlseTpcXFwiZm9ybVxcXCIgIWltcG9ydGFudDtcXG4gIGZvbnQtc2l6ZToxM3B4O1xcbiAgZm9udC1zdHlsZTpub3JtYWw7XFxuICAtd2Via2l0LWZvbnQtc21vb3RoaW5nOiBhbnRpYWxpYXNlZDtcXG4gIC1tb3otb3N4LWZvbnQtc21vb3RoaW5nOiBncmF5c2NhbGU7XFxufVxcblxcbi5mb3JtLTNjb2w6YmVmb3JlIHsgY29udGVudDogXFxcIlxcXFxFNkU3XFxcIjsgfVxcblxcbi5mb3JtLWN1c3RvbS1jb2w6YmVmb3JlIHsgY29udGVudDogXFxcIlxcXFxFNjE0XFxcIjsgfVxcblxcbi5mb3JtLWRyb3Bkb3duOmJlZm9yZSB7IGNvbnRlbnQ6IFxcXCJcXFxcRTYwNlxcXCI7IH1cXG5cXG4uZm9ybS1jaGVja2JveDpiZWZvcmUgeyBjb250ZW50OiBcXFwiXFxcXEU2MERcXFwiOyB9XFxuXFxuLmZvcm0tZGF0ZXRpbWU6YmVmb3JlIHsgY29udGVudDogXFxcIlxcXFxFNkNDXFxcIjsgfVxcblxcbi5mb3JtLXJhZGlvOmJlZm9yZSB7IGNvbnRlbnQ6IFxcXCJcXFxcRTYxMlxcXCI7IH1cXG5cXG4uZm9ybS10YWI6YmVmb3JlIHsgY29udGVudDogXFxcIlxcXFxFNjFGXFxcIjsgfVxcblxcbi5mb3JtLWRhbnllLTpiZWZvcmUgeyBjb250ZW50OiBcXFwiXFxcXEU2MDNcXFwiOyB9XFxuXFxuLmZvcm0tc3VibWl0OmJlZm9yZSB7IGNvbnRlbnQ6IFxcXCJcXFxcRTY3MFxcXCI7IH1cXG5cXG4uZm9ybS10ZXh0YXJlYTpiZWZvcmUgeyBjb250ZW50OiBcXFwiXFxcXEU2RUFcXFwiOyB9XFxuXFxuLmZvcm0tdGV4dGJveDpiZWZvcmUgeyBjb250ZW50OiBcXFwiXFxcXEU2RUJcXFwiOyB9XFxuXFxuLmZvcm0tMmNvbDpiZWZvcmUgeyBjb250ZW50OiBcXFwiXFxcXEU2NEJcXFwiOyB9XFxuXFxuLmZvcm0tNGNvbDpiZWZvcmUgeyBjb250ZW50OiBcXFwiXFxcXEU2MDJcXFwiOyB9XFxuXFxuLmZvcm0tcmVzZXQ6YmVmb3JlIHsgY29udGVudDogXFxcIlxcXFxFNkU4XFxcIjsgfVxcblxcbi5mb3JtLTFjb2w6YmVmb3JlIHsgY29udGVudDogXFxcIlxcXFxFNjQ5XFxcIjsgfVxcblxcblwiLCBcIlwiXSk7XG5cbi8vIGV4cG9ydHNcbiIsImV4cG9ydHMgPSBtb2R1bGUuZXhwb3J0cyA9IHJlcXVpcmUoXCIuLi8uLi8uLi9ub2RlX21vZHVsZXMvY3NzLWxvYWRlci9saWIvY3NzLWJhc2UuanNcIikoZmFsc2UpO1xuLy8gaW1wb3J0c1xuXG5cbi8vIG1vZHVsZVxuZXhwb3J0cy5wdXNoKFttb2R1bGUuaWQsIFwiLyohXFxuICogRGF0ZXRpbWVwaWNrZXIgZm9yIEJvb3RzdHJhcFxcbiAqXFxuICogQ29weXJpZ2h0IDIwMTIgU3RlZmFuIFBldHJlXFxuICogSW1wcm92ZW1lbnRzIGJ5IEFuZHJldyBSb3dsc1xcbiAqIExpY2Vuc2VkIHVuZGVyIHRoZSBBcGFjaGUgTGljZW5zZSB2Mi4wXFxuICogaHR0cDovL3d3dy5hcGFjaGUub3JnL2xpY2Vuc2VzL0xJQ0VOU0UtMi4wXFxuICpcXG4gKi9cXG4uZGF0ZXRpbWVwaWNrZXIge1xcblxcdHBhZGRpbmc6IDRweDtcXG5cXHRtYXJnaW4tdG9wOiAxcHg7XFxuXFx0LXdlYmtpdC1ib3JkZXItcmFkaXVzOiA0cHg7XFxuXFx0LW1vei1ib3JkZXItcmFkaXVzOiA0cHg7XFxuXFx0Ym9yZGVyLXJhZGl1czogNHB4O1xcblxcdGRpcmVjdGlvbjogbHRyO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXItaW5saW5lIHtcXG5cXHR3aWR0aDogMjIwcHg7XFxufVxcblxcbi5kYXRldGltZXBpY2tlci5kYXRldGltZXBpY2tlci1ydGwge1xcblxcdGRpcmVjdGlvbjogcnRsO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIuZGF0ZXRpbWVwaWNrZXItcnRsIHRhYmxlIHRyIHRkIHNwYW4ge1xcblxcdGZsb2F0OiByaWdodDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyLWRyb3Bkb3duLCAuZGF0ZXRpbWVwaWNrZXItZHJvcGRvd24tbGVmdCB7XFxuXFx0dG9wOiAwO1xcblxcdGxlZnQ6IDA7XFxufVxcblxcbltjbGFzcyo9XFxcIiBkYXRldGltZXBpY2tlci1kcm9wZG93blxcXCJdOmJlZm9yZSB7XFxuXFx0Y29udGVudDogJyc7XFxuXFx0ZGlzcGxheTogaW5saW5lLWJsb2NrO1xcblxcdGJvcmRlci1sZWZ0OiA3cHggc29saWQgdHJhbnNwYXJlbnQ7XFxuXFx0Ym9yZGVyLXJpZ2h0OiA3cHggc29saWQgdHJhbnNwYXJlbnQ7XFxuXFx0Ym9yZGVyLWJvdHRvbTogN3B4IHNvbGlkICNjY2NjY2M7XFxuXFx0Ym9yZGVyLWJvdHRvbS1jb2xvcjogcmdiYSgwLCAwLCAwLCAwLjIpO1xcblxcdHBvc2l0aW9uOiBhYnNvbHV0ZTtcXG59XFxuXFxuW2NsYXNzKj1cXFwiIGRhdGV0aW1lcGlja2VyLWRyb3Bkb3duXFxcIl06YWZ0ZXIge1xcblxcdGNvbnRlbnQ6ICcnO1xcblxcdGRpc3BsYXk6IGlubGluZS1ibG9jaztcXG5cXHRib3JkZXItbGVmdDogNnB4IHNvbGlkIHRyYW5zcGFyZW50O1xcblxcdGJvcmRlci1yaWdodDogNnB4IHNvbGlkIHRyYW5zcGFyZW50O1xcblxcdGJvcmRlci1ib3R0b206IDZweCBzb2xpZCAjZmZmZmZmO1xcblxcdHBvc2l0aW9uOiBhYnNvbHV0ZTtcXG59XFxuXFxuW2NsYXNzKj1cXFwiIGRhdGV0aW1lcGlja2VyLWRyb3Bkb3duLXRvcFxcXCJdOmJlZm9yZSB7XFxuXFx0Y29udGVudDogJyc7XFxuXFx0ZGlzcGxheTogaW5saW5lLWJsb2NrO1xcblxcdGJvcmRlci1sZWZ0OiA3cHggc29saWQgdHJhbnNwYXJlbnQ7XFxuXFx0Ym9yZGVyLXJpZ2h0OiA3cHggc29saWQgdHJhbnNwYXJlbnQ7XFxuXFx0Ym9yZGVyLXRvcDogN3B4IHNvbGlkICNjY2NjY2M7XFxuXFx0Ym9yZGVyLXRvcC1jb2xvcjogcmdiYSgwLCAwLCAwLCAwLjIpO1xcblxcdGJvcmRlci1ib3R0b206IDA7XFxufVxcblxcbltjbGFzcyo9XFxcIiBkYXRldGltZXBpY2tlci1kcm9wZG93bi10b3BcXFwiXTphZnRlciB7XFxuXFx0Y29udGVudDogJyc7XFxuXFx0ZGlzcGxheTogaW5saW5lLWJsb2NrO1xcblxcdGJvcmRlci1sZWZ0OiA2cHggc29saWQgdHJhbnNwYXJlbnQ7XFxuXFx0Ym9yZGVyLXJpZ2h0OiA2cHggc29saWQgdHJhbnNwYXJlbnQ7XFxuXFx0Ym9yZGVyLXRvcDogNnB4IHNvbGlkICNmZmZmZmY7XFxuXFx0Ym9yZGVyLWJvdHRvbTogMDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyLWRyb3Bkb3duLWJvdHRvbS1sZWZ0OmJlZm9yZSB7XFxuXFx0dG9wOiAtN3B4O1xcblxcdHJpZ2h0OiA2cHg7XFxufVxcblxcbi5kYXRldGltZXBpY2tlci1kcm9wZG93bi1ib3R0b20tbGVmdDphZnRlciB7XFxuXFx0dG9wOiAtNnB4O1xcblxcdHJpZ2h0OiA3cHg7XFxufVxcblxcbi5kYXRldGltZXBpY2tlci1kcm9wZG93bi1ib3R0b20tcmlnaHQ6YmVmb3JlIHtcXG5cXHR0b3A6IC03cHg7XFxuXFx0bGVmdDogNnB4O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXItZHJvcGRvd24tYm90dG9tLXJpZ2h0OmFmdGVyIHtcXG5cXHR0b3A6IC02cHg7XFxuXFx0bGVmdDogN3B4O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXItZHJvcGRvd24tdG9wLWxlZnQ6YmVmb3JlIHtcXG5cXHRib3R0b206IC03cHg7XFxuXFx0cmlnaHQ6IDZweDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyLWRyb3Bkb3duLXRvcC1sZWZ0OmFmdGVyIHtcXG5cXHRib3R0b206IC02cHg7XFxuXFx0cmlnaHQ6IDdweDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyLWRyb3Bkb3duLXRvcC1yaWdodDpiZWZvcmUge1xcblxcdGJvdHRvbTogLTdweDtcXG5cXHRsZWZ0OiA2cHg7XFxufVxcblxcbi5kYXRldGltZXBpY2tlci1kcm9wZG93bi10b3AtcmlnaHQ6YWZ0ZXIge1xcblxcdGJvdHRvbTogLTZweDtcXG5cXHRsZWZ0OiA3cHg7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciA+IGRpdiB7XFxuXFx0ZGlzcGxheTogbm9uZTtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyLm1pbnV0ZXMgZGl2LmRhdGV0aW1lcGlja2VyLW1pbnV0ZXMge1xcblxcdGRpc3BsYXk6IGJsb2NrO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIuaG91cnMgZGl2LmRhdGV0aW1lcGlja2VyLWhvdXJzIHtcXG5cXHRkaXNwbGF5OiBibG9jaztcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyLmRheXMgZGl2LmRhdGV0aW1lcGlja2VyLWRheXMge1xcblxcdGRpc3BsYXk6IGJsb2NrO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIubW9udGhzIGRpdi5kYXRldGltZXBpY2tlci1tb250aHMge1xcblxcdGRpc3BsYXk6IGJsb2NrO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIueWVhcnMgZGl2LmRhdGV0aW1lcGlja2VyLXllYXJzIHtcXG5cXHRkaXNwbGF5OiBibG9jaztcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHtcXG5cXHRtYXJnaW46IDA7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciAgdGQsXFxuLmRhdGV0aW1lcGlja2VyIHRoIHtcXG5cXHR0ZXh0LWFsaWduOiBjZW50ZXI7XFxuXFx0d2lkdGg6IDIwcHg7XFxuXFx0aGVpZ2h0OiAyMHB4O1xcblxcdC13ZWJraXQtYm9yZGVyLXJhZGl1czogNHB4O1xcblxcdC1tb3otYm9yZGVyLXJhZGl1czogNHB4O1xcblxcdGJvcmRlci1yYWRpdXM6IDRweDtcXG5cXHRib3JkZXI6IG5vbmU7XFxufVxcblxcbi50YWJsZS1zdHJpcGVkIC5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCxcXG4udGFibGUtc3RyaXBlZCAuZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGgge1xcblxcdGJhY2tncm91bmQtY29sb3I6IHRyYW5zcGFyZW50O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQubWludXRlOmhvdmVyIHtcXG5cXHRiYWNrZ3JvdW5kOiAjZWVlZWVlO1xcblxcdGN1cnNvcjogcG9pbnRlcjtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmhvdXI6aG92ZXIge1xcblxcdGJhY2tncm91bmQ6ICNlZWVlZWU7XFxuXFx0Y3Vyc29yOiBwb2ludGVyO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuZGF5OmhvdmVyIHtcXG5cXHRiYWNrZ3JvdW5kOiAjZWVlZWVlO1xcblxcdGN1cnNvcjogcG9pbnRlcjtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLm9sZCxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQubmV3IHtcXG5cXHRjb2xvcjogIzk5OTk5OTtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmRpc2FibGVkLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5kaXNhYmxlZDpob3ZlciB7XFxuXFx0YmFja2dyb3VuZDogbm9uZTtcXG5cXHRjb2xvcjogIzk5OTk5OTtcXG5cXHRjdXJzb3I6IGRlZmF1bHQ7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXk6aG92ZXIsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5LmRpc2FibGVkLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheS5kaXNhYmxlZDpob3ZlciB7XFxuXFx0YmFja2dyb3VuZC1jb2xvcjogI2ZkZTE5YTtcXG5cXHRiYWNrZ3JvdW5kLWltYWdlOiAtbW96LWxpbmVhci1ncmFkaWVudCh0b3AsICNmZGQ0OWEsICNmZGY1OWEpO1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IC1tcy1saW5lYXItZ3JhZGllbnQodG9wLCAjZmRkNDlhLCAjZmRmNTlhKTtcXG5cXHRiYWNrZ3JvdW5kLWltYWdlOiAtd2Via2l0LWdyYWRpZW50KGxpbmVhciwgMCAwLCAwIDEwMCUsIGZyb20oI2ZkZDQ5YSksIHRvKCNmZGY1OWEpKTtcXG5cXHRiYWNrZ3JvdW5kLWltYWdlOiAtd2Via2l0LWxpbmVhci1ncmFkaWVudCh0b3AsICNmZGQ0OWEsICNmZGY1OWEpO1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IC1vLWxpbmVhci1ncmFkaWVudCh0b3AsICNmZGQ0OWEsICNmZGY1OWEpO1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IGxpbmVhci1ncmFkaWVudCh0byBib3R0b20sICNmZGQ0OWEsICNmZGY1OWEpO1xcblxcdGJhY2tncm91bmQtcmVwZWF0OiByZXBlYXQteDtcXG5cXHRmaWx0ZXI6IHByb2dpZDpEWEltYWdlVHJhbnNmb3JtLk1pY3Jvc29mdC5ncmFkaWVudChzdGFydENvbG9yc3RyPScjZmRkNDlhJywgZW5kQ29sb3JzdHI9JyNmZGY1OWEnLCBHcmFkaWVudFR5cGU9MCk7XFxuXFx0Ym9yZGVyLWNvbG9yOiAjZmRmNTlhICNmZGY1OWEgI2ZiZWQ1MDtcXG5cXHRib3JkZXItY29sb3I6IHJnYmEoMCwgMCwgMCwgMC4xKSByZ2JhKDAsIDAsIDAsIDAuMSkgcmdiYSgwLCAwLCAwLCAwLjI1KTtcXG5cXHRmaWx0ZXI6IHByb2dpZDpEWEltYWdlVHJhbnNmb3JtLk1pY3Jvc29mdC5ncmFkaWVudChlbmFibGVkPWZhbHNlKTtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5OmhvdmVyLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheTpob3Zlcjpob3ZlcixcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXkuZGlzYWJsZWQ6aG92ZXIsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5LmRpc2FibGVkOmhvdmVyOmhvdmVyLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheTphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5OmhvdmVyOmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXkuZGlzYWJsZWQ6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheS5kaXNhYmxlZDpob3ZlcjphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5LmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXk6aG92ZXIuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheS5kaXNhYmxlZC5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5LmRpc2FibGVkOmhvdmVyLmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXkuZGlzYWJsZWQsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5OmhvdmVyLmRpc2FibGVkLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheS5kaXNhYmxlZC5kaXNhYmxlZCxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXkuZGlzYWJsZWQ6aG92ZXIuZGlzYWJsZWQsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5W2Rpc2FibGVkXSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXk6aG92ZXJbZGlzYWJsZWRdLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheS5kaXNhYmxlZFtkaXNhYmxlZF0sXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5LmRpc2FibGVkOmhvdmVyW2Rpc2FibGVkXSB7XFxuXFx0YmFja2dyb3VuZC1jb2xvcjogI2ZkZjU5YTtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5OmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXk6aG92ZXI6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheS5kaXNhYmxlZDphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5LmRpc2FibGVkOmhvdmVyOmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXkuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheTpob3Zlci5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5LmRpc2FibGVkLmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXkuZGlzYWJsZWQ6aG92ZXIuYWN0aXZlIHtcXG5cXHRiYWNrZ3JvdW5kLWNvbG9yOiAjZmJmMDY5O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmU6aG92ZXIsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5kaXNhYmxlZCxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuYWN0aXZlLmRpc2FibGVkOmhvdmVyIHtcXG5cXHRiYWNrZ3JvdW5kLWNvbG9yOiAjMDA2ZGNjO1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IC1tb3otbGluZWFyLWdyYWRpZW50KHRvcCwgIzAwODhjYywgIzAwNDRjYyk7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogLW1zLWxpbmVhci1ncmFkaWVudCh0b3AsICMwMDg4Y2MsICMwMDQ0Y2MpO1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IC13ZWJraXQtZ3JhZGllbnQobGluZWFyLCAwIDAsIDAgMTAwJSwgZnJvbSgjMDA4OGNjKSwgdG8oIzAwNDRjYykpO1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IC13ZWJraXQtbGluZWFyLWdyYWRpZW50KHRvcCwgIzAwODhjYywgIzAwNDRjYyk7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogLW8tbGluZWFyLWdyYWRpZW50KHRvcCwgIzAwODhjYywgIzAwNDRjYyk7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogbGluZWFyLWdyYWRpZW50KHRvIGJvdHRvbSwgIzAwODhjYywgIzAwNDRjYyk7XFxuXFx0YmFja2dyb3VuZC1yZXBlYXQ6IHJlcGVhdC14O1xcblxcdGZpbHRlcjogcHJvZ2lkOkRYSW1hZ2VUcmFuc2Zvcm0uTWljcm9zb2Z0LmdyYWRpZW50KHN0YXJ0Q29sb3JzdHI9JyMwMDg4Y2MnLCBlbmRDb2xvcnN0cj0nIzAwNDRjYycsIEdyYWRpZW50VHlwZT0wKTtcXG5cXHRib3JkZXItY29sb3I6ICMwMDQ0Y2MgIzAwNDRjYyAjMDAyYTgwO1xcblxcdGJvcmRlci1jb2xvcjogcmdiYSgwLCAwLCAwLCAwLjEpIHJnYmEoMCwgMCwgMCwgMC4xKSByZ2JhKDAsIDAsIDAsIDAuMjUpO1xcblxcdGZpbHRlcjogcHJvZ2lkOkRYSW1hZ2VUcmFuc2Zvcm0uTWljcm9zb2Z0LmdyYWRpZW50KGVuYWJsZWQ9ZmFsc2UpO1xcblxcdGNvbG9yOiAjZmZmZmZmO1xcblxcdHRleHQtc2hhZG93OiAwIC0xcHggMCByZ2JhKDAsIDAsIDAsIDAuMjUpO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuYWN0aXZlOmhvdmVyLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmU6aG92ZXI6aG92ZXIsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5kaXNhYmxlZDpob3ZlcixcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuYWN0aXZlLmRpc2FibGVkOmhvdmVyOmhvdmVyLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmU6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmU6aG92ZXI6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuZGlzYWJsZWQ6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuZGlzYWJsZWQ6aG92ZXI6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmU6aG92ZXIuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuZGlzYWJsZWQuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuZGlzYWJsZWQ6aG92ZXIuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuZGlzYWJsZWQsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZTpob3Zlci5kaXNhYmxlZCxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuYWN0aXZlLmRpc2FibGVkLmRpc2FibGVkLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuZGlzYWJsZWQ6aG92ZXIuZGlzYWJsZWQsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZVtkaXNhYmxlZF0sXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZTpob3ZlcltkaXNhYmxlZF0sXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5kaXNhYmxlZFtkaXNhYmxlZF0sXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5kaXNhYmxlZDpob3ZlcltkaXNhYmxlZF0ge1xcblxcdGJhY2tncm91bmQtY29sb3I6ICMwMDQ0Y2M7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmU6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmU6aG92ZXI6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuZGlzYWJsZWQ6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuZGlzYWJsZWQ6aG92ZXI6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmU6aG92ZXIuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuZGlzYWJsZWQuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuZGlzYWJsZWQ6aG92ZXIuYWN0aXZlIHtcXG5cXHRiYWNrZ3JvdW5kLWNvbG9yOiAjMDAzMzk5O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3BhbiB7XFxuXFx0ZGlzcGxheTogYmxvY2s7XFxuXFx0d2lkdGg6IDIzJTtcXG5cXHRoZWlnaHQ6IDU0cHg7XFxuXFx0bGluZS1oZWlnaHQ6IDU0cHg7XFxuXFx0ZmxvYXQ6IGxlZnQ7XFxuXFx0bWFyZ2luOiAxJTtcXG5cXHRjdXJzb3I6IHBvaW50ZXI7XFxuXFx0LXdlYmtpdC1ib3JkZXItcmFkaXVzOiA0cHg7XFxuXFx0LW1vei1ib3JkZXItcmFkaXVzOiA0cHg7XFxuXFx0Ym9yZGVyLXJhZGl1czogNHB4O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgLmRhdGV0aW1lcGlja2VyLWhvdXJzIHNwYW4ge1xcblxcdGhlaWdodDogMjZweDtcXG5cXHRsaW5lLWhlaWdodDogMjZweDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIC5kYXRldGltZXBpY2tlci1ob3VycyB0YWJsZSB0ciB0ZCBzcGFuLmhvdXJfYW0sXFxuLmRhdGV0aW1lcGlja2VyIC5kYXRldGltZXBpY2tlci1ob3VycyB0YWJsZSB0ciB0ZCBzcGFuLmhvdXJfcG0ge1xcblxcdHdpZHRoOiAxNC42JTtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIC5kYXRldGltZXBpY2tlci1ob3VycyBmaWVsZHNldCBsZWdlbmQsXFxuLmRhdGV0aW1lcGlja2VyIC5kYXRldGltZXBpY2tlci1taW51dGVzIGZpZWxkc2V0IGxlZ2VuZCB7XFxuXFx0bWFyZ2luLWJvdHRvbTogaW5oZXJpdDtcXG5cXHRsaW5lLWhlaWdodDogMzBweDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIC5kYXRldGltZXBpY2tlci1taW51dGVzIHNwYW4ge1xcblxcdGhlaWdodDogMjZweDtcXG5cXHRsaW5lLWhlaWdodDogMjZweDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW46aG92ZXIge1xcblxcdGJhY2tncm91bmQ6ICNlZWVlZWU7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmRpc2FibGVkLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmRpc2FibGVkOmhvdmVyIHtcXG5cXHRiYWNrZ3JvdW5kOiBub25lO1xcblxcdGNvbG9yOiAjOTk5OTk5O1xcblxcdGN1cnNvcjogZGVmYXVsdDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZTpob3ZlcixcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmUuZGlzYWJsZWQsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlLmRpc2FibGVkOmhvdmVyIHtcXG5cXHRiYWNrZ3JvdW5kLWNvbG9yOiAjMDA2ZGNjO1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IC1tb3otbGluZWFyLWdyYWRpZW50KHRvcCwgIzAwODhjYywgIzAwNDRjYyk7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogLW1zLWxpbmVhci1ncmFkaWVudCh0b3AsICMwMDg4Y2MsICMwMDQ0Y2MpO1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IC13ZWJraXQtZ3JhZGllbnQobGluZWFyLCAwIDAsIDAgMTAwJSwgZnJvbSgjMDA4OGNjKSwgdG8oIzAwNDRjYykpO1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IC13ZWJraXQtbGluZWFyLWdyYWRpZW50KHRvcCwgIzAwODhjYywgIzAwNDRjYyk7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogLW8tbGluZWFyLWdyYWRpZW50KHRvcCwgIzAwODhjYywgIzAwNDRjYyk7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogbGluZWFyLWdyYWRpZW50KHRvIGJvdHRvbSwgIzAwODhjYywgIzAwNDRjYyk7XFxuXFx0YmFja2dyb3VuZC1yZXBlYXQ6IHJlcGVhdC14O1xcblxcdGZpbHRlcjogcHJvZ2lkOkRYSW1hZ2VUcmFuc2Zvcm0uTWljcm9zb2Z0LmdyYWRpZW50KHN0YXJ0Q29sb3JzdHI9JyMwMDg4Y2MnLCBlbmRDb2xvcnN0cj0nIzAwNDRjYycsIEdyYWRpZW50VHlwZT0wKTtcXG5cXHRib3JkZXItY29sb3I6ICMwMDQ0Y2MgIzAwNDRjYyAjMDAyYTgwO1xcblxcdGJvcmRlci1jb2xvcjogcmdiYSgwLCAwLCAwLCAwLjEpIHJnYmEoMCwgMCwgMCwgMC4xKSByZ2JhKDAsIDAsIDAsIDAuMjUpO1xcblxcdGZpbHRlcjogcHJvZ2lkOkRYSW1hZ2VUcmFuc2Zvcm0uTWljcm9zb2Z0LmdyYWRpZW50KGVuYWJsZWQ9ZmFsc2UpO1xcblxcdGNvbG9yOiAjZmZmZmZmO1xcblxcdHRleHQtc2hhZG93OiAwIC0xcHggMCByZ2JhKDAsIDAsIDAsIDAuMjUpO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmU6aG92ZXIsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlOmhvdmVyOmhvdmVyLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZS5kaXNhYmxlZDpob3ZlcixcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmUuZGlzYWJsZWQ6aG92ZXI6aG92ZXIsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlOmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmU6aG92ZXI6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZS5kaXNhYmxlZDphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlLmRpc2FibGVkOmhvdmVyOmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmUuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZTpob3Zlci5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlLmRpc2FibGVkLmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmUuZGlzYWJsZWQ6aG92ZXIuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZS5kaXNhYmxlZCxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmU6aG92ZXIuZGlzYWJsZWQsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlLmRpc2FibGVkLmRpc2FibGVkLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZS5kaXNhYmxlZDpob3Zlci5kaXNhYmxlZCxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmVbZGlzYWJsZWRdLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZTpob3ZlcltkaXNhYmxlZF0sXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlLmRpc2FibGVkW2Rpc2FibGVkXSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmUuZGlzYWJsZWQ6aG92ZXJbZGlzYWJsZWRdIHtcXG5cXHRiYWNrZ3JvdW5kLWNvbG9yOiAjMDA0NGNjO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmU6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZTpob3ZlcjphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlLmRpc2FibGVkOmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmUuZGlzYWJsZWQ6aG92ZXI6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZS5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlOmhvdmVyLmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmUuZGlzYWJsZWQuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZS5kaXNhYmxlZDpob3Zlci5hY3RpdmUge1xcblxcdGJhY2tncm91bmQtY29sb3I6ICMwMDMzOTk7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLm9sZCB7XFxuXFx0Y29sb3I6ICM5OTk5OTk7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciB0aC5zd2l0Y2gge1xcblxcdHdpZHRoOiAxNDVweDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRoIHNwYW4uZ2x5cGhpY29uIHtcXG5cXHRwb2ludGVyLWV2ZW50czogbm9uZTtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRoZWFkIHRyOmZpcnN0LWNoaWxkIHRoLFxcbi5kYXRldGltZXBpY2tlciB0Zm9vdCB0aCB7XFxuXFx0Y3Vyc29yOiBwb2ludGVyO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGhlYWQgdHI6Zmlyc3QtY2hpbGQgdGg6aG92ZXIsXFxuLmRhdGV0aW1lcGlja2VyIHRmb290IHRoOmhvdmVyIHtcXG5cXHRiYWNrZ3JvdW5kOiAjZWVlZWVlO1xcbn1cXG5cXG4uaW5wdXQtYXBwZW5kLmRhdGUgLmFkZC1vbiBpLFxcbi5pbnB1dC1wcmVwZW5kLmRhdGUgLmFkZC1vbiBpLFxcbi5pbnB1dC1ncm91cC5kYXRlIC5pbnB1dC1ncm91cC1hZGRvbiBzcGFuIHtcXG5cXHRjdXJzb3I6IHBvaW50ZXI7XFxuXFx0d2lkdGg6IDE0cHg7XFxuXFx0aGVpZ2h0OiAxNHB4O1xcbn1cXG5cIiwgXCJcIl0pO1xuXG4vLyBleHBvcnRzXG4iLCJ2YXIgZXNjYXBlID0gcmVxdWlyZShcIi4uLy4uLy4uL25vZGVfbW9kdWxlcy9jc3MtbG9hZGVyL2xpYi91cmwvZXNjYXBlLmpzXCIpO1xuZXhwb3J0cyA9IG1vZHVsZS5leHBvcnRzID0gcmVxdWlyZShcIi4uLy4uLy4uL25vZGVfbW9kdWxlcy9jc3MtbG9hZGVyL2xpYi9jc3MtYmFzZS5qc1wiKShmYWxzZSk7XG4vLyBpbXBvcnRzXG5cblxuLy8gbW9kdWxlXG5leHBvcnRzLnB1c2goW21vZHVsZS5pZCwgXCIvKiEgalF1ZXJ5IFVJIC0gdjEuMTIuMSAtIDIwMTctMTAtMTNcXG4qIGh0dHA6Ly9qcXVlcnl1aS5jb21cXG4qIEluY2x1ZGVzOiBkcmFnZ2FibGUuY3NzLCBjb3JlLmNzcywgcmVzaXphYmxlLmNzcywgc2VsZWN0YWJsZS5jc3MsIHNvcnRhYmxlLmNzcywgdGhlbWUuY3NzXFxuKiBUbyB2aWV3IGFuZCBtb2RpZnkgdGhpcyB0aGVtZSwgdmlzaXQgaHR0cDovL2pxdWVyeXVpLmNvbS90aGVtZXJvbGxlci8/c2NvcGU9JmZvbGRlck5hbWU9YmFzZSZjb3JuZXJSYWRpdXNTaGFkb3c9OHB4Jm9mZnNldExlZnRTaGFkb3c9MHB4Jm9mZnNldFRvcFNoYWRvdz0wcHgmdGhpY2tuZXNzU2hhZG93PTVweCZvcGFjaXR5U2hhZG93PTMwJmJnSW1nT3BhY2l0eVNoYWRvdz0wJmJnVGV4dHVyZVNoYWRvdz1mbGF0JmJnQ29sb3JTaGFkb3c9NjY2NjY2Jm9wYWNpdHlPdmVybGF5PTMwJmJnSW1nT3BhY2l0eU92ZXJsYXk9MCZiZ1RleHR1cmVPdmVybGF5PWZsYXQmYmdDb2xvck92ZXJsYXk9YWFhYWFhJmljb25Db2xvckVycm9yPWNjMDAwMCZmY0Vycm9yPTVmM2YzZiZib3JkZXJDb2xvckVycm9yPWYxYTg5OSZiZ1RleHR1cmVFcnJvcj1mbGF0JmJnQ29sb3JFcnJvcj1mZGRmZGYmaWNvbkNvbG9ySGlnaGxpZ2h0PTc3NzYyMCZmY0hpZ2hsaWdodD03Nzc2MjAmYm9yZGVyQ29sb3JIaWdobGlnaHQ9ZGFkNTVlJmJnVGV4dHVyZUhpZ2hsaWdodD1mbGF0JmJnQ29sb3JIaWdobGlnaHQ9ZmZmYTkwJmljb25Db2xvckFjdGl2ZT1mZmZmZmYmZmNBY3RpdmU9ZmZmZmZmJmJvcmRlckNvbG9yQWN0aXZlPTAwM2VmZiZiZ1RleHR1cmVBY3RpdmU9ZmxhdCZiZ0NvbG9yQWN0aXZlPTAwN2ZmZiZpY29uQ29sb3JIb3Zlcj01NTU1NTUmZmNIb3Zlcj0yYjJiMmImYm9yZGVyQ29sb3JIb3Zlcj1jY2NjY2MmYmdUZXh0dXJlSG92ZXI9ZmxhdCZiZ0NvbG9ySG92ZXI9ZWRlZGVkJmljb25Db2xvckRlZmF1bHQ9Nzc3Nzc3JmZjRGVmYXVsdD00NTQ1NDUmYm9yZGVyQ29sb3JEZWZhdWx0PWM1YzVjNSZiZ1RleHR1cmVEZWZhdWx0PWZsYXQmYmdDb2xvckRlZmF1bHQ9ZjZmNmY2Jmljb25Db2xvckNvbnRlbnQ9NDQ0NDQ0JmZjQ29udGVudD0zMzMzMzMmYm9yZGVyQ29sb3JDb250ZW50PWRkZGRkZCZiZ1RleHR1cmVDb250ZW50PWZsYXQmYmdDb2xvckNvbnRlbnQ9ZmZmZmZmJmljb25Db2xvckhlYWRlcj00NDQ0NDQmZmNIZWFkZXI9MzMzMzMzJmJvcmRlckNvbG9ySGVhZGVyPWRkZGRkZCZiZ1RleHR1cmVIZWFkZXI9ZmxhdCZiZ0NvbG9ySGVhZGVyPWU5ZTllOSZjb3JuZXJSYWRpdXM9M3B4JmZ3RGVmYXVsdD1ub3JtYWwmZnNEZWZhdWx0PTFlbSZmZkRlZmF1bHQ9QXJpYWwlMkNIZWx2ZXRpY2ElMkNzYW5zLXNlcmlmXFxuKiBDb3B5cmlnaHQgalF1ZXJ5IEZvdW5kYXRpb24gYW5kIG90aGVyIGNvbnRyaWJ1dG9yczsgTGljZW5zZWQgTUlUICovXFxuXFxuLnVpLWRyYWdnYWJsZS1oYW5kbGUge1xcblxcdC1tcy10b3VjaC1hY3Rpb246IG5vbmU7XFxuXFx0dG91Y2gtYWN0aW9uOiBub25lO1xcbn1cXG4vKiBMYXlvdXQgaGVscGVyc1xcbi0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0qL1xcbi51aS1oZWxwZXItaGlkZGVuIHtcXG5cXHRkaXNwbGF5OiBub25lO1xcbn1cXG4udWktaGVscGVyLWhpZGRlbi1hY2Nlc3NpYmxlIHtcXG5cXHRib3JkZXI6IDA7XFxuXFx0Y2xpcDogcmVjdCgwIDAgMCAwKTtcXG5cXHRoZWlnaHQ6IDFweDtcXG5cXHRtYXJnaW46IC0xcHg7XFxuXFx0b3ZlcmZsb3c6IGhpZGRlbjtcXG5cXHRwYWRkaW5nOiAwO1xcblxcdHBvc2l0aW9uOiBhYnNvbHV0ZTtcXG5cXHR3aWR0aDogMXB4O1xcbn1cXG4udWktaGVscGVyLXJlc2V0IHtcXG5cXHRtYXJnaW46IDA7XFxuXFx0cGFkZGluZzogMDtcXG5cXHRib3JkZXI6IDA7XFxuXFx0b3V0bGluZTogMDtcXG5cXHRsaW5lLWhlaWdodDogMS4zO1xcblxcdHRleHQtZGVjb3JhdGlvbjogbm9uZTtcXG5cXHRmb250LXNpemU6IDEwMCU7XFxuXFx0bGlzdC1zdHlsZTogbm9uZTtcXG59XFxuLnVpLWhlbHBlci1jbGVhcmZpeDpiZWZvcmUsXFxuLnVpLWhlbHBlci1jbGVhcmZpeDphZnRlciB7XFxuXFx0Y29udGVudDogXFxcIlxcXCI7XFxuXFx0ZGlzcGxheTogdGFibGU7XFxuXFx0Ym9yZGVyLWNvbGxhcHNlOiBjb2xsYXBzZTtcXG59XFxuLnVpLWhlbHBlci1jbGVhcmZpeDphZnRlciB7XFxuXFx0Y2xlYXI6IGJvdGg7XFxufVxcbi51aS1oZWxwZXItemZpeCB7XFxuXFx0d2lkdGg6IDEwMCU7XFxuXFx0aGVpZ2h0OiAxMDAlO1xcblxcdHRvcDogMDtcXG5cXHRsZWZ0OiAwO1xcblxcdHBvc2l0aW9uOiBhYnNvbHV0ZTtcXG5cXHRvcGFjaXR5OiAwO1xcblxcdGZpbHRlcjpBbHBoYShPcGFjaXR5PTApOyAvKiBzdXBwb3J0OiBJRTggKi9cXG59XFxuXFxuLnVpLWZyb250IHtcXG5cXHR6LWluZGV4OiAxMDA7XFxufVxcblxcblxcbi8qIEludGVyYWN0aW9uIEN1ZXNcXG4tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tKi9cXG4udWktc3RhdGUtZGlzYWJsZWQge1xcblxcdGN1cnNvcjogZGVmYXVsdCAhaW1wb3J0YW50O1xcblxcdHBvaW50ZXItZXZlbnRzOiBub25lO1xcbn1cXG5cXG5cXG4vKiBJY29uc1xcbi0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0qL1xcbi51aS1pY29uIHtcXG5cXHRkaXNwbGF5OiBpbmxpbmUtYmxvY2s7XFxuXFx0dmVydGljYWwtYWxpZ246IG1pZGRsZTtcXG5cXHRtYXJnaW4tdG9wOiAtLjI1ZW07XFxuXFx0cG9zaXRpb246IHJlbGF0aXZlO1xcblxcdHRleHQtaW5kZW50OiAtOTk5OTlweDtcXG5cXHRvdmVyZmxvdzogaGlkZGVuO1xcblxcdGJhY2tncm91bmQtcmVwZWF0OiBuby1yZXBlYXQ7XFxufVxcblxcbi51aS13aWRnZXQtaWNvbi1ibG9jayB7XFxuXFx0bGVmdDogNTAlO1xcblxcdG1hcmdpbi1sZWZ0OiAtOHB4O1xcblxcdGRpc3BsYXk6IGJsb2NrO1xcbn1cXG5cXG4vKiBNaXNjIHZpc3VhbHNcXG4tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tKi9cXG5cXG4vKiBPdmVybGF5cyAqL1xcbi51aS13aWRnZXQtb3ZlcmxheSB7XFxuXFx0cG9zaXRpb246IGZpeGVkO1xcblxcdHRvcDogMDtcXG5cXHRsZWZ0OiAwO1xcblxcdHdpZHRoOiAxMDAlO1xcblxcdGhlaWdodDogMTAwJTtcXG59XFxuLnVpLXJlc2l6YWJsZSB7XFxuXFx0cG9zaXRpb246IHJlbGF0aXZlO1xcbn1cXG4udWktcmVzaXphYmxlLWhhbmRsZSB7XFxuXFx0cG9zaXRpb246IGFic29sdXRlO1xcblxcdGZvbnQtc2l6ZTogMC4xcHg7XFxuXFx0ZGlzcGxheTogYmxvY2s7XFxuXFx0LW1zLXRvdWNoLWFjdGlvbjogbm9uZTtcXG5cXHR0b3VjaC1hY3Rpb246IG5vbmU7XFxufVxcbi51aS1yZXNpemFibGUtZGlzYWJsZWQgLnVpLXJlc2l6YWJsZS1oYW5kbGUsXFxuLnVpLXJlc2l6YWJsZS1hdXRvaGlkZSAudWktcmVzaXphYmxlLWhhbmRsZSB7XFxuXFx0ZGlzcGxheTogbm9uZTtcXG59XFxuLnVpLXJlc2l6YWJsZS1uIHtcXG5cXHRjdXJzb3I6IG4tcmVzaXplO1xcblxcdGhlaWdodDogN3B4O1xcblxcdHdpZHRoOiAxMDAlO1xcblxcdHRvcDogLTVweDtcXG5cXHRsZWZ0OiAwO1xcbn1cXG4udWktcmVzaXphYmxlLXMge1xcblxcdGN1cnNvcjogcy1yZXNpemU7XFxuXFx0aGVpZ2h0OiA3cHg7XFxuXFx0d2lkdGg6IDEwMCU7XFxuXFx0Ym90dG9tOiAtNXB4O1xcblxcdGxlZnQ6IDA7XFxufVxcbi51aS1yZXNpemFibGUtZSB7XFxuXFx0Y3Vyc29yOiBlLXJlc2l6ZTtcXG5cXHR3aWR0aDogN3B4O1xcblxcdHJpZ2h0OiAtNXB4O1xcblxcdHRvcDogMDtcXG5cXHRoZWlnaHQ6IDEwMCU7XFxufVxcbi51aS1yZXNpemFibGUtdyB7XFxuXFx0Y3Vyc29yOiB3LXJlc2l6ZTtcXG5cXHR3aWR0aDogN3B4O1xcblxcdGxlZnQ6IC01cHg7XFxuXFx0dG9wOiAwO1xcblxcdGhlaWdodDogMTAwJTtcXG59XFxuLnVpLXJlc2l6YWJsZS1zZSB7XFxuXFx0Y3Vyc29yOiBzZS1yZXNpemU7XFxuXFx0d2lkdGg6IDEycHg7XFxuXFx0aGVpZ2h0OiAxMnB4O1xcblxcdHJpZ2h0OiAxcHg7XFxuXFx0Ym90dG9tOiAxcHg7XFxufVxcbi51aS1yZXNpemFibGUtc3cge1xcblxcdGN1cnNvcjogc3ctcmVzaXplO1xcblxcdHdpZHRoOiA5cHg7XFxuXFx0aGVpZ2h0OiA5cHg7XFxuXFx0bGVmdDogLTVweDtcXG5cXHRib3R0b206IC01cHg7XFxufVxcbi51aS1yZXNpemFibGUtbncge1xcblxcdGN1cnNvcjogbnctcmVzaXplO1xcblxcdHdpZHRoOiA5cHg7XFxuXFx0aGVpZ2h0OiA5cHg7XFxuXFx0bGVmdDogLTVweDtcXG5cXHR0b3A6IC01cHg7XFxufVxcbi51aS1yZXNpemFibGUtbmUge1xcblxcdGN1cnNvcjogbmUtcmVzaXplO1xcblxcdHdpZHRoOiA5cHg7XFxuXFx0aGVpZ2h0OiA5cHg7XFxuXFx0cmlnaHQ6IC01cHg7XFxuXFx0dG9wOiAtNXB4O1xcbn1cXG4udWktc2VsZWN0YWJsZSB7XFxuXFx0LW1zLXRvdWNoLWFjdGlvbjogbm9uZTtcXG5cXHR0b3VjaC1hY3Rpb246IG5vbmU7XFxufVxcbi51aS1zZWxlY3RhYmxlLWhlbHBlciB7XFxuXFx0cG9zaXRpb246IGFic29sdXRlO1xcblxcdHotaW5kZXg6IDEwMDtcXG5cXHRib3JkZXI6IDFweCBkb3R0ZWQgYmxhY2s7XFxufVxcbi51aS1zb3J0YWJsZS1oYW5kbGUge1xcblxcdC1tcy10b3VjaC1hY3Rpb246IG5vbmU7XFxuXFx0dG91Y2gtYWN0aW9uOiBub25lO1xcbn1cXG5cXG4vKiBDb21wb25lbnQgY29udGFpbmVyc1xcbi0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0qL1xcbi51aS13aWRnZXQge1xcblxcdGZvbnQtZmFtaWx5OiBBcmlhbCxIZWx2ZXRpY2Esc2Fucy1zZXJpZjtcXG5cXHRmb250LXNpemU6IDFlbTtcXG59XFxuLnVpLXdpZGdldCAudWktd2lkZ2V0IHtcXG5cXHRmb250LXNpemU6IDFlbTtcXG59XFxuLnVpLXdpZGdldCBpbnB1dCxcXG4udWktd2lkZ2V0IHNlbGVjdCxcXG4udWktd2lkZ2V0IHRleHRhcmVhLFxcbi51aS13aWRnZXQgYnV0dG9uIHtcXG5cXHRmb250LWZhbWlseTogQXJpYWwsSGVsdmV0aWNhLHNhbnMtc2VyaWY7XFxuXFx0Zm9udC1zaXplOiAxZW07XFxufVxcbi51aS13aWRnZXQudWktd2lkZ2V0LWNvbnRlbnQge1xcblxcdGJvcmRlcjogMXB4IHNvbGlkICNjNWM1YzU7XFxufVxcbi51aS13aWRnZXQtY29udGVudCB7XFxuXFx0Ym9yZGVyOiAxcHggc29saWQgI2RkZGRkZDtcXG5cXHRiYWNrZ3JvdW5kOiAjZmZmZmZmO1xcblxcdGNvbG9yOiAjMzMzMzMzO1xcbn1cXG4udWktd2lkZ2V0LWNvbnRlbnQgYSB7XFxuXFx0Y29sb3I6ICMzMzMzMzM7XFxufVxcbi51aS13aWRnZXQtaGVhZGVyIHtcXG5cXHRib3JkZXI6IDFweCBzb2xpZCAjZGRkZGRkO1xcblxcdGJhY2tncm91bmQ6ICNlOWU5ZTk7XFxuXFx0Y29sb3I6ICMzMzMzMzM7XFxuXFx0Zm9udC13ZWlnaHQ6IGJvbGQ7XFxufVxcbi51aS13aWRnZXQtaGVhZGVyIGEge1xcblxcdGNvbG9yOiAjMzMzMzMzO1xcbn1cXG5cXG4vKiBJbnRlcmFjdGlvbiBzdGF0ZXNcXG4tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tKi9cXG4udWktc3RhdGUtZGVmYXVsdCxcXG4udWktd2lkZ2V0LWNvbnRlbnQgLnVpLXN0YXRlLWRlZmF1bHQsXFxuLnVpLXdpZGdldC1oZWFkZXIgLnVpLXN0YXRlLWRlZmF1bHQsXFxuLnVpLWJ1dHRvbixcXG5cXG4vKiBXZSB1c2UgaHRtbCBoZXJlIGJlY2F1c2Ugd2UgbmVlZCBhIGdyZWF0ZXIgc3BlY2lmaWNpdHkgdG8gbWFrZSBzdXJlIGRpc2FibGVkXFxud29ya3MgcHJvcGVybHkgd2hlbiBjbGlja2VkIG9yIGhvdmVyZWQgKi9cXG5odG1sIC51aS1idXR0b24udWktc3RhdGUtZGlzYWJsZWQ6aG92ZXIsXFxuaHRtbCAudWktYnV0dG9uLnVpLXN0YXRlLWRpc2FibGVkOmFjdGl2ZSB7XFxuXFx0Ym9yZGVyOiAxcHggc29saWQgI2M1YzVjNTtcXG5cXHRiYWNrZ3JvdW5kOiAjZjZmNmY2O1xcblxcdGZvbnQtd2VpZ2h0OiBub3JtYWw7XFxuXFx0Y29sb3I6ICM0NTQ1NDU7XFxufVxcbi51aS1zdGF0ZS1kZWZhdWx0IGEsXFxuLnVpLXN0YXRlLWRlZmF1bHQgYTpsaW5rLFxcbi51aS1zdGF0ZS1kZWZhdWx0IGE6dmlzaXRlZCxcXG5hLnVpLWJ1dHRvbixcXG5hOmxpbmsudWktYnV0dG9uLFxcbmE6dmlzaXRlZC51aS1idXR0b24sXFxuLnVpLWJ1dHRvbiB7XFxuXFx0Y29sb3I6ICM0NTQ1NDU7XFxuXFx0dGV4dC1kZWNvcmF0aW9uOiBub25lO1xcbn1cXG4udWktc3RhdGUtaG92ZXIsXFxuLnVpLXdpZGdldC1jb250ZW50IC51aS1zdGF0ZS1ob3ZlcixcXG4udWktd2lkZ2V0LWhlYWRlciAudWktc3RhdGUtaG92ZXIsXFxuLnVpLXN0YXRlLWZvY3VzLFxcbi51aS13aWRnZXQtY29udGVudCAudWktc3RhdGUtZm9jdXMsXFxuLnVpLXdpZGdldC1oZWFkZXIgLnVpLXN0YXRlLWZvY3VzLFxcbi51aS1idXR0b246aG92ZXIsXFxuLnVpLWJ1dHRvbjpmb2N1cyB7XFxuXFx0Ym9yZGVyOiAxcHggc29saWQgI2NjY2NjYztcXG5cXHRiYWNrZ3JvdW5kOiAjZWRlZGVkO1xcblxcdGZvbnQtd2VpZ2h0OiBub3JtYWw7XFxuXFx0Y29sb3I6ICMyYjJiMmI7XFxufVxcbi51aS1zdGF0ZS1ob3ZlciBhLFxcbi51aS1zdGF0ZS1ob3ZlciBhOmhvdmVyLFxcbi51aS1zdGF0ZS1ob3ZlciBhOmxpbmssXFxuLnVpLXN0YXRlLWhvdmVyIGE6dmlzaXRlZCxcXG4udWktc3RhdGUtZm9jdXMgYSxcXG4udWktc3RhdGUtZm9jdXMgYTpob3ZlcixcXG4udWktc3RhdGUtZm9jdXMgYTpsaW5rLFxcbi51aS1zdGF0ZS1mb2N1cyBhOnZpc2l0ZWQsXFxuYS51aS1idXR0b246aG92ZXIsXFxuYS51aS1idXR0b246Zm9jdXMge1xcblxcdGNvbG9yOiAjMmIyYjJiO1xcblxcdHRleHQtZGVjb3JhdGlvbjogbm9uZTtcXG59XFxuXFxuLnVpLXZpc3VhbC1mb2N1cyB7XFxuXFx0Ym94LXNoYWRvdzogMCAwIDNweCAxcHggcmdiKDk0LCAxNTgsIDIxNCk7XFxufVxcbi51aS1zdGF0ZS1hY3RpdmUsXFxuLnVpLXdpZGdldC1jb250ZW50IC51aS1zdGF0ZS1hY3RpdmUsXFxuLnVpLXdpZGdldC1oZWFkZXIgLnVpLXN0YXRlLWFjdGl2ZSxcXG5hLnVpLWJ1dHRvbjphY3RpdmUsXFxuLnVpLWJ1dHRvbjphY3RpdmUsXFxuLnVpLWJ1dHRvbi51aS1zdGF0ZS1hY3RpdmU6aG92ZXIge1xcblxcdGJvcmRlcjogMXB4IHNvbGlkICMwMDNlZmY7XFxuXFx0YmFja2dyb3VuZDogIzAwN2ZmZjtcXG5cXHRmb250LXdlaWdodDogbm9ybWFsO1xcblxcdGNvbG9yOiAjZmZmZmZmO1xcbn1cXG4udWktaWNvbi1iYWNrZ3JvdW5kLFxcbi51aS1zdGF0ZS1hY3RpdmUgLnVpLWljb24tYmFja2dyb3VuZCB7XFxuXFx0Ym9yZGVyOiAjMDAzZWZmO1xcblxcdGJhY2tncm91bmQtY29sb3I6ICNmZmZmZmY7XFxufVxcbi51aS1zdGF0ZS1hY3RpdmUgYSxcXG4udWktc3RhdGUtYWN0aXZlIGE6bGluayxcXG4udWktc3RhdGUtYWN0aXZlIGE6dmlzaXRlZCB7XFxuXFx0Y29sb3I6ICNmZmZmZmY7XFxuXFx0dGV4dC1kZWNvcmF0aW9uOiBub25lO1xcbn1cXG5cXG4vKiBJbnRlcmFjdGlvbiBDdWVzXFxuLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLSovXFxuLnVpLXN0YXRlLWhpZ2hsaWdodCxcXG4udWktd2lkZ2V0LWNvbnRlbnQgLnVpLXN0YXRlLWhpZ2hsaWdodCxcXG4udWktd2lkZ2V0LWhlYWRlciAudWktc3RhdGUtaGlnaGxpZ2h0IHtcXG5cXHRib3JkZXI6IDFweCBzb2xpZCAjZGFkNTVlO1xcblxcdGJhY2tncm91bmQ6ICNmZmZhOTA7XFxuXFx0Y29sb3I6ICM3Nzc2MjA7XFxufVxcbi51aS1zdGF0ZS1jaGVja2VkIHtcXG5cXHRib3JkZXI6IDFweCBzb2xpZCAjZGFkNTVlO1xcblxcdGJhY2tncm91bmQ6ICNmZmZhOTA7XFxufVxcbi51aS1zdGF0ZS1oaWdobGlnaHQgYSxcXG4udWktd2lkZ2V0LWNvbnRlbnQgLnVpLXN0YXRlLWhpZ2hsaWdodCBhLFxcbi51aS13aWRnZXQtaGVhZGVyIC51aS1zdGF0ZS1oaWdobGlnaHQgYSB7XFxuXFx0Y29sb3I6ICM3Nzc2MjA7XFxufVxcbi51aS1zdGF0ZS1lcnJvcixcXG4udWktd2lkZ2V0LWNvbnRlbnQgLnVpLXN0YXRlLWVycm9yLFxcbi51aS13aWRnZXQtaGVhZGVyIC51aS1zdGF0ZS1lcnJvciB7XFxuXFx0Ym9yZGVyOiAxcHggc29saWQgI2YxYTg5OTtcXG5cXHRiYWNrZ3JvdW5kOiAjZmRkZmRmO1xcblxcdGNvbG9yOiAjNWYzZjNmO1xcbn1cXG4udWktc3RhdGUtZXJyb3IgYSxcXG4udWktd2lkZ2V0LWNvbnRlbnQgLnVpLXN0YXRlLWVycm9yIGEsXFxuLnVpLXdpZGdldC1oZWFkZXIgLnVpLXN0YXRlLWVycm9yIGEge1xcblxcdGNvbG9yOiAjNWYzZjNmO1xcbn1cXG4udWktc3RhdGUtZXJyb3ItdGV4dCxcXG4udWktd2lkZ2V0LWNvbnRlbnQgLnVpLXN0YXRlLWVycm9yLXRleHQsXFxuLnVpLXdpZGdldC1oZWFkZXIgLnVpLXN0YXRlLWVycm9yLXRleHQge1xcblxcdGNvbG9yOiAjNWYzZjNmO1xcbn1cXG4udWktcHJpb3JpdHktcHJpbWFyeSxcXG4udWktd2lkZ2V0LWNvbnRlbnQgLnVpLXByaW9yaXR5LXByaW1hcnksXFxuLnVpLXdpZGdldC1oZWFkZXIgLnVpLXByaW9yaXR5LXByaW1hcnkge1xcblxcdGZvbnQtd2VpZ2h0OiBib2xkO1xcbn1cXG4udWktcHJpb3JpdHktc2Vjb25kYXJ5LFxcbi51aS13aWRnZXQtY29udGVudCAudWktcHJpb3JpdHktc2Vjb25kYXJ5LFxcbi51aS13aWRnZXQtaGVhZGVyIC51aS1wcmlvcml0eS1zZWNvbmRhcnkge1xcblxcdG9wYWNpdHk6IC43O1xcblxcdGZpbHRlcjpBbHBoYShPcGFjaXR5PTcwKTsgLyogc3VwcG9ydDogSUU4ICovXFxuXFx0Zm9udC13ZWlnaHQ6IG5vcm1hbDtcXG59XFxuLnVpLXN0YXRlLWRpc2FibGVkLFxcbi51aS13aWRnZXQtY29udGVudCAudWktc3RhdGUtZGlzYWJsZWQsXFxuLnVpLXdpZGdldC1oZWFkZXIgLnVpLXN0YXRlLWRpc2FibGVkIHtcXG5cXHRvcGFjaXR5OiAuMzU7XFxuXFx0ZmlsdGVyOkFscGhhKE9wYWNpdHk9MzUpOyAvKiBzdXBwb3J0OiBJRTggKi9cXG5cXHRiYWNrZ3JvdW5kLWltYWdlOiBub25lO1xcbn1cXG4udWktc3RhdGUtZGlzYWJsZWQgLnVpLWljb24ge1xcblxcdGZpbHRlcjpBbHBoYShPcGFjaXR5PTM1KTsgLyogc3VwcG9ydDogSUU4IC0gU2VlICM2MDU5ICovXFxufVxcblxcbi8qIEljb25zXFxuLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLSovXFxuXFxuLyogc3RhdGVzIGFuZCBpbWFnZXMgKi9cXG4udWktaWNvbiB7XFxuXFx0d2lkdGg6IDE2cHg7XFxuXFx0aGVpZ2h0OiAxNnB4O1xcbn1cXG4udWktaWNvbixcXG4udWktd2lkZ2V0LWNvbnRlbnQgLnVpLWljb24ge1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IHVybChcIiArIGVzY2FwZShyZXF1aXJlKFwiLi9pbWFnZXMvdWktaWNvbnNfNDQ0NDQ0XzI1NngyNDAucG5nXCIpKSArIFwiKTtcXG59XFxuLnVpLXdpZGdldC1oZWFkZXIgLnVpLWljb24ge1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IHVybChcIiArIGVzY2FwZShyZXF1aXJlKFwiLi9pbWFnZXMvdWktaWNvbnNfNDQ0NDQ0XzI1NngyNDAucG5nXCIpKSArIFwiKTtcXG59XFxuLnVpLXN0YXRlLWhvdmVyIC51aS1pY29uLFxcbi51aS1zdGF0ZS1mb2N1cyAudWktaWNvbixcXG4udWktYnV0dG9uOmhvdmVyIC51aS1pY29uLFxcbi51aS1idXR0b246Zm9jdXMgLnVpLWljb24ge1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IHVybChcIiArIGVzY2FwZShyZXF1aXJlKFwiLi9pbWFnZXMvdWktaWNvbnNfNTU1NTU1XzI1NngyNDAucG5nXCIpKSArIFwiKTtcXG59XFxuLnVpLXN0YXRlLWFjdGl2ZSAudWktaWNvbixcXG4udWktYnV0dG9uOmFjdGl2ZSAudWktaWNvbiB7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogdXJsKFwiICsgZXNjYXBlKHJlcXVpcmUoXCIuL2ltYWdlcy91aS1pY29uc19mZmZmZmZfMjU2eDI0MC5wbmdcIikpICsgXCIpO1xcbn1cXG4udWktc3RhdGUtaGlnaGxpZ2h0IC51aS1pY29uLFxcbi51aS1idXR0b24gLnVpLXN0YXRlLWhpZ2hsaWdodC51aS1pY29uIHtcXG5cXHRiYWNrZ3JvdW5kLWltYWdlOiB1cmwoXCIgKyBlc2NhcGUocmVxdWlyZShcIi4vaW1hZ2VzL3VpLWljb25zXzc3NzYyMF8yNTZ4MjQwLnBuZ1wiKSkgKyBcIik7XFxufVxcbi51aS1zdGF0ZS1lcnJvciAudWktaWNvbixcXG4udWktc3RhdGUtZXJyb3ItdGV4dCAudWktaWNvbiB7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogdXJsKFwiICsgZXNjYXBlKHJlcXVpcmUoXCIuL2ltYWdlcy91aS1pY29uc19jYzAwMDBfMjU2eDI0MC5wbmdcIikpICsgXCIpO1xcbn1cXG4udWktYnV0dG9uIC51aS1pY29uIHtcXG5cXHRiYWNrZ3JvdW5kLWltYWdlOiB1cmwoXCIgKyBlc2NhcGUocmVxdWlyZShcIi4vaW1hZ2VzL3VpLWljb25zXzc3Nzc3N18yNTZ4MjQwLnBuZ1wiKSkgKyBcIik7XFxufVxcblxcbi8qIHBvc2l0aW9uaW5nICovXFxuLnVpLWljb24tYmxhbmsgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAxNnB4IDE2cHg7IH1cXG4udWktaWNvbi1jYXJldC0xLW4geyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAwIDA7IH1cXG4udWktaWNvbi1jYXJldC0xLW5lIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE2cHggMDsgfVxcbi51aS1pY29uLWNhcmV0LTEtZSB7IGJhY2tncm91bmQtcG9zaXRpb246IC0zMnB4IDA7IH1cXG4udWktaWNvbi1jYXJldC0xLXNlIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTQ4cHggMDsgfVxcbi51aS1pY29uLWNhcmV0LTEtcyB7IGJhY2tncm91bmQtcG9zaXRpb246IC02NXB4IDA7IH1cXG4udWktaWNvbi1jYXJldC0xLXN3IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTgwcHggMDsgfVxcbi51aS1pY29uLWNhcmV0LTEtdyB7IGJhY2tncm91bmQtcG9zaXRpb246IC05NnB4IDA7IH1cXG4udWktaWNvbi1jYXJldC0xLW53IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTExMnB4IDA7IH1cXG4udWktaWNvbi1jYXJldC0yLW4tcyB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xMjhweCAwOyB9XFxuLnVpLWljb24tY2FyZXQtMi1lLXcgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTQ0cHggMDsgfVxcbi51aS1pY29uLXRyaWFuZ2xlLTEtbiB7IGJhY2tncm91bmQtcG9zaXRpb246IDAgLTE2cHg7IH1cXG4udWktaWNvbi10cmlhbmdsZS0xLW5lIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE2cHggLTE2cHg7IH1cXG4udWktaWNvbi10cmlhbmdsZS0xLWUgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMzJweCAtMTZweDsgfVxcbi51aS1pY29uLXRyaWFuZ2xlLTEtc2UgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtNDhweCAtMTZweDsgfVxcbi51aS1pY29uLXRyaWFuZ2xlLTEtcyB7IGJhY2tncm91bmQtcG9zaXRpb246IC02NXB4IC0xNnB4OyB9XFxuLnVpLWljb24tdHJpYW5nbGUtMS1zdyB7IGJhY2tncm91bmQtcG9zaXRpb246IC04MHB4IC0xNnB4OyB9XFxuLnVpLWljb24tdHJpYW5nbGUtMS13IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTk2cHggLTE2cHg7IH1cXG4udWktaWNvbi10cmlhbmdsZS0xLW53IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTExMnB4IC0xNnB4OyB9XFxuLnVpLWljb24tdHJpYW5nbGUtMi1uLXMgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTI4cHggLTE2cHg7IH1cXG4udWktaWNvbi10cmlhbmdsZS0yLWUtdyB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xNDRweCAtMTZweDsgfVxcbi51aS1pY29uLWFycm93LTEtbiB7IGJhY2tncm91bmQtcG9zaXRpb246IDAgLTMycHg7IH1cXG4udWktaWNvbi1hcnJvdy0xLW5lIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE2cHggLTMycHg7IH1cXG4udWktaWNvbi1hcnJvdy0xLWUgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMzJweCAtMzJweDsgfVxcbi51aS1pY29uLWFycm93LTEtc2UgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtNDhweCAtMzJweDsgfVxcbi51aS1pY29uLWFycm93LTEtcyB7IGJhY2tncm91bmQtcG9zaXRpb246IC02NXB4IC0zMnB4OyB9XFxuLnVpLWljb24tYXJyb3ctMS1zdyB7IGJhY2tncm91bmQtcG9zaXRpb246IC04MHB4IC0zMnB4OyB9XFxuLnVpLWljb24tYXJyb3ctMS13IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTk2cHggLTMycHg7IH1cXG4udWktaWNvbi1hcnJvdy0xLW53IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTExMnB4IC0zMnB4OyB9XFxuLnVpLWljb24tYXJyb3ctMi1uLXMgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTI4cHggLTMycHg7IH1cXG4udWktaWNvbi1hcnJvdy0yLW5lLXN3IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE0NHB4IC0zMnB4OyB9XFxuLnVpLWljb24tYXJyb3ctMi1lLXcgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTYwcHggLTMycHg7IH1cXG4udWktaWNvbi1hcnJvdy0yLXNlLW53IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE3NnB4IC0zMnB4OyB9XFxuLnVpLWljb24tYXJyb3dzdG9wLTEtbiB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xOTJweCAtMzJweDsgfVxcbi51aS1pY29uLWFycm93c3RvcC0xLWUgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMjA4cHggLTMycHg7IH1cXG4udWktaWNvbi1hcnJvd3N0b3AtMS1zIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTIyNHB4IC0zMnB4OyB9XFxuLnVpLWljb24tYXJyb3dzdG9wLTEtdyB7IGJhY2tncm91bmQtcG9zaXRpb246IC0yNDBweCAtMzJweDsgfVxcbi51aS1pY29uLWFycm93dGhpY2stMS1uIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogMXB4IC00OHB4OyB9XFxuLnVpLWljb24tYXJyb3d0aGljay0xLW5lIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE2cHggLTQ4cHg7IH1cXG4udWktaWNvbi1hcnJvd3RoaWNrLTEtZSB7IGJhY2tncm91bmQtcG9zaXRpb246IC0zMnB4IC00OHB4OyB9XFxuLnVpLWljb24tYXJyb3d0aGljay0xLXNlIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTQ4cHggLTQ4cHg7IH1cXG4udWktaWNvbi1hcnJvd3RoaWNrLTEtcyB7IGJhY2tncm91bmQtcG9zaXRpb246IC02NHB4IC00OHB4OyB9XFxuLnVpLWljb24tYXJyb3d0aGljay0xLXN3IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTgwcHggLTQ4cHg7IH1cXG4udWktaWNvbi1hcnJvd3RoaWNrLTEtdyB7IGJhY2tncm91bmQtcG9zaXRpb246IC05NnB4IC00OHB4OyB9XFxuLnVpLWljb24tYXJyb3d0aGljay0xLW53IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTExMnB4IC00OHB4OyB9XFxuLnVpLWljb24tYXJyb3d0aGljay0yLW4tcyB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xMjhweCAtNDhweDsgfVxcbi51aS1pY29uLWFycm93dGhpY2stMi1uZS1zdyB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xNDRweCAtNDhweDsgfVxcbi51aS1pY29uLWFycm93dGhpY2stMi1lLXcgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTYwcHggLTQ4cHg7IH1cXG4udWktaWNvbi1hcnJvd3RoaWNrLTItc2UtbncgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTc2cHggLTQ4cHg7IH1cXG4udWktaWNvbi1hcnJvd3RoaWNrc3RvcC0xLW4geyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTkycHggLTQ4cHg7IH1cXG4udWktaWNvbi1hcnJvd3RoaWNrc3RvcC0xLWUgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMjA4cHggLTQ4cHg7IH1cXG4udWktaWNvbi1hcnJvd3RoaWNrc3RvcC0xLXMgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMjI0cHggLTQ4cHg7IH1cXG4udWktaWNvbi1hcnJvd3RoaWNrc3RvcC0xLXcgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMjQwcHggLTQ4cHg7IH1cXG4udWktaWNvbi1hcnJvd3JldHVybnRoaWNrLTEtdyB7IGJhY2tncm91bmQtcG9zaXRpb246IDAgLTY0cHg7IH1cXG4udWktaWNvbi1hcnJvd3JldHVybnRoaWNrLTEtbiB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xNnB4IC02NHB4OyB9XFxuLnVpLWljb24tYXJyb3dyZXR1cm50aGljay0xLWUgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMzJweCAtNjRweDsgfVxcbi51aS1pY29uLWFycm93cmV0dXJudGhpY2stMS1zIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTQ4cHggLTY0cHg7IH1cXG4udWktaWNvbi1hcnJvd3JldHVybi0xLXcgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtNjRweCAtNjRweDsgfVxcbi51aS1pY29uLWFycm93cmV0dXJuLTEtbiB7IGJhY2tncm91bmQtcG9zaXRpb246IC04MHB4IC02NHB4OyB9XFxuLnVpLWljb24tYXJyb3dyZXR1cm4tMS1lIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTk2cHggLTY0cHg7IH1cXG4udWktaWNvbi1hcnJvd3JldHVybi0xLXMgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTEycHggLTY0cHg7IH1cXG4udWktaWNvbi1hcnJvd3JlZnJlc2gtMS13IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTEyOHB4IC02NHB4OyB9XFxuLnVpLWljb24tYXJyb3dyZWZyZXNoLTEtbiB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xNDRweCAtNjRweDsgfVxcbi51aS1pY29uLWFycm93cmVmcmVzaC0xLWUgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTYwcHggLTY0cHg7IH1cXG4udWktaWNvbi1hcnJvd3JlZnJlc2gtMS1zIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE3NnB4IC02NHB4OyB9XFxuLnVpLWljb24tYXJyb3ctNCB7IGJhY2tncm91bmQtcG9zaXRpb246IDAgLTgwcHg7IH1cXG4udWktaWNvbi1hcnJvdy00LWRpYWcgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTZweCAtODBweDsgfVxcbi51aS1pY29uLWV4dGxpbmsgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMzJweCAtODBweDsgfVxcbi51aS1pY29uLW5ld3dpbiB7IGJhY2tncm91bmQtcG9zaXRpb246IC00OHB4IC04MHB4OyB9XFxuLnVpLWljb24tcmVmcmVzaCB7IGJhY2tncm91bmQtcG9zaXRpb246IC02NHB4IC04MHB4OyB9XFxuLnVpLWljb24tc2h1ZmZsZSB7IGJhY2tncm91bmQtcG9zaXRpb246IC04MHB4IC04MHB4OyB9XFxuLnVpLWljb24tdHJhbnNmZXItZS13IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTk2cHggLTgwcHg7IH1cXG4udWktaWNvbi10cmFuc2ZlcnRoaWNrLWUtdyB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xMTJweCAtODBweDsgfVxcbi51aS1pY29uLWZvbGRlci1jb2xsYXBzZWQgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAwIC05NnB4OyB9XFxuLnVpLWljb24tZm9sZGVyLW9wZW4geyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTZweCAtOTZweDsgfVxcbi51aS1pY29uLWRvY3VtZW50IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTMycHggLTk2cHg7IH1cXG4udWktaWNvbi1kb2N1bWVudC1iIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTQ4cHggLTk2cHg7IH1cXG4udWktaWNvbi1ub3RlIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTY0cHggLTk2cHg7IH1cXG4udWktaWNvbi1tYWlsLWNsb3NlZCB7IGJhY2tncm91bmQtcG9zaXRpb246IC04MHB4IC05NnB4OyB9XFxuLnVpLWljb24tbWFpbC1vcGVuIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTk2cHggLTk2cHg7IH1cXG4udWktaWNvbi1zdWl0Y2FzZSB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xMTJweCAtOTZweDsgfVxcbi51aS1pY29uLWNvbW1lbnQgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTI4cHggLTk2cHg7IH1cXG4udWktaWNvbi1wZXJzb24geyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTQ0cHggLTk2cHg7IH1cXG4udWktaWNvbi1wcmludCB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xNjBweCAtOTZweDsgfVxcbi51aS1pY29uLXRyYXNoIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE3NnB4IC05NnB4OyB9XFxuLnVpLWljb24tbG9ja2VkIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE5MnB4IC05NnB4OyB9XFxuLnVpLWljb24tdW5sb2NrZWQgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMjA4cHggLTk2cHg7IH1cXG4udWktaWNvbi1ib29rbWFyayB7IGJhY2tncm91bmQtcG9zaXRpb246IC0yMjRweCAtOTZweDsgfVxcbi51aS1pY29uLXRhZyB7IGJhY2tncm91bmQtcG9zaXRpb246IC0yNDBweCAtOTZweDsgfVxcbi51aS1pY29uLWhvbWUgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAwIC0xMTJweDsgfVxcbi51aS1pY29uLWZsYWcgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTZweCAtMTEycHg7IH1cXG4udWktaWNvbi1jYWxlbmRhciB7IGJhY2tncm91bmQtcG9zaXRpb246IC0zMnB4IC0xMTJweDsgfVxcbi51aS1pY29uLWNhcnQgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtNDhweCAtMTEycHg7IH1cXG4udWktaWNvbi1wZW5jaWwgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtNjRweCAtMTEycHg7IH1cXG4udWktaWNvbi1jbG9jayB7IGJhY2tncm91bmQtcG9zaXRpb246IC04MHB4IC0xMTJweDsgfVxcbi51aS1pY29uLWRpc2sgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtOTZweCAtMTEycHg7IH1cXG4udWktaWNvbi1jYWxjdWxhdG9yIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTExMnB4IC0xMTJweDsgfVxcbi51aS1pY29uLXpvb21pbiB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xMjhweCAtMTEycHg7IH1cXG4udWktaWNvbi16b29tb3V0IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE0NHB4IC0xMTJweDsgfVxcbi51aS1pY29uLXNlYXJjaCB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xNjBweCAtMTEycHg7IH1cXG4udWktaWNvbi13cmVuY2ggeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTc2cHggLTExMnB4OyB9XFxuLnVpLWljb24tZ2VhciB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xOTJweCAtMTEycHg7IH1cXG4udWktaWNvbi1oZWFydCB7IGJhY2tncm91bmQtcG9zaXRpb246IC0yMDhweCAtMTEycHg7IH1cXG4udWktaWNvbi1zdGFyIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTIyNHB4IC0xMTJweDsgfVxcbi51aS1pY29uLWxpbmsgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMjQwcHggLTExMnB4OyB9XFxuLnVpLWljb24tY2FuY2VsIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogMCAtMTI4cHg7IH1cXG4udWktaWNvbi1wbHVzIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE2cHggLTEyOHB4OyB9XFxuLnVpLWljb24tcGx1c3RoaWNrIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTMycHggLTEyOHB4OyB9XFxuLnVpLWljb24tbWludXMgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtNDhweCAtMTI4cHg7IH1cXG4udWktaWNvbi1taW51c3RoaWNrIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTY0cHggLTEyOHB4OyB9XFxuLnVpLWljb24tY2xvc2UgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtODBweCAtMTI4cHg7IH1cXG4udWktaWNvbi1jbG9zZXRoaWNrIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTk2cHggLTEyOHB4OyB9XFxuLnVpLWljb24ta2V5IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTExMnB4IC0xMjhweDsgfVxcbi51aS1pY29uLWxpZ2h0YnVsYiB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xMjhweCAtMTI4cHg7IH1cXG4udWktaWNvbi1zY2lzc29ycyB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xNDRweCAtMTI4cHg7IH1cXG4udWktaWNvbi1jbGlwYm9hcmQgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTYwcHggLTEyOHB4OyB9XFxuLnVpLWljb24tY29weSB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xNzZweCAtMTI4cHg7IH1cXG4udWktaWNvbi1jb250YWN0IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE5MnB4IC0xMjhweDsgfVxcbi51aS1pY29uLWltYWdlIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTIwOHB4IC0xMjhweDsgfVxcbi51aS1pY29uLXZpZGVvIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTIyNHB4IC0xMjhweDsgfVxcbi51aS1pY29uLXNjcmlwdCB7IGJhY2tncm91bmQtcG9zaXRpb246IC0yNDBweCAtMTI4cHg7IH1cXG4udWktaWNvbi1hbGVydCB7IGJhY2tncm91bmQtcG9zaXRpb246IDAgLTE0NHB4OyB9XFxuLnVpLWljb24taW5mbyB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xNnB4IC0xNDRweDsgfVxcbi51aS1pY29uLW5vdGljZSB7IGJhY2tncm91bmQtcG9zaXRpb246IC0zMnB4IC0xNDRweDsgfVxcbi51aS1pY29uLWhlbHAgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtNDhweCAtMTQ0cHg7IH1cXG4udWktaWNvbi1jaGVjayB7IGJhY2tncm91bmQtcG9zaXRpb246IC02NHB4IC0xNDRweDsgfVxcbi51aS1pY29uLWJ1bGxldCB7IGJhY2tncm91bmQtcG9zaXRpb246IC04MHB4IC0xNDRweDsgfVxcbi51aS1pY29uLXJhZGlvLW9uIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTk2cHggLTE0NHB4OyB9XFxuLnVpLWljb24tcmFkaW8tb2ZmIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTExMnB4IC0xNDRweDsgfVxcbi51aS1pY29uLXBpbi13IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTEyOHB4IC0xNDRweDsgfVxcbi51aS1pY29uLXBpbi1zIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE0NHB4IC0xNDRweDsgfVxcbi51aS1pY29uLXBsYXkgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAwIC0xNjBweDsgfVxcbi51aS1pY29uLXBhdXNlIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE2cHggLTE2MHB4OyB9XFxuLnVpLWljb24tc2Vlay1uZXh0IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTMycHggLTE2MHB4OyB9XFxuLnVpLWljb24tc2Vlay1wcmV2IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTQ4cHggLTE2MHB4OyB9XFxuLnVpLWljb24tc2Vlay1lbmQgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtNjRweCAtMTYwcHg7IH1cXG4udWktaWNvbi1zZWVrLXN0YXJ0IHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTgwcHggLTE2MHB4OyB9XFxuLyogdWktaWNvbi1zZWVrLWZpcnN0IGlzIGRlcHJlY2F0ZWQsIHVzZSB1aS1pY29uLXNlZWstc3RhcnQgaW5zdGVhZCAqL1xcbi51aS1pY29uLXNlZWstZmlyc3QgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtODBweCAtMTYwcHg7IH1cXG4udWktaWNvbi1zdG9wIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTk2cHggLTE2MHB4OyB9XFxuLnVpLWljb24tZWplY3QgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTEycHggLTE2MHB4OyB9XFxuLnVpLWljb24tdm9sdW1lLW9mZiB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xMjhweCAtMTYwcHg7IH1cXG4udWktaWNvbi12b2x1bWUtb24geyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTQ0cHggLTE2MHB4OyB9XFxuLnVpLWljb24tcG93ZXIgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAwIC0xNzZweDsgfVxcbi51aS1pY29uLXNpZ25hbC1kaWFnIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTE2cHggLTE3NnB4OyB9XFxuLnVpLWljb24tc2lnbmFsIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTMycHggLTE3NnB4OyB9XFxuLnVpLWljb24tYmF0dGVyeS0wIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTQ4cHggLTE3NnB4OyB9XFxuLnVpLWljb24tYmF0dGVyeS0xIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTY0cHggLTE3NnB4OyB9XFxuLnVpLWljb24tYmF0dGVyeS0yIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTgwcHggLTE3NnB4OyB9XFxuLnVpLWljb24tYmF0dGVyeS0zIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTk2cHggLTE3NnB4OyB9XFxuLnVpLWljb24tY2lyY2xlLXBsdXMgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAwIC0xOTJweDsgfVxcbi51aS1pY29uLWNpcmNsZS1taW51cyB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xNnB4IC0xOTJweDsgfVxcbi51aS1pY29uLWNpcmNsZS1jbG9zZSB7IGJhY2tncm91bmQtcG9zaXRpb246IC0zMnB4IC0xOTJweDsgfVxcbi51aS1pY29uLWNpcmNsZS10cmlhbmdsZS1lIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTQ4cHggLTE5MnB4OyB9XFxuLnVpLWljb24tY2lyY2xlLXRyaWFuZ2xlLXMgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtNjRweCAtMTkycHg7IH1cXG4udWktaWNvbi1jaXJjbGUtdHJpYW5nbGUtdyB7IGJhY2tncm91bmQtcG9zaXRpb246IC04MHB4IC0xOTJweDsgfVxcbi51aS1pY29uLWNpcmNsZS10cmlhbmdsZS1uIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTk2cHggLTE5MnB4OyB9XFxuLnVpLWljb24tY2lyY2xlLWFycm93LWUgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTEycHggLTE5MnB4OyB9XFxuLnVpLWljb24tY2lyY2xlLWFycm93LXMgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTI4cHggLTE5MnB4OyB9XFxuLnVpLWljb24tY2lyY2xlLWFycm93LXcgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTQ0cHggLTE5MnB4OyB9XFxuLnVpLWljb24tY2lyY2xlLWFycm93LW4geyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTYwcHggLTE5MnB4OyB9XFxuLnVpLWljb24tY2lyY2xlLXpvb21pbiB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xNzZweCAtMTkycHg7IH1cXG4udWktaWNvbi1jaXJjbGUtem9vbW91dCB7IGJhY2tncm91bmQtcG9zaXRpb246IC0xOTJweCAtMTkycHg7IH1cXG4udWktaWNvbi1jaXJjbGUtY2hlY2sgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMjA4cHggLTE5MnB4OyB9XFxuLnVpLWljb24tY2lyY2xlc21hbGwtcGx1cyB7IGJhY2tncm91bmQtcG9zaXRpb246IDAgLTIwOHB4OyB9XFxuLnVpLWljb24tY2lyY2xlc21hbGwtbWludXMgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTZweCAtMjA4cHg7IH1cXG4udWktaWNvbi1jaXJjbGVzbWFsbC1jbG9zZSB7IGJhY2tncm91bmQtcG9zaXRpb246IC0zMnB4IC0yMDhweDsgfVxcbi51aS1pY29uLXNxdWFyZXNtYWxsLXBsdXMgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtNDhweCAtMjA4cHg7IH1cXG4udWktaWNvbi1zcXVhcmVzbWFsbC1taW51cyB7IGJhY2tncm91bmQtcG9zaXRpb246IC02NHB4IC0yMDhweDsgfVxcbi51aS1pY29uLXNxdWFyZXNtYWxsLWNsb3NlIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTgwcHggLTIwOHB4OyB9XFxuLnVpLWljb24tZ3JpcC1kb3R0ZWQtdmVydGljYWwgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAwIC0yMjRweDsgfVxcbi51aS1pY29uLWdyaXAtZG90dGVkLWhvcml6b250YWwgeyBiYWNrZ3JvdW5kLXBvc2l0aW9uOiAtMTZweCAtMjI0cHg7IH1cXG4udWktaWNvbi1ncmlwLXNvbGlkLXZlcnRpY2FsIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTMycHggLTIyNHB4OyB9XFxuLnVpLWljb24tZ3JpcC1zb2xpZC1ob3Jpem9udGFsIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTQ4cHggLTIyNHB4OyB9XFxuLnVpLWljb24tZ3JpcHNtYWxsLWRpYWdvbmFsLXNlIHsgYmFja2dyb3VuZC1wb3NpdGlvbjogLTY0cHggLTIyNHB4OyB9XFxuLnVpLWljb24tZ3JpcC1kaWFnb25hbC1zZSB7IGJhY2tncm91bmQtcG9zaXRpb246IC04MHB4IC0yMjRweDsgfVxcblxcblxcbi8qIE1pc2MgdmlzdWFsc1xcbi0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0qL1xcblxcbi8qIENvcm5lciByYWRpdXMgKi9cXG4udWktY29ybmVyLWFsbCxcXG4udWktY29ybmVyLXRvcCxcXG4udWktY29ybmVyLWxlZnQsXFxuLnVpLWNvcm5lci10bCB7XFxuXFx0Ym9yZGVyLXRvcC1sZWZ0LXJhZGl1czogM3B4O1xcbn1cXG4udWktY29ybmVyLWFsbCxcXG4udWktY29ybmVyLXRvcCxcXG4udWktY29ybmVyLXJpZ2h0LFxcbi51aS1jb3JuZXItdHIge1xcblxcdGJvcmRlci10b3AtcmlnaHQtcmFkaXVzOiAzcHg7XFxufVxcbi51aS1jb3JuZXItYWxsLFxcbi51aS1jb3JuZXItYm90dG9tLFxcbi51aS1jb3JuZXItbGVmdCxcXG4udWktY29ybmVyLWJsIHtcXG5cXHRib3JkZXItYm90dG9tLWxlZnQtcmFkaXVzOiAzcHg7XFxufVxcbi51aS1jb3JuZXItYWxsLFxcbi51aS1jb3JuZXItYm90dG9tLFxcbi51aS1jb3JuZXItcmlnaHQsXFxuLnVpLWNvcm5lci1iciB7XFxuXFx0Ym9yZGVyLWJvdHRvbS1yaWdodC1yYWRpdXM6IDNweDtcXG59XFxuXFxuLyogT3ZlcmxheXMgKi9cXG4udWktd2lkZ2V0LW92ZXJsYXkge1xcblxcdGJhY2tncm91bmQ6ICNhYWFhYWE7XFxuXFx0b3BhY2l0eTogLjM7XFxuXFx0ZmlsdGVyOiBBbHBoYShPcGFjaXR5PTMwKTsgLyogc3VwcG9ydDogSUU4ICovXFxufVxcbi51aS13aWRnZXQtc2hhZG93IHtcXG5cXHQtd2Via2l0LWJveC1zaGFkb3c6IDBweCAwcHggNXB4ICM2NjY2NjY7XFxuXFx0Ym94LXNoYWRvdzogMHB4IDBweCA1cHggIzY2NjY2NjtcXG59XFxuXCIsIFwiXCJdKTtcblxuLy8gZXhwb3J0c1xuIiwiLypcblx0TUlUIExpY2Vuc2UgaHR0cDovL3d3dy5vcGVuc291cmNlLm9yZy9saWNlbnNlcy9taXQtbGljZW5zZS5waHBcblx0QXV0aG9yIFRvYmlhcyBLb3BwZXJzIEBzb2tyYVxuKi9cbi8vIGNzcyBiYXNlIGNvZGUsIGluamVjdGVkIGJ5IHRoZSBjc3MtbG9hZGVyXG5tb2R1bGUuZXhwb3J0cyA9IGZ1bmN0aW9uKHVzZVNvdXJjZU1hcCkge1xuXHR2YXIgbGlzdCA9IFtdO1xuXG5cdC8vIHJldHVybiB0aGUgbGlzdCBvZiBtb2R1bGVzIGFzIGNzcyBzdHJpbmdcblx0bGlzdC50b1N0cmluZyA9IGZ1bmN0aW9uIHRvU3RyaW5nKCkge1xuXHRcdHJldHVybiB0aGlzLm1hcChmdW5jdGlvbiAoaXRlbSkge1xuXHRcdFx0dmFyIGNvbnRlbnQgPSBjc3NXaXRoTWFwcGluZ1RvU3RyaW5nKGl0ZW0sIHVzZVNvdXJjZU1hcCk7XG5cdFx0XHRpZihpdGVtWzJdKSB7XG5cdFx0XHRcdHJldHVybiBcIkBtZWRpYSBcIiArIGl0ZW1bMl0gKyBcIntcIiArIGNvbnRlbnQgKyBcIn1cIjtcblx0XHRcdH0gZWxzZSB7XG5cdFx0XHRcdHJldHVybiBjb250ZW50O1xuXHRcdFx0fVxuXHRcdH0pLmpvaW4oXCJcIik7XG5cdH07XG5cblx0Ly8gaW1wb3J0IGEgbGlzdCBvZiBtb2R1bGVzIGludG8gdGhlIGxpc3Rcblx0bGlzdC5pID0gZnVuY3Rpb24obW9kdWxlcywgbWVkaWFRdWVyeSkge1xuXHRcdGlmKHR5cGVvZiBtb2R1bGVzID09PSBcInN0cmluZ1wiKVxuXHRcdFx0bW9kdWxlcyA9IFtbbnVsbCwgbW9kdWxlcywgXCJcIl1dO1xuXHRcdHZhciBhbHJlYWR5SW1wb3J0ZWRNb2R1bGVzID0ge307XG5cdFx0Zm9yKHZhciBpID0gMDsgaSA8IHRoaXMubGVuZ3RoOyBpKyspIHtcblx0XHRcdHZhciBpZCA9IHRoaXNbaV1bMF07XG5cdFx0XHRpZih0eXBlb2YgaWQgPT09IFwibnVtYmVyXCIpXG5cdFx0XHRcdGFscmVhZHlJbXBvcnRlZE1vZHVsZXNbaWRdID0gdHJ1ZTtcblx0XHR9XG5cdFx0Zm9yKGkgPSAwOyBpIDwgbW9kdWxlcy5sZW5ndGg7IGkrKykge1xuXHRcdFx0dmFyIGl0ZW0gPSBtb2R1bGVzW2ldO1xuXHRcdFx0Ly8gc2tpcCBhbHJlYWR5IGltcG9ydGVkIG1vZHVsZVxuXHRcdFx0Ly8gdGhpcyBpbXBsZW1lbnRhdGlvbiBpcyBub3QgMTAwJSBwZXJmZWN0IGZvciB3ZWlyZCBtZWRpYSBxdWVyeSBjb21iaW5hdGlvbnNcblx0XHRcdC8vICB3aGVuIGEgbW9kdWxlIGlzIGltcG9ydGVkIG11bHRpcGxlIHRpbWVzIHdpdGggZGlmZmVyZW50IG1lZGlhIHF1ZXJpZXMuXG5cdFx0XHQvLyAgSSBob3BlIHRoaXMgd2lsbCBuZXZlciBvY2N1ciAoSGV5IHRoaXMgd2F5IHdlIGhhdmUgc21hbGxlciBidW5kbGVzKVxuXHRcdFx0aWYodHlwZW9mIGl0ZW1bMF0gIT09IFwibnVtYmVyXCIgfHwgIWFscmVhZHlJbXBvcnRlZE1vZHVsZXNbaXRlbVswXV0pIHtcblx0XHRcdFx0aWYobWVkaWFRdWVyeSAmJiAhaXRlbVsyXSkge1xuXHRcdFx0XHRcdGl0ZW1bMl0gPSBtZWRpYVF1ZXJ5O1xuXHRcdFx0XHR9IGVsc2UgaWYobWVkaWFRdWVyeSkge1xuXHRcdFx0XHRcdGl0ZW1bMl0gPSBcIihcIiArIGl0ZW1bMl0gKyBcIikgYW5kIChcIiArIG1lZGlhUXVlcnkgKyBcIilcIjtcblx0XHRcdFx0fVxuXHRcdFx0XHRsaXN0LnB1c2goaXRlbSk7XG5cdFx0XHR9XG5cdFx0fVxuXHR9O1xuXHRyZXR1cm4gbGlzdDtcbn07XG5cbmZ1bmN0aW9uIGNzc1dpdGhNYXBwaW5nVG9TdHJpbmcoaXRlbSwgdXNlU291cmNlTWFwKSB7XG5cdHZhciBjb250ZW50ID0gaXRlbVsxXSB8fCAnJztcblx0dmFyIGNzc01hcHBpbmcgPSBpdGVtWzNdO1xuXHRpZiAoIWNzc01hcHBpbmcpIHtcblx0XHRyZXR1cm4gY29udGVudDtcblx0fVxuXG5cdGlmICh1c2VTb3VyY2VNYXAgJiYgdHlwZW9mIGJ0b2EgPT09ICdmdW5jdGlvbicpIHtcblx0XHR2YXIgc291cmNlTWFwcGluZyA9IHRvQ29tbWVudChjc3NNYXBwaW5nKTtcblx0XHR2YXIgc291cmNlVVJMcyA9IGNzc01hcHBpbmcuc291cmNlcy5tYXAoZnVuY3Rpb24gKHNvdXJjZSkge1xuXHRcdFx0cmV0dXJuICcvKiMgc291cmNlVVJMPScgKyBjc3NNYXBwaW5nLnNvdXJjZVJvb3QgKyBzb3VyY2UgKyAnICovJ1xuXHRcdH0pO1xuXG5cdFx0cmV0dXJuIFtjb250ZW50XS5jb25jYXQoc291cmNlVVJMcykuY29uY2F0KFtzb3VyY2VNYXBwaW5nXSkuam9pbignXFxuJyk7XG5cdH1cblxuXHRyZXR1cm4gW2NvbnRlbnRdLmpvaW4oJ1xcbicpO1xufVxuXG4vLyBBZGFwdGVkIGZyb20gY29udmVydC1zb3VyY2UtbWFwIChNSVQpXG5mdW5jdGlvbiB0b0NvbW1lbnQoc291cmNlTWFwKSB7XG5cdC8vIGVzbGludC1kaXNhYmxlLW5leHQtbGluZSBuby11bmRlZlxuXHR2YXIgYmFzZTY0ID0gYnRvYSh1bmVzY2FwZShlbmNvZGVVUklDb21wb25lbnQoSlNPTi5zdHJpbmdpZnkoc291cmNlTWFwKSkpKTtcblx0dmFyIGRhdGEgPSAnc291cmNlTWFwcGluZ1VSTD1kYXRhOmFwcGxpY2F0aW9uL2pzb247Y2hhcnNldD11dGYtODtiYXNlNjQsJyArIGJhc2U2NDtcblxuXHRyZXR1cm4gJy8qIyAnICsgZGF0YSArICcgKi8nO1xufVxuIiwibW9kdWxlLmV4cG9ydHMgPSBmdW5jdGlvbiBlc2NhcGUodXJsKSB7XG4gICAgaWYgKHR5cGVvZiB1cmwgIT09ICdzdHJpbmcnKSB7XG4gICAgICAgIHJldHVybiB1cmxcbiAgICB9XG4gICAgLy8gSWYgdXJsIGlzIGFscmVhZHkgd3JhcHBlZCBpbiBxdW90ZXMsIHJlbW92ZSB0aGVtXG4gICAgaWYgKC9eWydcIl0uKlsnXCJdJC8udGVzdCh1cmwpKSB7XG4gICAgICAgIHVybCA9IHVybC5zbGljZSgxLCAtMSk7XG4gICAgfVxuICAgIC8vIFNob3VsZCB1cmwgYmUgd3JhcHBlZD9cbiAgICAvLyBTZWUgaHR0cHM6Ly9kcmFmdHMuY3Nzd2cub3JnL2Nzcy12YWx1ZXMtMy8jdXJsc1xuICAgIGlmICgvW1wiJygpIFxcdFxcbl0vLnRlc3QodXJsKSkge1xuICAgICAgICByZXR1cm4gJ1wiJyArIHVybC5yZXBsYWNlKC9cIi9nLCAnXFxcXFwiJykucmVwbGFjZSgvXFxuL2csICdcXFxcbicpICsgJ1wiJ1xuICAgIH1cblxuICAgIHJldHVybiB1cmxcbn1cbiIsIi8qXG5cdE1JVCBMaWNlbnNlIGh0dHA6Ly93d3cub3BlbnNvdXJjZS5vcmcvbGljZW5zZXMvbWl0LWxpY2Vuc2UucGhwXG5cdEF1dGhvciBUb2JpYXMgS29wcGVycyBAc29rcmFcbiovXG52YXIgc3R5bGVzSW5Eb20gPSB7fSxcblx0bWVtb2l6ZSA9IGZ1bmN0aW9uKGZuKSB7XG5cdFx0dmFyIG1lbW87XG5cdFx0cmV0dXJuIGZ1bmN0aW9uICgpIHtcblx0XHRcdGlmICh0eXBlb2YgbWVtbyA9PT0gXCJ1bmRlZmluZWRcIikgbWVtbyA9IGZuLmFwcGx5KHRoaXMsIGFyZ3VtZW50cyk7XG5cdFx0XHRyZXR1cm4gbWVtbztcblx0XHR9O1xuXHR9LFxuXHRpc09sZElFID0gbWVtb2l6ZShmdW5jdGlvbigpIHtcblx0XHRyZXR1cm4gL21zaWUgWzYtOV1cXGIvLnRlc3Qoc2VsZi5uYXZpZ2F0b3IudXNlckFnZW50LnRvTG93ZXJDYXNlKCkpO1xuXHR9KSxcblx0Z2V0SGVhZEVsZW1lbnQgPSBtZW1vaXplKGZ1bmN0aW9uICgpIHtcblx0XHRyZXR1cm4gZG9jdW1lbnQuaGVhZCB8fCBkb2N1bWVudC5nZXRFbGVtZW50c0J5VGFnTmFtZShcImhlYWRcIilbMF07XG5cdH0pLFxuXHRzaW5nbGV0b25FbGVtZW50ID0gbnVsbCxcblx0c2luZ2xldG9uQ291bnRlciA9IDAsXG5cdHN0eWxlRWxlbWVudHNJbnNlcnRlZEF0VG9wID0gW107XG5cbm1vZHVsZS5leHBvcnRzID0gZnVuY3Rpb24obGlzdCwgb3B0aW9ucykge1xuXHRpZih0eXBlb2YgREVCVUcgIT09IFwidW5kZWZpbmVkXCIgJiYgREVCVUcpIHtcblx0XHRpZih0eXBlb2YgZG9jdW1lbnQgIT09IFwib2JqZWN0XCIpIHRocm93IG5ldyBFcnJvcihcIlRoZSBzdHlsZS1sb2FkZXIgY2Fubm90IGJlIHVzZWQgaW4gYSBub24tYnJvd3NlciBlbnZpcm9ubWVudFwiKTtcblx0fVxuXG5cdG9wdGlvbnMgPSBvcHRpb25zIHx8IHt9O1xuXHQvLyBGb3JjZSBzaW5nbGUtdGFnIHNvbHV0aW9uIG9uIElFNi05LCB3aGljaCBoYXMgYSBoYXJkIGxpbWl0IG9uIHRoZSAjIG9mIDxzdHlsZT5cblx0Ly8gdGFncyBpdCB3aWxsIGFsbG93IG9uIGEgcGFnZVxuXHRpZiAodHlwZW9mIG9wdGlvbnMuc2luZ2xldG9uID09PSBcInVuZGVmaW5lZFwiKSBvcHRpb25zLnNpbmdsZXRvbiA9IGlzT2xkSUUoKTtcblxuXHQvLyBCeSBkZWZhdWx0LCBhZGQgPHN0eWxlPiB0YWdzIHRvIHRoZSBib3R0b20gb2YgPGhlYWQ+LlxuXHRpZiAodHlwZW9mIG9wdGlvbnMuaW5zZXJ0QXQgPT09IFwidW5kZWZpbmVkXCIpIG9wdGlvbnMuaW5zZXJ0QXQgPSBcImJvdHRvbVwiO1xuXG5cdHZhciBzdHlsZXMgPSBsaXN0VG9TdHlsZXMobGlzdCk7XG5cdGFkZFN0eWxlc1RvRG9tKHN0eWxlcywgb3B0aW9ucyk7XG5cblx0cmV0dXJuIGZ1bmN0aW9uIHVwZGF0ZShuZXdMaXN0KSB7XG5cdFx0dmFyIG1heVJlbW92ZSA9IFtdO1xuXHRcdGZvcih2YXIgaSA9IDA7IGkgPCBzdHlsZXMubGVuZ3RoOyBpKyspIHtcblx0XHRcdHZhciBpdGVtID0gc3R5bGVzW2ldO1xuXHRcdFx0dmFyIGRvbVN0eWxlID0gc3R5bGVzSW5Eb21baXRlbS5pZF07XG5cdFx0XHRkb21TdHlsZS5yZWZzLS07XG5cdFx0XHRtYXlSZW1vdmUucHVzaChkb21TdHlsZSk7XG5cdFx0fVxuXHRcdGlmKG5ld0xpc3QpIHtcblx0XHRcdHZhciBuZXdTdHlsZXMgPSBsaXN0VG9TdHlsZXMobmV3TGlzdCk7XG5cdFx0XHRhZGRTdHlsZXNUb0RvbShuZXdTdHlsZXMsIG9wdGlvbnMpO1xuXHRcdH1cblx0XHRmb3IodmFyIGkgPSAwOyBpIDwgbWF5UmVtb3ZlLmxlbmd0aDsgaSsrKSB7XG5cdFx0XHR2YXIgZG9tU3R5bGUgPSBtYXlSZW1vdmVbaV07XG5cdFx0XHRpZihkb21TdHlsZS5yZWZzID09PSAwKSB7XG5cdFx0XHRcdGZvcih2YXIgaiA9IDA7IGogPCBkb21TdHlsZS5wYXJ0cy5sZW5ndGg7IGorKylcblx0XHRcdFx0XHRkb21TdHlsZS5wYXJ0c1tqXSgpO1xuXHRcdFx0XHRkZWxldGUgc3R5bGVzSW5Eb21bZG9tU3R5bGUuaWRdO1xuXHRcdFx0fVxuXHRcdH1cblx0fTtcbn1cblxuZnVuY3Rpb24gYWRkU3R5bGVzVG9Eb20oc3R5bGVzLCBvcHRpb25zKSB7XG5cdGZvcih2YXIgaSA9IDA7IGkgPCBzdHlsZXMubGVuZ3RoOyBpKyspIHtcblx0XHR2YXIgaXRlbSA9IHN0eWxlc1tpXTtcblx0XHR2YXIgZG9tU3R5bGUgPSBzdHlsZXNJbkRvbVtpdGVtLmlkXTtcblx0XHRpZihkb21TdHlsZSkge1xuXHRcdFx0ZG9tU3R5bGUucmVmcysrO1xuXHRcdFx0Zm9yKHZhciBqID0gMDsgaiA8IGRvbVN0eWxlLnBhcnRzLmxlbmd0aDsgaisrKSB7XG5cdFx0XHRcdGRvbVN0eWxlLnBhcnRzW2pdKGl0ZW0ucGFydHNbal0pO1xuXHRcdFx0fVxuXHRcdFx0Zm9yKDsgaiA8IGl0ZW0ucGFydHMubGVuZ3RoOyBqKyspIHtcblx0XHRcdFx0ZG9tU3R5bGUucGFydHMucHVzaChhZGRTdHlsZShpdGVtLnBhcnRzW2pdLCBvcHRpb25zKSk7XG5cdFx0XHR9XG5cdFx0fSBlbHNlIHtcblx0XHRcdHZhciBwYXJ0cyA9IFtdO1xuXHRcdFx0Zm9yKHZhciBqID0gMDsgaiA8IGl0ZW0ucGFydHMubGVuZ3RoOyBqKyspIHtcblx0XHRcdFx0cGFydHMucHVzaChhZGRTdHlsZShpdGVtLnBhcnRzW2pdLCBvcHRpb25zKSk7XG5cdFx0XHR9XG5cdFx0XHRzdHlsZXNJbkRvbVtpdGVtLmlkXSA9IHtpZDogaXRlbS5pZCwgcmVmczogMSwgcGFydHM6IHBhcnRzfTtcblx0XHR9XG5cdH1cbn1cblxuZnVuY3Rpb24gbGlzdFRvU3R5bGVzKGxpc3QpIHtcblx0dmFyIHN0eWxlcyA9IFtdO1xuXHR2YXIgbmV3U3R5bGVzID0ge307XG5cdGZvcih2YXIgaSA9IDA7IGkgPCBsaXN0Lmxlbmd0aDsgaSsrKSB7XG5cdFx0dmFyIGl0ZW0gPSBsaXN0W2ldO1xuXHRcdHZhciBpZCA9IGl0ZW1bMF07XG5cdFx0dmFyIGNzcyA9IGl0ZW1bMV07XG5cdFx0dmFyIG1lZGlhID0gaXRlbVsyXTtcblx0XHR2YXIgc291cmNlTWFwID0gaXRlbVszXTtcblx0XHR2YXIgcGFydCA9IHtjc3M6IGNzcywgbWVkaWE6IG1lZGlhLCBzb3VyY2VNYXA6IHNvdXJjZU1hcH07XG5cdFx0aWYoIW5ld1N0eWxlc1tpZF0pXG5cdFx0XHRzdHlsZXMucHVzaChuZXdTdHlsZXNbaWRdID0ge2lkOiBpZCwgcGFydHM6IFtwYXJ0XX0pO1xuXHRcdGVsc2Vcblx0XHRcdG5ld1N0eWxlc1tpZF0ucGFydHMucHVzaChwYXJ0KTtcblx0fVxuXHRyZXR1cm4gc3R5bGVzO1xufVxuXG5mdW5jdGlvbiBpbnNlcnRTdHlsZUVsZW1lbnQob3B0aW9ucywgc3R5bGVFbGVtZW50KSB7XG5cdHZhciBoZWFkID0gZ2V0SGVhZEVsZW1lbnQoKTtcblx0dmFyIGxhc3RTdHlsZUVsZW1lbnRJbnNlcnRlZEF0VG9wID0gc3R5bGVFbGVtZW50c0luc2VydGVkQXRUb3Bbc3R5bGVFbGVtZW50c0luc2VydGVkQXRUb3AubGVuZ3RoIC0gMV07XG5cdGlmIChvcHRpb25zLmluc2VydEF0ID09PSBcInRvcFwiKSB7XG5cdFx0aWYoIWxhc3RTdHlsZUVsZW1lbnRJbnNlcnRlZEF0VG9wKSB7XG5cdFx0XHRoZWFkLmluc2VydEJlZm9yZShzdHlsZUVsZW1lbnQsIGhlYWQuZmlyc3RDaGlsZCk7XG5cdFx0fSBlbHNlIGlmKGxhc3RTdHlsZUVsZW1lbnRJbnNlcnRlZEF0VG9wLm5leHRTaWJsaW5nKSB7XG5cdFx0XHRoZWFkLmluc2VydEJlZm9yZShzdHlsZUVsZW1lbnQsIGxhc3RTdHlsZUVsZW1lbnRJbnNlcnRlZEF0VG9wLm5leHRTaWJsaW5nKTtcblx0XHR9IGVsc2Uge1xuXHRcdFx0aGVhZC5hcHBlbmRDaGlsZChzdHlsZUVsZW1lbnQpO1xuXHRcdH1cblx0XHRzdHlsZUVsZW1lbnRzSW5zZXJ0ZWRBdFRvcC5wdXNoKHN0eWxlRWxlbWVudCk7XG5cdH0gZWxzZSBpZiAob3B0aW9ucy5pbnNlcnRBdCA9PT0gXCJib3R0b21cIikge1xuXHRcdGhlYWQuYXBwZW5kQ2hpbGQoc3R5bGVFbGVtZW50KTtcblx0fSBlbHNlIHtcblx0XHR0aHJvdyBuZXcgRXJyb3IoXCJJbnZhbGlkIHZhbHVlIGZvciBwYXJhbWV0ZXIgJ2luc2VydEF0Jy4gTXVzdCBiZSAndG9wJyBvciAnYm90dG9tJy5cIik7XG5cdH1cbn1cblxuZnVuY3Rpb24gcmVtb3ZlU3R5bGVFbGVtZW50KHN0eWxlRWxlbWVudCkge1xuXHRzdHlsZUVsZW1lbnQucGFyZW50Tm9kZS5yZW1vdmVDaGlsZChzdHlsZUVsZW1lbnQpO1xuXHR2YXIgaWR4ID0gc3R5bGVFbGVtZW50c0luc2VydGVkQXRUb3AuaW5kZXhPZihzdHlsZUVsZW1lbnQpO1xuXHRpZihpZHggPj0gMCkge1xuXHRcdHN0eWxlRWxlbWVudHNJbnNlcnRlZEF0VG9wLnNwbGljZShpZHgsIDEpO1xuXHR9XG59XG5cbmZ1bmN0aW9uIGNyZWF0ZVN0eWxlRWxlbWVudChvcHRpb25zKSB7XG5cdHZhciBzdHlsZUVsZW1lbnQgPSBkb2N1bWVudC5jcmVhdGVFbGVtZW50KFwic3R5bGVcIik7XG5cdHN0eWxlRWxlbWVudC50eXBlID0gXCJ0ZXh0L2Nzc1wiO1xuXHRpbnNlcnRTdHlsZUVsZW1lbnQob3B0aW9ucywgc3R5bGVFbGVtZW50KTtcblx0cmV0dXJuIHN0eWxlRWxlbWVudDtcbn1cblxuZnVuY3Rpb24gY3JlYXRlTGlua0VsZW1lbnQob3B0aW9ucykge1xuXHR2YXIgbGlua0VsZW1lbnQgPSBkb2N1bWVudC5jcmVhdGVFbGVtZW50KFwibGlua1wiKTtcblx0bGlua0VsZW1lbnQucmVsID0gXCJzdHlsZXNoZWV0XCI7XG5cdGluc2VydFN0eWxlRWxlbWVudChvcHRpb25zLCBsaW5rRWxlbWVudCk7XG5cdHJldHVybiBsaW5rRWxlbWVudDtcbn1cblxuZnVuY3Rpb24gYWRkU3R5bGUob2JqLCBvcHRpb25zKSB7XG5cdHZhciBzdHlsZUVsZW1lbnQsIHVwZGF0ZSwgcmVtb3ZlO1xuXG5cdGlmIChvcHRpb25zLnNpbmdsZXRvbikge1xuXHRcdHZhciBzdHlsZUluZGV4ID0gc2luZ2xldG9uQ291bnRlcisrO1xuXHRcdHN0eWxlRWxlbWVudCA9IHNpbmdsZXRvbkVsZW1lbnQgfHwgKHNpbmdsZXRvbkVsZW1lbnQgPSBjcmVhdGVTdHlsZUVsZW1lbnQob3B0aW9ucykpO1xuXHRcdHVwZGF0ZSA9IGFwcGx5VG9TaW5nbGV0b25UYWcuYmluZChudWxsLCBzdHlsZUVsZW1lbnQsIHN0eWxlSW5kZXgsIGZhbHNlKTtcblx0XHRyZW1vdmUgPSBhcHBseVRvU2luZ2xldG9uVGFnLmJpbmQobnVsbCwgc3R5bGVFbGVtZW50LCBzdHlsZUluZGV4LCB0cnVlKTtcblx0fSBlbHNlIGlmKG9iai5zb3VyY2VNYXAgJiZcblx0XHR0eXBlb2YgVVJMID09PSBcImZ1bmN0aW9uXCIgJiZcblx0XHR0eXBlb2YgVVJMLmNyZWF0ZU9iamVjdFVSTCA9PT0gXCJmdW5jdGlvblwiICYmXG5cdFx0dHlwZW9mIFVSTC5yZXZva2VPYmplY3RVUkwgPT09IFwiZnVuY3Rpb25cIiAmJlxuXHRcdHR5cGVvZiBCbG9iID09PSBcImZ1bmN0aW9uXCIgJiZcblx0XHR0eXBlb2YgYnRvYSA9PT0gXCJmdW5jdGlvblwiKSB7XG5cdFx0c3R5bGVFbGVtZW50ID0gY3JlYXRlTGlua0VsZW1lbnQob3B0aW9ucyk7XG5cdFx0dXBkYXRlID0gdXBkYXRlTGluay5iaW5kKG51bGwsIHN0eWxlRWxlbWVudCk7XG5cdFx0cmVtb3ZlID0gZnVuY3Rpb24oKSB7XG5cdFx0XHRyZW1vdmVTdHlsZUVsZW1lbnQoc3R5bGVFbGVtZW50KTtcblx0XHRcdGlmKHN0eWxlRWxlbWVudC5ocmVmKVxuXHRcdFx0XHRVUkwucmV2b2tlT2JqZWN0VVJMKHN0eWxlRWxlbWVudC5ocmVmKTtcblx0XHR9O1xuXHR9IGVsc2Uge1xuXHRcdHN0eWxlRWxlbWVudCA9IGNyZWF0ZVN0eWxlRWxlbWVudChvcHRpb25zKTtcblx0XHR1cGRhdGUgPSBhcHBseVRvVGFnLmJpbmQobnVsbCwgc3R5bGVFbGVtZW50KTtcblx0XHRyZW1vdmUgPSBmdW5jdGlvbigpIHtcblx0XHRcdHJlbW92ZVN0eWxlRWxlbWVudChzdHlsZUVsZW1lbnQpO1xuXHRcdH07XG5cdH1cblxuXHR1cGRhdGUob2JqKTtcblxuXHRyZXR1cm4gZnVuY3Rpb24gdXBkYXRlU3R5bGUobmV3T2JqKSB7XG5cdFx0aWYobmV3T2JqKSB7XG5cdFx0XHRpZihuZXdPYmouY3NzID09PSBvYmouY3NzICYmIG5ld09iai5tZWRpYSA9PT0gb2JqLm1lZGlhICYmIG5ld09iai5zb3VyY2VNYXAgPT09IG9iai5zb3VyY2VNYXApXG5cdFx0XHRcdHJldHVybjtcblx0XHRcdHVwZGF0ZShvYmogPSBuZXdPYmopO1xuXHRcdH0gZWxzZSB7XG5cdFx0XHRyZW1vdmUoKTtcblx0XHR9XG5cdH07XG59XG5cbnZhciByZXBsYWNlVGV4dCA9IChmdW5jdGlvbiAoKSB7XG5cdHZhciB0ZXh0U3RvcmUgPSBbXTtcblxuXHRyZXR1cm4gZnVuY3Rpb24gKGluZGV4LCByZXBsYWNlbWVudCkge1xuXHRcdHRleHRTdG9yZVtpbmRleF0gPSByZXBsYWNlbWVudDtcblx0XHRyZXR1cm4gdGV4dFN0b3JlLmZpbHRlcihCb29sZWFuKS5qb2luKCdcXG4nKTtcblx0fTtcbn0pKCk7XG5cbmZ1bmN0aW9uIGFwcGx5VG9TaW5nbGV0b25UYWcoc3R5bGVFbGVtZW50LCBpbmRleCwgcmVtb3ZlLCBvYmopIHtcblx0dmFyIGNzcyA9IHJlbW92ZSA/IFwiXCIgOiBvYmouY3NzO1xuXG5cdGlmIChzdHlsZUVsZW1lbnQuc3R5bGVTaGVldCkge1xuXHRcdHN0eWxlRWxlbWVudC5zdHlsZVNoZWV0LmNzc1RleHQgPSByZXBsYWNlVGV4dChpbmRleCwgY3NzKTtcblx0fSBlbHNlIHtcblx0XHR2YXIgY3NzTm9kZSA9IGRvY3VtZW50LmNyZWF0ZVRleHROb2RlKGNzcyk7XG5cdFx0dmFyIGNoaWxkTm9kZXMgPSBzdHlsZUVsZW1lbnQuY2hpbGROb2Rlcztcblx0XHRpZiAoY2hpbGROb2Rlc1tpbmRleF0pIHN0eWxlRWxlbWVudC5yZW1vdmVDaGlsZChjaGlsZE5vZGVzW2luZGV4XSk7XG5cdFx0aWYgKGNoaWxkTm9kZXMubGVuZ3RoKSB7XG5cdFx0XHRzdHlsZUVsZW1lbnQuaW5zZXJ0QmVmb3JlKGNzc05vZGUsIGNoaWxkTm9kZXNbaW5kZXhdKTtcblx0XHR9IGVsc2Uge1xuXHRcdFx0c3R5bGVFbGVtZW50LmFwcGVuZENoaWxkKGNzc05vZGUpO1xuXHRcdH1cblx0fVxufVxuXG5mdW5jdGlvbiBhcHBseVRvVGFnKHN0eWxlRWxlbWVudCwgb2JqKSB7XG5cdHZhciBjc3MgPSBvYmouY3NzO1xuXHR2YXIgbWVkaWEgPSBvYmoubWVkaWE7XG5cblx0aWYobWVkaWEpIHtcblx0XHRzdHlsZUVsZW1lbnQuc2V0QXR0cmlidXRlKFwibWVkaWFcIiwgbWVkaWEpXG5cdH1cblxuXHRpZihzdHlsZUVsZW1lbnQuc3R5bGVTaGVldCkge1xuXHRcdHN0eWxlRWxlbWVudC5zdHlsZVNoZWV0LmNzc1RleHQgPSBjc3M7XG5cdH0gZWxzZSB7XG5cdFx0d2hpbGUoc3R5bGVFbGVtZW50LmZpcnN0Q2hpbGQpIHtcblx0XHRcdHN0eWxlRWxlbWVudC5yZW1vdmVDaGlsZChzdHlsZUVsZW1lbnQuZmlyc3RDaGlsZCk7XG5cdFx0fVxuXHRcdHN0eWxlRWxlbWVudC5hcHBlbmRDaGlsZChkb2N1bWVudC5jcmVhdGVUZXh0Tm9kZShjc3MpKTtcblx0fVxufVxuXG5mdW5jdGlvbiB1cGRhdGVMaW5rKGxpbmtFbGVtZW50LCBvYmopIHtcblx0dmFyIGNzcyA9IG9iai5jc3M7XG5cdHZhciBzb3VyY2VNYXAgPSBvYmouc291cmNlTWFwO1xuXG5cdGlmKHNvdXJjZU1hcCkge1xuXHRcdC8vIGh0dHA6Ly9zdGFja292ZXJmbG93LmNvbS9hLzI2NjAzODc1XG5cdFx0Y3NzICs9IFwiXFxuLyojIHNvdXJjZU1hcHBpbmdVUkw9ZGF0YTphcHBsaWNhdGlvbi9qc29uO2Jhc2U2NCxcIiArIGJ0b2EodW5lc2NhcGUoZW5jb2RlVVJJQ29tcG9uZW50KEpTT04uc3RyaW5naWZ5KHNvdXJjZU1hcCkpKSkgKyBcIiAqL1wiO1xuXHR9XG5cblx0dmFyIGJsb2IgPSBuZXcgQmxvYihbY3NzXSwgeyB0eXBlOiBcInRleHQvY3NzXCIgfSk7XG5cblx0dmFyIG9sZFNyYyA9IGxpbmtFbGVtZW50LmhyZWY7XG5cblx0bGlua0VsZW1lbnQuaHJlZiA9IFVSTC5jcmVhdGVPYmplY3RVUkwoYmxvYik7XG5cblx0aWYob2xkU3JjKVxuXHRcdFVSTC5yZXZva2VPYmplY3RVUkwob2xkU3JjKTtcbn1cbiIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0xMi5cbiAqL1xuaW1wb3J0ICcuL2Nzcy9pY29uZm9udC5jc3MnO1xuaW1wb3J0ICcuL2Nzcy9mb3JtLmNzcyc7XG5pbXBvcnQgJy4vZXh0ZXJuYWwvanF1ZXJ5LXVpLmNzcyc7XG5pbXBvcnQgJy4vZXh0ZXJuYWwvYm9vdHN0cmFwLWRhdGV0aW1lcGlja2VyLmNzcyc7XG5pbXBvcnQgJy4uLy4uL25vZGVfbW9kdWxlcy9ib290c3RyYXAvZGlzdC9qcy9ib290c3RyYXAuanMnO1xuaW1wb3J0IFV0aWxzIGZyb20gJy4vVXRpbHMuanMnO1xuaW1wb3J0IENhbnZhc0NvbnRhaW5lciBmcm9tICcuL2NvbnRhaW5lci9DYW52YXNDb250YWluZXIuanMnO1xuaW1wb3J0IFRvb2xiYXIgZnJvbSAnLi9Ub29sYmFyLmpzJztcbmltcG9ydCBQYWxldHRlIGZyb20gJy4vUGFsZXR0ZS5qcyc7XG5pbXBvcnQgUGFnZVByb3BlcnR5IGZyb20gJy4vcHJvcGVydHkvUGFnZVByb3BlcnR5LmpzJztcbmltcG9ydCBDb21wb25lbnQgZnJvbSAnLi9jb21wb25lbnQvQ29tcG9uZW50LmpzJztcblxuZXhwb3J0IGRlZmF1bHQgY2xhc3MgRm9ybUJ1aWxkZXJ7XG4gICAgY29uc3RydWN0b3IoY29udGFpbmVyKXtcbiAgICAgICAgd2luZG93LmZvcm1CdWlsZGVyPXRoaXM7XG4gICAgICAgIHRoaXMuY29udGFpbmVyPWNvbnRhaW5lcjtcbiAgICAgICAgdGhpcy5mb3JtUG9zaXRpb249XCJ1cFwiO1xuICAgICAgICB0aGlzLnRvb2xiYXI9bmV3IFRvb2xiYXIoKTtcbiAgICAgICAgdGhpcy5jb250YWluZXIuYXBwZW5kKHRoaXMudG9vbGJhci50b29sYmFyKTtcblxuICAgICAgICB2YXIgcGFsZXR0ZT1uZXcgUGFsZXR0ZSgpO1xuICAgICAgICB0aGlzLnByb3BlcnR5UGFsZXR0ZT1wYWxldHRlLnByb3BlcnR5UGFsZXR0ZTtcbiAgICAgICAgdGhpcy5jb21wb25lbnRzPXBhbGV0dGUuY29tcG9uZW50cztcbiAgICAgICAgdGhpcy5wYWdlUHJvcGVydHk9bmV3IFBhZ2VQcm9wZXJ0eSgpO1xuICAgICAgICB0aGlzLnByb3BlcnR5UGFsZXR0ZS5hcHBlbmQodGhpcy5wYWdlUHJvcGVydHkucHJvcGVydHlDb250YWluZXIpO1xuICAgICAgICB0aGlzLnBhZ2VQcm9wZXJ0eS5wcm9wZXJ0eUNvbnRhaW5lci5zaG93KCk7XG5cbiAgICAgICAgdGhpcy5jb250YWluZXIuYXBwZW5kKHBhbGV0dGUudGFiQ29udHJvbCk7XG4gICAgICAgIHRoaXMuY29udGFpbmVycz1bXTtcbiAgICAgICAgdGhpcy5pbnN0YW5jZXM9W107XG4gICAgICAgIHRoaXMuaW5pdFJvb3RDb250YWluZXIoKTtcbiAgICB9XG4gICAgaW5pdFJvb3RDb250YWluZXIoKXtcbiAgICAgICAgY29uc3QgYm9keT0kKFwiPGRpdiBzdHlsZT0nd2lkdGg6YXV0bzttYXJnaW4tbGVmdDozMDBweDttYXJnaW4tcmlnaHQ6MTBweCc+XCIpO1xuICAgICAgICB0aGlzLmNvbnRhaW5lci5hcHBlbmQoYm9keSk7XG4gICAgICAgIGNvbnN0IHNoYWRvd0NvbnRhaW5lcj0kKFwiPGRpdiBjbGFzcz0ncGItc2hhZG93Jz5cIik7XG4gICAgICAgIGJvZHkuYXBwZW5kKHNoYWRvd0NvbnRhaW5lcik7XG4gICAgICAgIGNvbnN0IGNvbnRhaW5lcj0kKFwiPGRpdiBjbGFzcz0nY29udGFpbmVyIHBiLWNhbnZhcy1jb250YWluZXIgZm9ybS1ob3Jpem9udGFsJyBzdHlsZT0nd2lkdGg6IGF1dG87cGFkZGluZzogMDsnPlwiKTtcbiAgICAgICAgc2hhZG93Q29udGFpbmVyLmFwcGVuZChjb250YWluZXIpO1xuICAgICAgICBjb25zdCByb3c9JChcIjxkaXYgY2xhc3M9J3Jvdyc+XCIpO1xuICAgICAgICBjb25zdCBjYW52YXM9JChcIjxkaXYgY2xhc3M9J2NvbC1tZC0xMiBwYi1kcm9wYWJsZS1ncmlkJyBzdHlsZT0nbWluLWhlaWdodDogMTAwcHg7Ym9yZGVyOiBub25lO3BhZGRpbmc6IDA7Oyc+XCIpO1xuICAgICAgICByb3cuYXBwZW5kKGNhbnZhcyk7XG4gICAgICAgIGNvbnRhaW5lci5hcHBlbmQocm93KTtcbiAgICAgICAgdGhpcy5yb290Q29udGFpbmVyPW5ldyBDYW52YXNDb250YWluZXIoY2FudmFzKTtcbiAgICAgICAgdGhpcy5jb250YWluZXJzLnB1c2godGhpcy5yb290Q29udGFpbmVyKTtcbiAgICAgICAgVXRpbHMuYXR0YWNoU29ydGFibGUoY2FudmFzKTtcbiAgICB9XG4gICAgaW5pdERhdGEocmVwb3J0RGVmKXtcbiAgICAgICAgdGhpcy5yZXBvcnREZWY9cmVwb3J0RGVmO1xuICAgICAgICByZXBvcnREZWYuX2Zvcm1CdWlsZGVyPXRoaXM7XG4gICAgICAgIGxldCBkYXRhc291cmNlcz1yZXBvcnREZWYuZGF0YXNvdXJjZXM7XG4gICAgICAgIGlmKCFkYXRhc291cmNlcyl7XG4gICAgICAgICAgICBkYXRhc291cmNlcz1bXTtcbiAgICAgICAgfVxuICAgICAgICBsZXQgcGFyYW1zPVtdO1xuICAgICAgICBsZXQgZGF0YXNldE1hcD1uZXcgTWFwKCk7XG4gICAgICAgIGZvcihsZXQgZHMgb2YgZGF0YXNvdXJjZXMpe1xuICAgICAgICAgICAgY29uc3QgZGF0YXNldHM9ZHMuZGF0YXNldHMgfHwgW107XG4gICAgICAgICAgICBmb3IobGV0IGRhdGFzZXQgb2YgZGF0YXNldHMpe1xuICAgICAgICAgICAgICAgIGNvbnN0IHBhcmFtZXRlcnM9ZGF0YXNldC5wYXJhbWV0ZXJzIHx8IFtdO1xuICAgICAgICAgICAgICAgIHBhcmFtcz1wYXJhbXMuY29uY2F0KHBhcmFtZXRlcnMpO1xuICAgICAgICAgICAgICAgIGRhdGFzZXRNYXAuc2V0KGRhdGFzZXQubmFtZSxkYXRhc2V0LmZpZWxkcyk7XG4gICAgICAgICAgICB9XG4gICAgICAgIH1cbiAgICAgICAgdGhpcy5yZXBvcnRQYXJhbWV0ZXJzPXBhcmFtcztcbiAgICAgICAgdGhpcy5kYXRhc2V0TWFwPWRhdGFzZXRNYXA7XG4gICAgICAgIGNvbnN0IGZvcm09cmVwb3J0RGVmLnNlYXJjaEZvcm0gfHwge307XG4gICAgICAgIGlmKGZvcm0pe1xuICAgICAgICAgICAgdGhpcy5mb3JtUG9zaXRpb249Zm9ybS5mb3JtUG9zaXRpb247XG4gICAgICAgICAgICBjb25zdCBjb21wb25lbnRzPSBmb3JtLmNvbXBvbmVudHM7XG4gICAgICAgICAgICB0aGlzLmJ1aWxkUGFnZUVsZW1lbnRzKGNvbXBvbmVudHMsdGhpcy5yb290Q29udGFpbmVyKTtcbiAgICAgICAgfVxuICAgICAgICB0aGlzLnBhZ2VQcm9wZXJ0eS5yZWZyZXNoVmFsdWUoKTtcbiAgICB9XG5cbiAgICBidWlsZERhdGEoKXtcbiAgICAgICAgdGhpcy5yZXBvcnREZWYuc2VhcmNoRm9ybVhtbD10aGlzLnRvWG1sKCk7XG4gICAgICAgIHRoaXMucmVwb3J0RGVmLnNlYXJjaEZvcm09dGhpcy50b0pzb24oKTtcbiAgICB9XG5cbiAgICBidWlsZFBhZ2VFbGVtZW50cyhlbGVtZW50cyxwYXJlbnRDb250YWluZXIpe1xuICAgICAgICBpZighZWxlbWVudHMgfHwgZWxlbWVudHMubGVuZ3RoPT09MCl7XG4gICAgICAgICAgICByZXR1cm47XG4gICAgICAgIH1cbiAgICAgICAgZm9yKHZhciBpPTA7aTxlbGVtZW50cy5sZW5ndGg7aSsrKXtcbiAgICAgICAgICAgIHZhciBlbGVtZW50PWVsZW1lbnRzW2ldO1xuICAgICAgICAgICAgdmFyIHR5cGU9ZWxlbWVudC50eXBlO1xuICAgICAgICAgICAgdmFyIHRhcmdldENvbXBvbmVudDtcbiAgICAgICAgICAgICQuZWFjaCh0aGlzLmNvbXBvbmVudHMsZnVuY3Rpb24oaW5kZXgsYyl7XG4gICAgICAgICAgICAgICAgaWYoYy5jb21wb25lbnQuc3VwcG9ydCh0eXBlKSl7XG4gICAgICAgICAgICAgICAgICAgIHRhcmdldENvbXBvbmVudD1jLmNvbXBvbmVudDtcbiAgICAgICAgICAgICAgICAgICAgcmV0dXJuIGZhbHNlO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgaWYoIXRhcmdldENvbXBvbmVudCl7XG4gICAgICAgICAgICAgICAgdGhyb3cgXCJVbmtub3cgY29tcG9uZW50IDogXCIrdHlwZStcIlwiO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgVXRpbHMuYXR0YWNoQ29tcG9uZW50KHRhcmdldENvbXBvbmVudCxwYXJlbnRDb250YWluZXIsZWxlbWVudCk7XG4gICAgICAgIH1cbiAgICB9XG4gICAgZ2V0SW5zdGFuY2UoaWQpe1xuICAgICAgICBsZXQgdGFyZ2V0O1xuICAgICAgICAkLmVhY2godGhpcy5pbnN0YW5jZXMsZnVuY3Rpb24oaW5kZXgsaXRlbSl7XG4gICAgICAgICAgICBpZihpdGVtLmlkPT09aWQpe1xuICAgICAgICAgICAgICAgIHRhcmdldD1pdGVtLmluc3RhbmNlO1xuICAgICAgICAgICAgICAgIHJldHVybiBmYWxzZTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfSk7XG4gICAgICAgIHJldHVybiB0YXJnZXQ7XG4gICAgfVxuICAgIHRvSnNvbigpe1xuICAgICAgICBjb25zdCBqc29uPXtmb3JtUG9zaXRpb246dGhpcy5mb3JtUG9zaXRpb259O1xuICAgICAgICBqc29uLmNvbXBvbmVudHM9dGhpcy5yb290Q29udGFpbmVyLnRvSnNvbigpO1xuICAgICAgICByZXR1cm4ganNvbjtcbiAgICB9XG4gICAgdG9YbWwoKXtcbiAgICAgICAgbGV0IHhtbD1gPHNlYXJjaC1mb3JtIGZvcm0tcG9zaXRpb249XCIke3RoaXMuZm9ybVBvc2l0aW9uIHx8ICd1cCd9XCI+YDtcbiAgICAgICAgeG1sKz10aGlzLnJvb3RDb250YWluZXIudG9YbWwoKTtcbiAgICAgICAgeG1sKz0nPC9zZWFyY2gtZm9ybT4nO1xuICAgICAgICByZXR1cm4geG1sO1xuICAgIH1cbiAgICBnZXRDb250YWluZXIoY29udGFpbmVySWQpe1xuICAgICAgICB2YXIgdGFyZ2V0Q29udGFpbmVyO1xuICAgICAgICAkLmVhY2godGhpcy5jb250YWluZXJzLGZ1bmN0aW9uKGluZGV4LGNvbnRhaW5lcil7XG4gICAgICAgICAgICBpZihjb250YWluZXIuaWQ9PT1jb250YWluZXJJZCl7XG4gICAgICAgICAgICAgICAgdGFyZ2V0Q29udGFpbmVyPWNvbnRhaW5lcjtcbiAgICAgICAgICAgICAgICByZXR1cm4gZmFsc2U7XG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuICAgICAgICByZXR1cm4gdGFyZ2V0Q29udGFpbmVyO1xuICAgIH1cbiAgICBzZWxlY3RFbGVtZW50KGluc3RhbmNlKXtcbiAgICAgICAgdmFyIGNoaWxkcmVuPXRoaXMucHJvcGVydHlQYWxldHRlLmNoaWxkcmVuKCk7XG4gICAgICAgIGNoaWxkcmVuLmVhY2goZnVuY3Rpb24oaSxpdGVtKXtcbiAgICAgICAgICAgICQoaXRlbSkuaGlkZSgpO1xuICAgICAgICB9KTtcbiAgICAgICAgaWYoIWluc3RhbmNlKXtcbiAgICAgICAgICAgIHRoaXMuc2VsZWN0PW51bGw7XG4gICAgICAgICAgICB0aGlzLnBhZ2VQcm9wZXJ0eS5yZWZyZXNoVmFsdWUoKTtcbiAgICAgICAgICAgIHRoaXMucGFnZVByb3BlcnR5LnByb3BlcnR5Q29udGFpbmVyLnNob3coKTtcbiAgICAgICAgICAgIHJldHVybjtcbiAgICAgICAgfVxuICAgICAgICBpZih0aGlzLnNlbGVjdCl7XG4gICAgICAgICAgICB2YXIgc2FtZUluc3RhbmNlPWZhbHNlO1xuICAgICAgICAgICAgaWYodGhpcy5zZWxlY3QucHJvcChcImlkXCIpPT09aW5zdGFuY2UucHJvcChcImlkXCIpKXtcbiAgICAgICAgICAgICAgICBzYW1lSW5zdGFuY2U9dHJ1ZTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIHRoaXMuc2VsZWN0LnJlbW92ZUNsYXNzKFwicGItaGFzRm9jdXNcIik7XG4gICAgICAgICAgICB0aGlzLnNlbGVjdD1udWxsO1xuICAgICAgICAgICAgaWYoc2FtZUluc3RhbmNlKXtcbiAgICAgICAgICAgICAgICB0aGlzLnBhZ2VQcm9wZXJ0eS5yZWZyZXNoVmFsdWUoKTtcbiAgICAgICAgICAgICAgICB0aGlzLnBhZ2VQcm9wZXJ0eS5wcm9wZXJ0eUNvbnRhaW5lci5zaG93KCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuO1xuICAgICAgICAgICAgfVxuICAgICAgICB9XG4gICAgICAgIGlmKCF0aGlzLnNlbGVjdCl7XG4gICAgICAgICAgICB0aGlzLnNlbGVjdD1pbnN0YW5jZTtcbiAgICAgICAgICAgIHRoaXMuc2VsZWN0LmFkZENsYXNzKFwicGItaGFzRm9jdXNcIik7XG4gICAgICAgIH1lbHNle1xuICAgICAgICAgICAgdGhpcy5zZWxlY3QucmVtb3ZlQ2xhc3MoXCJwYi1oYXNGb2N1c1wiKTtcbiAgICAgICAgICAgIGlmKHRoaXMuc2VsZWN0IT1pbnN0YW5jZSl7XG4gICAgICAgICAgICAgICAgdGhpcy5zZWxlY3Q9aW5zdGFuY2U7XG4gICAgICAgICAgICAgICAgdGhpcy5zZWxlY3QuYWRkQ2xhc3MoXCJwYi1oYXNGb2N1c1wiKTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfVxuICAgICAgICB2YXIgaW5zdGFuY2VJZD1pbnN0YW5jZS5wcm9wKFwiaWRcIik7XG4gICAgICAgICQuZWFjaCh0aGlzLmluc3RhbmNlcyxmdW5jdGlvbihpbmRleCxpdGVtKXtcbiAgICAgICAgICAgIGlmKGl0ZW0uaWQ9PT1pbnN0YW5jZUlkKXtcbiAgICAgICAgICAgICAgICB2YXIgaW5zdGFuY2U9aXRlbS5pbnN0YW5jZTtcbiAgICAgICAgICAgICAgICB2YXIgcHJvcGVydHk9aXRlbS5wcm9wZXJ0eTtcbiAgICAgICAgICAgICAgICBpZighcHJvcGVydHkpe1xuICAgICAgICAgICAgICAgICAgICByZXR1cm4gZmFsc2U7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIHByb3BlcnR5LnJlZnJlc2hWYWx1ZShpbnN0YW5jZSk7XG4gICAgICAgICAgICAgICAgcHJvcGVydHkucHJvcGVydHlDb250YWluZXIuc2hvdygpO1xuICAgICAgICAgICAgICAgIHJldHVybiBmYWxzZTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfSk7XG4gICAgfVxuICAgIGFkZEluc3RhbmNlKG5ld0luc3RhbmNlLG5ld0VsZW1lbnQsY29tcG9uZW50KXtcbiAgICAgICAgdGhpcy5pbnN0YW5jZXMucHVzaCh7XG4gICAgICAgICAgICBpZDpuZXdFbGVtZW50LnByb3AoXCJpZFwiKSxcbiAgICAgICAgICAgIGluc3RhbmNlOm5ld0luc3RhbmNlLFxuICAgICAgICAgICAgcHJvcGVydHk6Y29tcG9uZW50LnByb3BlcnR5XG4gICAgICAgIH0pO1xuICAgIH1cbiAgICBnZXRDb21wb25lbnQoaXRlbSl7XG4gICAgICAgIHZhciBjb21wb25lbnRJZD1pdGVtLmF0dHIoQ29tcG9uZW50LklEKTtcbiAgICAgICAgdmFyIHRhcmdldD1udWxsO1xuICAgICAgICAkKHRoaXMuY29tcG9uZW50cykuZWFjaChmdW5jdGlvbihpLGl0ZW0pe1xuICAgICAgICAgICAgdmFyIGlkPWl0ZW0uaWQ7XG4gICAgICAgICAgICBpZihpZD09PWNvbXBvbmVudElkKXtcbiAgICAgICAgICAgICAgICB0YXJnZXQ9aXRlbS5jb21wb25lbnQ7XG4gICAgICAgICAgICAgICAgcmV0dXJuIGZhbHNlO1xuICAgICAgICAgICAgfVxuICAgICAgICB9KTtcbiAgICAgICAgcmV0dXJuIHRhcmdldDtcbiAgICB9XG59IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTEyLlxuICovXG5pbXBvcnQgR3JpZDJYMkNvbXBvbmVudCBmcm9tICcuL2NvbXBvbmVudC9HcmlkMlgyQ29tcG9uZW50LmpzJztcbmltcG9ydCBHcmlkU2luZ2xlQ29tcG9uZW50IGZyb20gJy4vY29tcG9uZW50L0dyaWRTaW5nbGVDb21wb25lbnQuanMnO1xuaW1wb3J0IEdyaWQzeDN4M0NvbXBvbmVudCBmcm9tICcuL2NvbXBvbmVudC9HcmlkM3gzeDNDb21wb25lbnQuanMnO1xuaW1wb3J0IEdyaWQ0eDR4NHg0Q29tcG9uZW50IGZyb20gJy4vY29tcG9uZW50L0dyaWQ0eDR4NHg0Q29tcG9uZW50LmpzJztcbmltcG9ydCBHcmlkQ3VzdG9tQ29tcG9uZW50IGZyb20gJy4vY29tcG9uZW50L0dyaWRDdXN0b21Db21wb25lbnQuanMnO1xuaW1wb3J0IFRleHRDb21wb25lbnQgZnJvbSAnLi9jb21wb25lbnQvVGV4dENvbXBvbmVudC5qcyc7XG5pbXBvcnQgUmFkaW9Db21wb25lbnQgZnJvbSAnLi9jb21wb25lbnQvUmFkaW9Db21wb25lbnQuanMnO1xuaW1wb3J0IENoZWNrYm94Q29tcG9uZW50IGZyb20gJy4vY29tcG9uZW50L0NoZWNrYm94Q29tcG9uZW50LmpzJztcbmltcG9ydCBTZWxlY3RDb21wb25lbnQgZnJvbSAnLi9jb21wb25lbnQvU2VsZWN0Q29tcG9uZW50LmpzJztcbmltcG9ydCBTdWJtaXRCdXR0b25Db21wb25lbnQgZnJvbSAnLi9jb21wb25lbnQvU3VibWl0QnV0dG9uQ29tcG9uZW50LmpzJztcbmltcG9ydCBSZXNldEJ1dHRvbkNvbXBvbmVudCBmcm9tICcuL2NvbXBvbmVudC9SZXNldEJ1dHRvbkNvbXBvbmVudC5qcyc7XG5pbXBvcnQgRGF0ZXRpbWVDb21wb25lbnQgZnJvbSAnLi9jb21wb25lbnQvRGF0ZXRpbWVDb21wb25lbnQuanMnO1xuXG5leHBvcnQgZGVmYXVsdCBjbGFzcyBQYWxldHRle1xuICAgIGNvbnN0cnVjdG9yKCl7XG4gICAgICAgIHRoaXMuY29tcG9uZW50cz1bXTtcbiAgICAgICAgdGhpcy5pbml0Q29udGFpbmVyKCk7XG4gICAgICAgIHRoaXMuaW5pdENvbXBvbmVudHMoKTtcbiAgICB9XG4gICAgaW5pdENvbXBvbmVudHMoKXtcbiAgICAgICAgdGhpcy5hZGRDb21wb25lbnQobmV3IEdyaWRTaW5nbGVDb21wb25lbnQoe1xuICAgICAgICAgICAgaWNvbjpcImZvcm0gZm9ybS0xY29sXCIsXG4gICAgICAgICAgICBsYWJlbDpcIuS4gOWIl+W4g+WxgFwiXG4gICAgICAgIH0pKTtcbiAgICAgICAgdGhpcy5hZGRDb21wb25lbnQobmV3IEdyaWQyWDJDb21wb25lbnQoe1xuICAgICAgICAgICAgaWNvbjpcImZvcm0gZm9ybS0yY29sXCIsXG4gICAgICAgICAgICBsYWJlbDpcIuS4pOWIl+W4g+WxgFwiXG4gICAgICAgIH0pKTtcbiAgICAgICAgdGhpcy5hZGRDb21wb25lbnQobmV3IEdyaWQzeDN4M0NvbXBvbmVudCh7XG4gICAgICAgICAgICBpY29uOlwiZm9ybSBmb3JtLTNjb2xcIixcbiAgICAgICAgICAgIGxhYmVsOlwi5LiJ5YiX5biD5bGAXCJcbiAgICAgICAgfSkpO1xuICAgICAgICB0aGlzLmFkZENvbXBvbmVudChuZXcgR3JpZDR4NHg0eDRDb21wb25lbnQoe1xuICAgICAgICAgICAgaWNvbjpcImZvcm0gZm9ybS00Y29sXCIsXG4gICAgICAgICAgICBsYWJlbDpcIuWbm+WIl+W4g+WxgFwiXG4gICAgICAgIH0pKTtcbiAgICAgICAgdGhpcy5hZGRDb21wb25lbnQobmV3IEdyaWRDdXN0b21Db21wb25lbnQoe1xuICAgICAgICAgICAgaWNvbjpcImZvcm0gZm9ybS1jdXN0b20tY29sXCIsXG4gICAgICAgICAgICBsYWJlbDpcIuiHquWumuS5ieWIl+W4g+WxgFwiXG4gICAgICAgIH0pKTtcbiAgICAgICAgdGhpcy5hZGRDb21wb25lbnQobmV3IFRleHRDb21wb25lbnQoe1xuICAgICAgICAgICAgaWNvbjpcImZvcm0gZm9ybS10ZXh0Ym94XCIsXG4gICAgICAgICAgICBsYWJlbDpcIuaWh+acrOahhlwiXG4gICAgICAgIH0pKTtcbiAgICAgICAgdGhpcy5hZGRDb21wb25lbnQobmV3IERhdGV0aW1lQ29tcG9uZW50KHtcbiAgICAgICAgICAgIGljb246XCJnbHlwaGljb24gZ2x5cGhpY29uLWNhbGVuZGFyXCIsXG4gICAgICAgICAgICBsYWJlbDpcIuaXpeacn+mAieaLqeahhlwiXG4gICAgICAgIH0pKTtcbiAgICAgICAgdGhpcy5hZGRDb21wb25lbnQobmV3IFJhZGlvQ29tcG9uZW50KHtcbiAgICAgICAgICAgIGljb246XCJmb3JtIGZvcm0tcmFkaW9cIixcbiAgICAgICAgICAgIGxhYmVsOlwi5Y2V6YCJ5qGGXCJcbiAgICAgICAgfSkpO1xuICAgICAgICB0aGlzLmFkZENvbXBvbmVudChuZXcgQ2hlY2tib3hDb21wb25lbnQoe1xuICAgICAgICAgICAgaWNvbjpcImZvcm0gZm9ybS1jaGVja2JveFwiLFxuICAgICAgICAgICAgbGFiZWw6XCLlpI3pgInmoYZcIlxuICAgICAgICB9KSk7XG4gICAgICAgIHRoaXMuYWRkQ29tcG9uZW50KG5ldyBTZWxlY3RDb21wb25lbnQoe1xuICAgICAgICAgICAgaWNvbjpcImZvcm0gZm9ybS1kcm9wZG93blwiLFxuICAgICAgICAgICAgbGFiZWw6XCLljZXpgInliJfooahcIlxuICAgICAgICB9KSk7XG4gICAgICAgIHRoaXMuYWRkQ29tcG9uZW50KG5ldyBTdWJtaXRCdXR0b25Db21wb25lbnQoe1xuICAgICAgICAgICAgaWNvbjpcImZvcm0gZm9ybS1zdWJtaXRcIixcbiAgICAgICAgICAgIGxhYmVsOlwi5o+Q5Lqk5oyJ6ZKuXCJcbiAgICAgICAgfSkpO1xuICAgICAgICB0aGlzLmFkZENvbXBvbmVudChuZXcgUmVzZXRCdXR0b25Db21wb25lbnQoe1xuICAgICAgICAgICAgaWNvbjpcImZvcm0gZm9ybS1yZXNldFwiLFxuICAgICAgICAgICAgbGFiZWw6XCLph43nva7mjInpkq5cIlxuICAgICAgICB9KSk7XG4gICAgfVxuICAgIGluaXRDb250YWluZXIoKXtcbiAgICAgICAgdGhpcy50YWJDb250cm9sPSQoXCI8ZGl2IGNsYXNzPSdwYi1wYWxldHRlJz5cIik7XG4gICAgICAgIHZhciB1bD0kKFwiPHVsIGNsYXNzPSduYXYgbmF2LXRhYnMnIHN0eWxlPSdtYXJnaW46IDE1cHg7Jz5cIik7XG4gICAgICAgIHZhciBjb21wb25lbnRMaT0kKFwiPGxpIGNsYXNzPSdhY3RpdmUnPjxhIGhyZWY9JyNcIitQYWxldHRlLmNvbXBvbmVudElkK1wiJyBkYXRhLXRvZ2dsZT0ndGFiJz7nu4Tku7Y8L2E+XCIpO1xuICAgICAgICB1bC5hcHBlbmQoY29tcG9uZW50TGkpO1xuICAgICAgICB2YXIgcHJvcGVydHlMaT0kKFwiPGxpPjxhIGhyZWY9JyNcIitQYWxldHRlLnByb3BlcnR5SWQrXCInIGRhdGEtdG9nZ2xlPSd0YWInPuWxnuaApzwvYT48L2xpPlwiKTtcbiAgICAgICAgdWwuYXBwZW5kKHByb3BlcnR5TGkpO1xuICAgICAgICB0aGlzLnRhYkNvbnRyb2wuYXBwZW5kKHVsKTtcbiAgICAgICAgdmFyIHRhYkNvbnRlbnQ9JChcIjxkaXYgY2xhc3M9J3RhYi1jb250ZW50Jz5cIik7XG4gICAgICAgIHRoaXMuY29tcG9uZW50UGFsZXR0ZT0kKFwiPGRpdiBjbGFzcz1cXFwidGFiLXBhbmUgZmFkZSBpbiBhY3RpdmUgY29udGFpbmVyXFxcIiBpZD1cXFwiXCIrUGFsZXR0ZS5jb21wb25lbnRJZCtcIlxcXCIgc3R5bGU9XFxcIndpZHRoOiAxMDAlXFxcIj5cIik7XG4gICAgICAgIHRoaXMucHJvcGVydHlQYWxldHRlPSQoXCI8ZGl2IGNsYXNzPVxcXCJ0YWItcGFuZSBmYWRlIGNvbnRhaW5lclxcXCIgaWQ9XFxcIlwiK1BhbGV0dGUucHJvcGVydHlJZCtcIlxcXCIgc3R5bGU9XFxcIndpZHRoOmF1dG9cXFwiPlwiKTtcbiAgICAgICAgdGFiQ29udGVudC5hcHBlbmQodGhpcy5jb21wb25lbnRQYWxldHRlKTtcbiAgICAgICAgdGFiQ29udGVudC5hcHBlbmQodGhpcy5wcm9wZXJ0eVBhbGV0dGUpO1xuICAgICAgICB0aGlzLnRhYkNvbnRyb2wuYXBwZW5kKHRhYkNvbnRlbnQpO1xuICAgIH1cbiAgICBhZGRDb21wb25lbnQoY29tcG9uZW50KXtcbiAgICAgICAgaWYodGhpcy5yb3cpe1xuICAgICAgICAgICAgdmFyIGNvbD0kKFwiPGRpdiBjbGFzcz1cXFwiY29sLXNtLTZcXFwiPlwiKTtcbiAgICAgICAgICAgIGNvbC5hcHBlbmQoY29tcG9uZW50LnRvb2wpO1xuICAgICAgICAgICAgdGhpcy5yb3cuYXBwZW5kKGNvbCk7XG4gICAgICAgICAgICB0aGlzLnJvdz1udWxsO1xuICAgICAgICB9ZWxzZXtcbiAgICAgICAgICAgIHRoaXMucm93PSQoXCI8ZGl2IGNsYXNzPVxcXCJyb3dcXFwiPlwiKTtcbiAgICAgICAgICAgIHZhciBjb2w9JChcIjxkaXYgY2xhc3M9XFxcImNvbC1zbS02XFxcIj5cIik7XG4gICAgICAgICAgICBjb2wuYXBwZW5kKGNvbXBvbmVudC50b29sKTtcbiAgICAgICAgICAgIHRoaXMucm93LmFwcGVuZChjb2wpO1xuICAgICAgICAgICAgdGhpcy5jb21wb25lbnRQYWxldHRlLmFwcGVuZCh0aGlzLnJvdyk7XG4gICAgICAgIH1cbiAgICAgICAgdmFyIGNvbXBvbmVudElkPWNvbXBvbmVudC5pZDtcbiAgICAgICAgdGhpcy5jb21wb25lbnRzLnB1c2goe1xuICAgICAgICAgICAgXCJpZFwiOmNvbXBvbmVudElkLFxuICAgICAgICAgICAgXCJjb21wb25lbnRcIjpjb21wb25lbnRcbiAgICAgICAgfSk7XG4gICAgICAgIGlmKGNvbXBvbmVudC5wcm9wZXJ0eSl7XG4gICAgICAgICAgICB0aGlzLnByb3BlcnR5UGFsZXR0ZS5hcHBlbmQoY29tcG9uZW50LnByb3BlcnR5LnByb3BlcnR5Q29udGFpbmVyKTtcbiAgICAgICAgICAgIGNvbXBvbmVudC5wcm9wZXJ0eS5wcm9wZXJ0eUNvbnRhaW5lci5oaWRlKCk7XG4gICAgICAgIH1cbiAgICB9XG59XG5QYWxldHRlLmNvbXBvbmVudElkPVwicGJfY29tcG9uZW50X2NvbnRhaW5lcl9wYWxldHRlXCI7XG5QYWxldHRlLnByb3BlcnR5SWQ9XCJwYl9jb21wb25lbnRfcHJvcGVydHlfcGFsZXR0ZVwiOyIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0xMi5cbiAqL1xuaW1wb3J0IFV0aWxzIGZyb20gJy4vVXRpbHMuanMnO1xuaW1wb3J0IEluc3RhbmNlIGZyb20gJy4vaW5zdGFuY2UvSW5zdGFuY2UuanMnO1xuXG5leHBvcnQgZGVmYXVsdCBjbGFzcyBUb29sYmFye1xuICAgIGNvbnN0cnVjdG9yKCl7XG4gICAgICAgIHRoaXMudG9vbGJhcj0kKFwiPG5hdiBjbGFzcz1cXFwibmF2YmFyIG5hdmJhci1kZWZhdWx0IHBiLXRvb2xiYXJcXFwiIHN0eWxlPSdiYWNrZ3JvdW5kOiAjZmZmZmZmO21pbi1oZWlnaHQ6NDBweCcgcm9sZT1cXFwibmF2aWdhdGlvblxcXCI+XCIpO1xuICAgICAgICB2YXIgdWw9JChcIjx1bCBjbGFzcz1cXFwibmF2IG5hdmJhci1uYXZcXFwiPlwiKTtcbiAgICAgICAgdGhpcy50b29sYmFyLmFwcGVuZCh1bCk7XG5cbiAgICAgICAgdGhpcy50aXA9JChcIjxkaXYgY2xhc3M9J2FsZXJ0IGFsZXJ0LXN1Y2Nlc3MgYWxlcnQtZGlzbWlzc2FibGUnICBzdHlsZT0ncG9zaXRpb246IGFic29sdXRlO3RvcDo1MHB4O3dpZHRoOjEwMCU7ei1pbmRleDogMTAwJz4gPGJ1dHRvbiB0eXBlPSdidXR0b24nIGNsYXNzPSdjbG9zZScgZGF0YS1kaXNtaXNzPSdhbGVydCcgYXJpYS1oaWRkZW49J3RydWUnPiAmdGltZXM7IDwvYnV0dG9uPiDkv53lrZjmiJDlip8hICA8L2Rpdj5cIik7XG4gICAgICAgIHRoaXMudG9vbGJhci5hcHBlbmQodGhpcy50aXApO1xuICAgICAgICB0aGlzLnRpcC5oaWRlKCk7XG5cbiAgICAgICAgLy91bC5hcHBlbmQodGhpcy5idWlsZFNhdmUoKSk7XG4gICAgICAgIHVsLmFwcGVuZCh0aGlzLmJ1aWxkUmVtb3ZlKCkpO1xuICAgIH1cbiAgICBidWlsZFNhdmUoKXtcbiAgICAgICAgdGhpcy5zYXZlPSQoXCI8aSBjbGFzcz0nZ2x5cGhpY29uIGdseXBoaWNvbi1mbG9wcHktc2F2ZScgc3R5bGU9J2NvbG9yOiMyMTk2RjM7Zm9udC1zaXplOiAyMnB4O21hcmdpbjogMTBweDsnIHRpdGxlPSfkv53lrZgnPjwvaT5cIik7XG4gICAgICAgIHJldHVybiB0aGlzLnNhdmU7XG4gICAgfVxuICAgIGJ1aWxkUmVtb3ZlKCl7XG4gICAgICAgIHRoaXMucmVtb3ZlPSQoXCI8YnV0dG9uIHR5cGU9J2J1dHRvbicgc3R5bGU9J21hcmdpbjogNXB4JyBjbGFzcz0nYnRuIGJ0bi1kZWZhdWx0IGJ0bi1zbWFsbCc+PGkgc3R5bGU9J2NvbG9yOiByZWQnIGNsYXNzPSdnbHlwaGljb24gZ2x5cGhpY29uLXJlbW92ZSc+PC9pPiDliKDpmaTpgInkuK3nmoTlhYPntKA8L2J1dHRvbj5cIik7XG4gICAgICAgIHZhciBzZWxmPXRoaXM7XG4gICAgICAgIHRoaXMucmVtb3ZlLmNsaWNrKGZ1bmN0aW9uKCl7XG4gICAgICAgICAgICBzZWxmLmRlbGV0ZUVsZW1lbnQoKTtcbiAgICAgICAgfSk7XG4gICAgICAgICQoZG9jdW1lbnQpLmtleWRvd24oZnVuY3Rpb24oZSl7XG4gICAgICAgICAgICBpZihlLndoaWNoID09PSA0NiAmJiBlLnRhcmdldCAmJiBlLnRhcmdldD09PWRvY3VtZW50LmJvZHkpe1xuICAgICAgICAgICAgICAgIHNlbGYuZGVsZXRlRWxlbWVudCgpO1xuICAgICAgICAgICAgfVxuICAgICAgICB9KTtcbiAgICAgICAgcmV0dXJuIHRoaXMucmVtb3ZlO1xuICAgIH1cbiAgICBkZWxldGVFbGVtZW50KCl7XG4gICAgICAgIHZhciBzZWxlY3Q9Zm9ybUJ1aWxkZXIuc2VsZWN0O1xuICAgICAgICBpZighc2VsZWN0KXtcbiAgICAgICAgICAgIGJvb3Rib3guYWxlcnQoXCLor7flhYjpgInmi6nkuIDkuKrnu4Tku7YuXCIpO1xuICAgICAgICAgICAgcmV0dXJuO1xuICAgICAgICB9XG4gICAgICAgIHZhciBwYXJlbnQ9c2VsZWN0LnBhcmVudCgpO1xuICAgICAgICB2YXIgcGFyZW50Q29udGFpbmVyPWZvcm1CdWlsZGVyLmdldENvbnRhaW5lcihwYXJlbnQucHJvcChcImlkXCIpKTtcbiAgICAgICAgcGFyZW50Q29udGFpbmVyLnJlbW92ZUNoaWxkKHNlbGVjdCk7XG4gICAgICAgIHZhciBpZD1zZWxlY3QucHJvcChcImlkXCIpO1xuICAgICAgICB2YXIgcG9zPS0xLCB0YXJnZXRJbnM9bnVsbDtcbiAgICAgICAgJC5lYWNoKGZvcm1CdWlsZGVyLmluc3RhbmNlcyxmdW5jdGlvbihpLGl0ZW0pe1xuICAgICAgICAgICAgaWYoaXRlbS5pbnN0YW5jZS5pZD09PWlkKXtcbiAgICAgICAgICAgICAgICBwb3M9aTtcbiAgICAgICAgICAgICAgICB0YXJnZXRJbnM9aXRlbS5pbnN0YW5jZTtcbiAgICAgICAgICAgICAgICByZXR1cm4gZmFsc2U7XG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuICAgICAgICBpZihwb3M+LTEpe1xuICAgICAgICAgICAgZm9ybUJ1aWxkZXIuaW5zdGFuY2VzLnNwbGljZShwb3MsMSk7XG4gICAgICAgIH1lbHNle1xuICAgICAgICAgICAgYm9vdGJveC5hbGVydCgn5Yig6Zmk5YWD57Sg5pyq5rOo5YaMLOS4jeiDveiiq+WIoOmZpC4nKTtcbiAgICAgICAgICAgIHJldHVybjtcbiAgICAgICAgfVxuICAgICAgICBVdGlscy5yZW1vdmVDb250YWluZXJJbnN0YW5jZUNoaWxkcmVuKHRhcmdldElucyk7XG4gICAgICAgIHNlbGVjdC5yZW1vdmUoKTtcbiAgICAgICAgZm9ybUJ1aWxkZXIuc2VsZWN0RWxlbWVudCgpO1xuICAgIH1cbn0iLCIvKipcbiAqIENyZWF0ZWQgYnkgSmFja3kuR2FvIG9uIDIwMTctMTAtMTIuXG4gKi9cbmltcG9ydCBDb21wb25lbnQgZnJvbSAnLi9jb21wb25lbnQvQ29tcG9uZW50LmpzJztcbmltcG9ydCBUYWJDb250cm9sSW5zdGFuY2UgZnJvbSAnLi9pbnN0YW5jZS9UYWJDb250cm9sSW5zdGFuY2UuanMnO1xuaW1wb3J0IENvbnRhaW5lckluc3RhbmNlIGZyb20gJy4vaW5zdGFuY2UvQ29udGFpbmVySW5zdGFuY2UuanMnO1xuZXhwb3J0IGRlZmF1bHQgY2xhc3MgVXRpbHN7XG4gICAgc3RhdGljIHNlcShpZCl7XG4gICAgICAgIHZhciBzZXFWYWx1ZTtcbiAgICAgICAgJC5lYWNoKFV0aWxzLlNFUVVFTkNFLGZ1bmN0aW9uKG5hbWUsdmFsdWUpe1xuICAgICAgICAgICAgaWYobmFtZT09PWlkKXtcbiAgICAgICAgICAgICAgICB2YWx1ZSsrO1xuICAgICAgICAgICAgICAgIHNlcVZhbHVlPXZhbHVlO1xuICAgICAgICAgICAgICAgIFV0aWxzLlNFUVVFTkNFW2lkXT1zZXFWYWx1ZTtcbiAgICAgICAgICAgICAgICByZXR1cm4gZmFsc2U7XG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuICAgICAgICBpZighc2VxVmFsdWUpe1xuICAgICAgICAgICAgc2VxVmFsdWU9MTtcbiAgICAgICAgICAgIFV0aWxzLlNFUVVFTkNFW2lkXT1zZXFWYWx1ZTtcbiAgICAgICAgfVxuICAgICAgICByZXR1cm4gc2VxVmFsdWU7XG4gICAgfVxuICAgIHN0YXRpYyBhdHRhY2hTb3J0YWJsZSh0YXJnZXQpe1xuICAgICAgICB0YXJnZXQuc29ydGFibGUoe1xuICAgICAgICAgICAgdG9sZXJhbmNlOlwicG9pbnRlclwiLFxuICAgICAgICAgICAgZGVsYXk6MjAwLFxuICAgICAgICAgICAgZHJvcE9uRW1wdHk6dHJ1ZSxcbiAgICAgICAgICAgIGZvcmNlUGxhY2Vob2xkZXJTaXplOnRydWUsXG4gICAgICAgICAgICBmb3JjZUhlbHBlclNpemU6dHJ1ZSxcbiAgICAgICAgICAgIHBsYWNlaG9sZGVyOlwicGItc29ydGFibGUtcGxhY2Vob2xkZXJcIixcbiAgICAgICAgICAgIGNvbm5lY3RXaXRoOlwiLnBiLWRyb3BhYmxlLWdyaWQsLnBiLXRhYi1ncmlkLC5wYW5lbC1ib2R5LC5wYi1jYXJvdXNlbC1jb250YWluZXJcIixcbiAgICAgICAgICAgIHN0YXJ0OmZ1bmN0aW9uKGUsdWkpe1xuICAgICAgICAgICAgICAgIHVpLml0ZW0uY3NzKFwiZGlzcGxheVwiLFwiYmxvY2tcIik7XG4gICAgICAgICAgICB9LFxuICAgICAgICAgICAgcmVjZWl2ZTpmdW5jdGlvbihlLHVpKXtcbiAgICAgICAgICAgICAgICBVdGlscy5hZGQ9dHJ1ZTtcbiAgICAgICAgICAgIH0sXG4gICAgICAgICAgICByZW1vdmU6ZnVuY3Rpb24oZSx1aSl7XG4gICAgICAgICAgICAgICAgdmFyIGl0ZW09dWkuaXRlbTtcbiAgICAgICAgICAgICAgICB2YXIgcGFyZW50PSQodGhpcyk7XG4gICAgICAgICAgICAgICAgdmFyIHBhcmVudENvbnRhaW5lcj1mb3JtQnVpbGRlci5nZXRDb250YWluZXIocGFyZW50LnByb3AoXCJpZFwiKSk7XG4gICAgICAgICAgICAgICAgcGFyZW50Q29udGFpbmVyLnJlbW92ZUNoaWxkKGl0ZW0pO1xuICAgICAgICAgICAgfSxcbiAgICAgICAgICAgIHN0b3A6ZnVuY3Rpb24oZSx1aSl7XG4gICAgICAgICAgICAgICAgdmFyIGl0ZW09dWkuaXRlbTtcbiAgICAgICAgICAgICAgICB2YXIgcGFyZW50PWl0ZW0ucGFyZW50KCk7XG4gICAgICAgICAgICAgICAgdmFyIHBhcmVudENvbnRhaW5lcj1mb3JtQnVpbGRlci5nZXRDb250YWluZXIocGFyZW50LnByb3AoXCJpZFwiKSk7XG4gICAgICAgICAgICAgICAgaWYoIXBhcmVudENvbnRhaW5lcil7XG4gICAgICAgICAgICAgICAgICAgIHJldHVybjtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgaWYoaXRlbS5oYXNDbGFzcyhcInBiLWNvbXBvbmVudFwiKSl7XG4gICAgICAgICAgICAgICAgICAgIC8vbmV3IGNvbXBvbmVudFxuICAgICAgICAgICAgICAgICAgICB2YXIgdGFyZ2V0Q29tcG9uZW50PWZvcm1CdWlsZGVyLmdldENvbXBvbmVudChpdGVtKTtcbiAgICAgICAgICAgICAgICAgICAgdmFyIG5ld0VsZW1lbnQ9VXRpbHMuYXR0YWNoQ29tcG9uZW50KHRhcmdldENvbXBvbmVudCxwYXJlbnRDb250YWluZXIpO1xuICAgICAgICAgICAgICAgICAgICBpdGVtLnJlcGxhY2VXaXRoKG5ld0VsZW1lbnQpO1xuICAgICAgICAgICAgICAgICAgICBpdGVtPW5ld0VsZW1lbnQ7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGlmKFV0aWxzLmFkZCl7XG4gICAgICAgICAgICAgICAgICAgIHZhciB0YXJnZXRJbnN0YW5jZT1mb3JtQnVpbGRlci5nZXRJbnN0YW5jZShpdGVtLnByb3AoXCJpZFwiKSk7XG4gICAgICAgICAgICAgICAgICAgIHBhcmVudENvbnRhaW5lci5hZGRDaGlsZCh0YXJnZXRJbnN0YW5jZSk7XG4gICAgICAgICAgICAgICAgICAgIFV0aWxzLmFkZD1mYWxzZTtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgdmFyIG5ld09yZGVyPXBhcmVudC5zb3J0YWJsZShcInRvQXJyYXlcIik7XG4gICAgICAgICAgICAgICAgaWYobmV3T3JkZXIubGVuZ3RoPjEpe1xuICAgICAgICAgICAgICAgICAgICBwYXJlbnRDb250YWluZXIubmV3T3JkZXIobmV3T3JkZXIpO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgIH1cbiAgICAgICAgfSk7XG4gICAgfVxuICAgIHN0YXRpYyBhdHRhY2hDb21wb25lbnQodGFyZ2V0Q29tcG9uZW50LHBhcmVudENvbnRhaW5lcixpbml0SnNvbil7XG4gICAgICAgIHZhciBuZXdJbnN0YW5jZTtcbiAgICAgICAgaWYoaW5pdEpzb24pe1xuICAgICAgICAgICAgbmV3SW5zdGFuY2U9dGFyZ2V0Q29tcG9uZW50Lm5ld0luc3RhbmNlKGluaXRKc29uLmNvbHMpO1xuICAgICAgICAgICAgbmV3SW5zdGFuY2UuaW5pdEZyb21Kc29uKGluaXRKc29uKTtcbiAgICAgICAgfWVsc2V7XG4gICAgICAgICAgICBuZXdJbnN0YW5jZT10YXJnZXRDb21wb25lbnQubmV3SW5zdGFuY2UoKTtcbiAgICAgICAgfVxuICAgICAgICBwYXJlbnRDb250YWluZXIuYWRkQ2hpbGQobmV3SW5zdGFuY2UpO1xuICAgICAgICBpZihuZXdJbnN0YW5jZSBpbnN0YW5jZW9mIENvbnRhaW5lckluc3RhbmNlKXtcbiAgICAgICAgICAgICQuZWFjaChuZXdJbnN0YW5jZS5jb250YWluZXJzLGZ1bmN0aW9uKGksY29udGFpbmVyKXtcbiAgICAgICAgICAgICAgICBmb3JtQnVpbGRlci5jb250YWluZXJzLnB1c2goY29udGFpbmVyKTtcbiAgICAgICAgICAgIH0pO1xuICAgICAgICB9XG4gICAgICAgIHZhciBuZXdFbGVtZW50PW5ld0luc3RhbmNlLmVsZW1lbnQ7XG4gICAgICAgIG5ld0VsZW1lbnQuYXR0cihDb21wb25lbnQuSUQsdGFyZ2V0Q29tcG9uZW50LmlkKTtcbiAgICAgICAgZm9ybUJ1aWxkZXIuYWRkSW5zdGFuY2UobmV3SW5zdGFuY2UsbmV3RWxlbWVudCx0YXJnZXRDb21wb25lbnQpO1xuICAgICAgICBpZihpbml0SnNvbil7XG4gICAgICAgICAgICBwYXJlbnRDb250YWluZXIuYWRkRWxlbWVudChuZXdFbGVtZW50KTtcbiAgICAgICAgfVxuICAgICAgICB2YXIgY2hpbGRyZW5Db250YWluZXJzO1xuICAgICAgICBpZihuZXdFbGVtZW50Lmhhc0NsYXNzKFwicm93XCIpKXtcbiAgICAgICAgICAgIGNoaWxkcmVuQ29udGFpbmVycz1uZXdFbGVtZW50LmNoaWxkcmVuKFwiLnBiLWRyb3BhYmxlLWdyaWRcIik7XG4gICAgICAgIH1lbHNlIGlmKG5ld0VsZW1lbnQuaGFzQ2xhc3MoXCJ0YWJjb250YWluZXJcIikpe1xuICAgICAgICAgICAgY2hpbGRyZW5Db250YWluZXJzPW5ld0VsZW1lbnQuZmluZChcIi5wYi10YWItZ3JpZFwiKTtcbiAgICAgICAgfWVsc2UgaWYobmV3RWxlbWVudC5oYXNDbGFzcyhcInBhbmVsLWdyb3VwXCIpIHx8IG5ld0VsZW1lbnQuaGFzQ2xhc3MoXCJwYW5lbC1kZWZhdWx0XCIpKXtcbiAgICAgICAgICAgIGNoaWxkcmVuQ29udGFpbmVycz1uZXdFbGVtZW50LmZpbmQoXCIucGFuZWwtYm9keVwiKTtcbiAgICAgICAgfWVsc2UgaWYobmV3RWxlbWVudC5oYXNDbGFzcyhcImNhcm91c2VsXCIpKXtcbiAgICAgICAgICAgIGNoaWxkcmVuQ29udGFpbmVycz1uZXdFbGVtZW50LmZpbmQoXCIucGItY2Fyb3VzZWwtY29udGFpbmVyXCIpO1xuICAgICAgICB9ZWxzZSBpZihuZXdFbGVtZW50Lmhhc0NsYXNzKCdidG4nKSl7XG4gICAgICAgICAgICBjaGlsZHJlbkNvbnRhaW5lcnM9bmV3RWxlbWVudDtcbiAgICAgICAgfVxuICAgICAgICBpZihjaGlsZHJlbkNvbnRhaW5lcnMpe1xuICAgICAgICAgICAgY2hpbGRyZW5Db250YWluZXJzLmVhY2goZnVuY3Rpb24oaW5kZXgsY2hpbGQpe1xuICAgICAgICAgICAgICAgIFV0aWxzLmF0dGFjaFNvcnRhYmxlKCQoY2hpbGQpKTtcbiAgICAgICAgICAgIH0pO1xuICAgICAgICB9XG4gICAgICAgIG5ld0VsZW1lbnQuY2xpY2soZnVuY3Rpb24oZXZlbnQpe1xuICAgICAgICAgICAgZm9ybUJ1aWxkZXIuc2VsZWN0RWxlbWVudCgkKHRoaXMpKTtcbiAgICAgICAgICAgIGV2ZW50LnN0b3BQcm9wYWdhdGlvbigpO1xuICAgICAgICB9KTtcbiAgICAgICAgaWYoIW5ld0VsZW1lbnQuaGFzQ2xhc3MoXCJwYW5lbFwiKSAmJiAhbmV3RWxlbWVudC5oYXNDbGFzcyhcInBhbmVsLWRlZmF1bHRcIikpe1xuICAgICAgICAgICAgbmV3RWxlbWVudC5hZGRDbGFzcyhcInBiLWVsZW1lbnRcIik7XG4gICAgICAgIH1cbiAgICAgICAgbmV3RWxlbWVudC5tb3VzZW92ZXIoZnVuY3Rpb24oZSl7XG4gICAgICAgICAgICBuZXdFbGVtZW50LmFkZENsYXNzKFwicGItZWxlbWVudC1ob3ZlclwiKTtcbiAgICAgICAgICAgIGUuc3RvcFByb3BhZ2F0aW9uKCk7XG4gICAgICAgIH0pO1xuICAgICAgICBuZXdFbGVtZW50Lm1vdXNlb3V0KGZ1bmN0aW9uKGUpe1xuICAgICAgICAgICAgbmV3RWxlbWVudC5yZW1vdmVDbGFzcyhcInBiLWVsZW1lbnQtaG92ZXJcIik7XG4gICAgICAgICAgICBlLnN0b3BQcm9wYWdhdGlvbigpO1xuICAgICAgICB9KTtcbiAgICAgICAgcmV0dXJuIG5ld0VsZW1lbnQ7XG4gICAgfVxuICAgIHN0YXRpYyByZW1vdmVDb250YWluZXJJbnN0YW5jZUNoaWxkcmVuKGlucyl7XG4gICAgICAgIHZhciBjaGlsZHJlbkluc3RhbmNlcz1bXTtcbiAgICAgICAgaWYoaW5zIGluc3RhbmNlb2YgVGFiQ29udHJvbEluc3RhbmNlKXtcbiAgICAgICAgICAgIHZhciB0YWJzPWlucy50YWJzO1xuICAgICAgICAgICAgJC5lYWNoKHRhYnMsZnVuY3Rpb24oaW5kZXgsdGFiKXtcbiAgICAgICAgICAgICAgICB2YXIgY2hpbGRyZW49dGFiLmNvbnRhaW5lci5jaGlsZHJlbjtcbiAgICAgICAgICAgICAgICBjaGlsZHJlbkluc3RhbmNlcz1jaGlsZHJlbkluc3RhbmNlcy5jb25jYXQoY2hpbGRyZW4pO1xuICAgICAgICAgICAgfSk7XG4gICAgICAgIH1lbHNlIGlmKGlucyBpbnN0YW5jZW9mIENvbnRhaW5lckluc3RhbmNlKXtcbiAgICAgICAgICAgICQuZWFjaChpbnMuY29udGFpbmVycyxmdW5jdGlvbihpbmRleCxjb250YWluZXIpe1xuICAgICAgICAgICAgICAgIHZhciBjaGlsZHJlbj1jb250YWluZXIuY2hpbGRyZW47XG4gICAgICAgICAgICAgICAgY2hpbGRyZW5JbnN0YW5jZXM9Y2hpbGRyZW5JbnN0YW5jZXMuY29uY2F0KGNoaWxkcmVuKTtcbiAgICAgICAgICAgIH0pO1xuICAgICAgICB9XG4gICAgICAgIGlmKGNoaWxkcmVuSW5zdGFuY2VzLmxlbmd0aD09PTApcmV0dXJuO1xuICAgICAgICAkLmVhY2goY2hpbGRyZW5JbnN0YW5jZXMsZnVuY3Rpb24oaW5kZXgsY2hpbGQpe1xuICAgICAgICAgICAgdmFyIHBvcz0tMSxpZD1jaGlsZC5pZDtcbiAgICAgICAgICAgICQuZWFjaChmb3JtQnVpbGRlci5pbnN0YW5jZXMsZnVuY3Rpb24oaSxpdGVtKXtcbiAgICAgICAgICAgICAgICBpZihpdGVtLmlkPT09aWQpe1xuICAgICAgICAgICAgICAgICAgICBwb3M9aTtcbiAgICAgICAgICAgICAgICAgICAgcmV0dXJuIGZhbHNlO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgaWYocG9zPi0xKXtcbiAgICAgICAgICAgICAgICBmb3JtQnVpbGRlci5pbnN0YW5jZXMuc3BsaWNlKHBvcywxKTtcbiAgICAgICAgICAgIH1lbHNle1xuICAgICAgICAgICAgICAgIGJvb3Rib3guYWxlcnQoJ+WIoOmZpOWFg+e0oOacquazqOWGjCzkuI3og73ooqvliKDpmaQuJyk7XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBVdGlscy5yZW1vdmVDb250YWluZXJJbnN0YW5jZUNoaWxkcmVuKGNoaWxkKTtcbiAgICAgICAgfSk7XG4gICAgfVxufVxuVXRpbHMuU0VRVUVOQ0U9e307XG5VdGlscy5iaW5kaW5nPXRydWU7XG5VdGlscy5hZGQ9ZmFsc2U7IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTE2LlxuICovXG5pbXBvcnQgQ29tcG9uZW50IGZyb20gJy4vQ29tcG9uZW50LmpzJztcbmltcG9ydCBDaGVja2JveEluc3RhbmNlIGZyb20gJy4uL2luc3RhbmNlL0NoZWNrYm94SW5zdGFuY2UuanMnO1xuaW1wb3J0IENoZWNrYm94UHJvcGVydHkgZnJvbSAnLi4vcHJvcGVydHkvQ2hlY2tib3hQcm9wZXJ0eS5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBDaGVja2JveENvbXBvbmVudCBleHRlbmRzIENvbXBvbmVudHtcbiAgICBjb25zdHJ1Y3RvcihvcHRpb25zKXtcbiAgICAgICAgc3VwZXIob3B0aW9ucyk7XG4gICAgICAgIHRoaXMucHJvcGVydHk9bmV3IENoZWNrYm94UHJvcGVydHkoKTtcbiAgICB9XG4gICAgbmV3SW5zdGFuY2UoKXtcbiAgICAgICAgcmV0dXJuIG5ldyBDaGVja2JveEluc3RhbmNlKCk7XG4gICAgfVxuICAgIGdldFR5cGUoKXtcbiAgICAgICByZXR1cm4gQ2hlY2tib3hJbnN0YW5jZS5UWVBFO1xuICAgIH1cbiAgICBnZXRJZCgpe1xuICAgICAgICB0aGlzLmlkPVwiY2hlY2tib3hfY29tcG9uZW50XCI7XG4gICAgICAgIHJldHVybiB0aGlzLmlkO1xuICAgIH1cbn0iLCIvKipcbiAqIENyZWF0ZWQgYnkgSmFja3kuR2FvIG9uIDIwMTctMTAtMTIuXG4gKi9cblxuZXhwb3J0IGRlZmF1bHQgY2xhc3MgQ29tcG9uZW50e1xuICAgIGNvbnN0cnVjdG9yKG9wdGlvbnMpe1xuICAgICAgICB0aGlzLm9wdGlvbnM9b3B0aW9ucztcbiAgICAgICAgdGhpcy5lbnRpdHlMaXN0PVtdO1xuICAgICAgICB0aGlzLnRvb2w9JChcIjxkaXY+PGkgY2xhc3M9J1wiK29wdGlvbnMuaWNvbitcIicgc3R5bGU9J21hcmdpbi1yaWdodDo1cHgnPlwiK29wdGlvbnMubGFiZWwrXCI8L2Rpdj5cIik7XG4gICAgICAgIHRoaXMudG9vbC5hZGRDbGFzcyhcInBiLWNvbXBvbmVudFwiKTtcbiAgICAgICAgdGhpcy50b29sLmF0dHIoQ29tcG9uZW50LklELHRoaXMuZ2V0SWQoKSk7XG4gICAgICAgIHRoaXMudG9vbC5kcmFnZ2FibGUoe1xuICAgICAgICAgICAgcmV2ZXJ0OiBmYWxzZSxcbiAgICAgICAgICAgIGNvbm5lY3RUb1NvcnRhYmxlOlwiLnBiLWRyb3BhYmxlLWdyaWRcIixcbiAgICAgICAgICAgIGhlbHBlcjpcImNsb25lXCJcbiAgICAgICAgfSk7XG4gICAgfVxuICAgIHN1cHBvcnQodHlwZSl7XG4gICAgICAgIGlmKHR5cGU9PT10aGlzLmdldFR5cGUoKSl7XG4gICAgICAgICAgICByZXR1cm4gdHJ1ZTtcbiAgICAgICAgfVxuICAgICAgICByZXR1cm4gZmFsc2U7XG4gICAgfVxuICAgIGdldElkKCl7XG4gICAgICAgIHJldHVybiAnJztcbiAgICB9XG59XG5Db21wb25lbnQuSUQ9XCJjb21wb25lbnRfaWRcIjtcbkNvbXBvbmVudC5HUklEPVwiY29tcG9uZW50X2dyaWRcIjsiLCIvKipcbiAqIENyZWF0ZWQgYnkgSmFja3kuR2FvIG9uIDIwMTctMTAtMjMuXG4gKi9cbmltcG9ydCBDb21wb25lbnQgZnJvbSAnLi9Db21wb25lbnQuanMnO1xuaW1wb3J0IERhdGV0aW1lUHJvcGVydHkgZnJvbSAnLi4vcHJvcGVydHkvRGF0ZXRpbWVQcm9wZXJ0eS5qcyc7XG5pbXBvcnQgRGF0ZXRpbWVJbnN0YW5jZSBmcm9tICcuLi9pbnN0YW5jZS9EYXRldGltZUluc3RhbmNlLmpzJztcbmV4cG9ydCBkZWZhdWx0IGNsYXNzIERhdGV0aW1lQ29tcG9uZW50IGV4dGVuZHMgQ29tcG9uZW50e1xuICAgIGNvbnN0cnVjdG9yKG9wdGlvbnMpe1xuICAgICAgICBzdXBlcihvcHRpb25zKTtcbiAgICAgICAgdGhpcy5wcm9wZXJ0eT1uZXcgRGF0ZXRpbWVQcm9wZXJ0eSgpO1xuICAgIH1cbiAgICBuZXdJbnN0YW5jZSgpe1xuICAgICAgICByZXR1cm4gbmV3IERhdGV0aW1lSW5zdGFuY2UoKTtcbiAgICB9XG4gICAgZ2V0VHlwZSgpe1xuICAgICAgICByZXR1cm4gRGF0ZXRpbWVJbnN0YW5jZS5UWVBFO1xuICAgIH1cbiAgICBnZXRJZCgpe1xuICAgICAgICB0aGlzLmlkPVwiZGF0ZXRpbWVfY29tcG9uZW50XCI7XG4gICAgICAgIHJldHVybiB0aGlzLmlkO1xuICAgIH1cbn0iLCIvKipcbiAqIENyZWF0ZWQgYnkgSmFja3kuR2FvIG9uIDIwMTctMTAtMTUuXG4gKi9cbmltcG9ydCBHcmlkQ29tcG9uZW50IGZyb20gJy4vR3JpZENvbXBvbmVudC5qcyc7XG5pbXBvcnQgR3JpZDJYMkluc3RhbmNlIGZyb20gJy4uL2luc3RhbmNlL0dyaWQyWDJJbnN0YW5jZS5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBHcmlkMlgyQ29tcG9uZW50IGV4dGVuZHMgR3JpZENvbXBvbmVudHtcbiAgICBjb25zdHJ1Y3RvcihvcHRpb25zKXtcbiAgICAgICAgc3VwZXIob3B0aW9ucyk7XG4gICAgfVxuICAgIGdldElkKCl7XG4gICAgICAgIHRoaXMuaWQ9XCJjb21wb25lbnRfZ3JpZDJ4MlwiO1xuICAgICAgICByZXR1cm4gdGhpcy5pZDtcbiAgICB9XG4gICAgbmV3SW5zdGFuY2UoKXtcbiAgICAgICAgcmV0dXJuIG5ldyBHcmlkMlgySW5zdGFuY2UoKTtcbiAgICB9XG4gICAgZ2V0VHlwZSgpe1xuICAgICAgICByZXR1cm4gR3JpZDJYMkluc3RhbmNlLlRZUEU7XG4gICAgfVxufSIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0xNi5cbiAqL1xuaW1wb3J0IEdyaWRDb21wb25lbnQgZnJvbSAnLi9HcmlkQ29tcG9uZW50LmpzJztcbmltcG9ydCBHcmlkM3gzeDNJbnN0YW5jZSBmcm9tICcuLi9pbnN0YW5jZS9HcmlkM3gzeDNJbnN0YW5jZS5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBHcmlkM3gzeDNDb21wb25lbnQgZXh0ZW5kcyBHcmlkQ29tcG9uZW50e1xuICAgIGNvbnN0cnVjdG9yKG9wdGlvbnMpe1xuICAgICAgICBzdXBlcihvcHRpb25zKTtcbiAgICB9XG4gICAgbmV3SW5zdGFuY2UoKXtcbiAgICAgICAgcmV0dXJuIG5ldyBHcmlkM3gzeDNJbnN0YW5jZSgpO1xuICAgIH1cbiAgICBnZXRUeXBlKCl7XG4gICAgICAgIHJldHVybiBHcmlkM3gzeDNJbnN0YW5jZS5UWVBFO1xuICAgIH1cbiAgICBnZXRJZCgpe1xuICAgICAgICB0aGlzLmlkPVwiY29tcG9uZW50X2dyaWQzeDN4M1wiO1xuICAgICAgICByZXR1cm4gdGhpcy5pZDtcbiAgICB9XG59IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTE2LlxuICovXG5pbXBvcnQgR3JpZENvbXBvbmVudCBmcm9tICcuL0dyaWRDb21wb25lbnQuanMnO1xuaW1wb3J0IEdyaWQ0eDR4NHg0SW5zdGFuY2UgZnJvbSAnLi4vaW5zdGFuY2UvR3JpZDR4NHg0eDRJbnN0YW5jZS5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBHcmlkNHg0eDR4NENvbXBvbmVudCBleHRlbmRzIEdyaWRDb21wb25lbnR7XG4gICAgY29uc3RydWN0b3Iob3B0aW9ucyl7XG4gICAgICAgIHN1cGVyKG9wdGlvbnMpO1xuICAgIH1cbiAgICBuZXdJbnN0YW5jZSgpe1xuICAgICAgICByZXR1cm4gbmV3IEdyaWQ0eDR4NHg0SW5zdGFuY2UoKTtcbiAgICB9XG4gICAgZ2V0VHlwZSgpe1xuICAgICAgICByZXR1cm4gR3JpZDR4NHg0eDRJbnN0YW5jZS5UWVBFO1xuICAgIH1cbiAgICBnZXRJZCgpe1xuICAgICAgICB0aGlzLmlkPVwiY29tcG9uZW50X2dyaWQ0eDR4NHg0XCI7XG4gICAgICAgIHJldHVybiB0aGlzLmlkO1xuICAgIH1cbn1cbiIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0xNS5cbiAqL1xuaW1wb3J0IEdyaWRQcm9wZXJ0eSBmcm9tICcuLi9wcm9wZXJ0eS9HcmlkUHJvcGVydHkuanMnO1xuaW1wb3J0IENvbXBvbmVudCBmcm9tICcuL0NvbXBvbmVudC5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBHcmlkQ29tcG9uZW50IGV4dGVuZHMgQ29tcG9uZW50e1xuICAgIGNvbnN0cnVjdG9yKG9wdGlvbnMpe1xuICAgICAgICBzdXBlcihvcHRpb25zKTtcbiAgICAgICAgdGhpcy5wcm9wZXJ0eT1HcmlkQ29tcG9uZW50LnByb3BlcnR5O1xuICAgIH1cbn1cbkdyaWRDb21wb25lbnQucHJvcGVydHk9bmV3IEdyaWRQcm9wZXJ0eSgpOyIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0xNi5cbiAqL1xuaW1wb3J0IEdyaWRDb21wb25lbnQgZnJvbSAnLi9HcmlkQ29tcG9uZW50LmpzJztcbmltcG9ydCBHcmlkQ3VzdG9tSW5zdGFuY2UgZnJvbSAnLi4vaW5zdGFuY2UvR3JpZEN1c3RvbUluc3RhbmNlLmpzJztcbmV4cG9ydCBkZWZhdWx0IGNsYXNzIEdyaWRDdXN0b21Db21wb25lbnQgZXh0ZW5kcyBHcmlkQ29tcG9uZW50e1xuICAgIGNvbnN0cnVjdG9yKG9wdGlvbnMpe1xuICAgICAgICBzdXBlcihvcHRpb25zKTtcbiAgICB9XG4gICAgbmV3SW5zdGFuY2UoY29scyl7XG4gICAgICAgIHJldHVybiBuZXcgR3JpZEN1c3RvbUluc3RhbmNlKGNvbHMpO1xuICAgIH1cbiAgICBnZXRUeXBlKCl7XG4gICAgICAgIHJldHVybiBHcmlkQ3VzdG9tSW5zdGFuY2UuVFlQRTtcbiAgICB9XG4gICAgZ2V0SWQoKXtcbiAgICAgICAgdGhpcy5pZD1cImNvbXBvbmVudF9ncmlkY3VzdG9tXCI7XG4gICAgICAgIHJldHVybiB0aGlzLmlkO1xuICAgIH1cbn1cbiIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0xNi5cbiAqL1xuaW1wb3J0IEdyaWRDb21wb25lbnQgZnJvbSAnLi9HcmlkQ29tcG9uZW50LmpzJztcbmltcG9ydCBHcmlkU2luZ2xlSW5zdGFuY2UgZnJvbSAnLi4vaW5zdGFuY2UvR3JpZFNpbmdsZUluc3RhbmNlLmpzJztcbmV4cG9ydCBkZWZhdWx0IGNsYXNzIEdyaWRTaW5nbGVDb21wb25lbnQgZXh0ZW5kcyBHcmlkQ29tcG9uZW50e1xuICAgIGNvbnN0cnVjdG9yKG9wdGlvbnMpe1xuICAgICAgICBzdXBlcihvcHRpb25zKTtcbiAgICB9XG4gICAgbmV3SW5zdGFuY2UoKXtcbiAgICAgICAgcmV0dXJuIG5ldyBHcmlkU2luZ2xlSW5zdGFuY2UoKTtcbiAgICB9XG4gICAgZ2V0VHlwZSgpe1xuICAgICAgICByZXR1cm4gR3JpZFNpbmdsZUluc3RhbmNlLlRZUEU7XG4gICAgfVxuICAgIGdldElkKCl7XG4gICAgICAgIHRoaXMuaWQ9XCJjb21wb25lbnRfZ3JpZHNpbmdsZVwiO1xuICAgICAgICByZXR1cm4gdGhpcy5pZDtcbiAgICB9XG59IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTE2LlxuICovXG5pbXBvcnQgQ29tcG9uZW50IGZyb20gJy4vQ29tcG9uZW50LmpzJztcbmltcG9ydCBSYWRpb1Byb3BlcnR5IGZyb20gJy4uL3Byb3BlcnR5L1JhZGlvUHJvcGVydHkuanMnO1xuaW1wb3J0IFJhZGlvSW5zdGFuY2UgZnJvbSAnLi4vaW5zdGFuY2UvUmFkaW9JbnN0YW5jZS5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBSYWRpb0NvbXBvbmVudCBleHRlbmRzIENvbXBvbmVudHtcbiAgICBjb25zdHJ1Y3RvcihvcHRpb25zKXtcbiAgICAgICAgc3VwZXIob3B0aW9ucyk7XG4gICAgICAgIHRoaXMucHJvcGVydHk9bmV3IFJhZGlvUHJvcGVydHkoKTtcbiAgICB9XG4gICAgbmV3SW5zdGFuY2UoKXtcbiAgICAgICAgcmV0dXJuIG5ldyBSYWRpb0luc3RhbmNlKCk7XG4gICAgfVxuICAgIGdldFR5cGUoKXtcbiAgICAgICAgcmV0dXJuIFJhZGlvSW5zdGFuY2UuVFlQRTtcbiAgICB9XG4gICAgZ2V0SWQoKXtcbiAgICAgICAgdGhpcy5pZD1cInJhZGlvX2NvbXBvbmVudFwiO1xuICAgICAgICByZXR1cm4gdGhpcy5pZDtcbiAgICB9XG59IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTIwLlxuICovXG5pbXBvcnQgQ29tcG9uZW50IGZyb20gJy4vQ29tcG9uZW50LmpzJztcbmltcG9ydCBSZXNldEJ1dHRvbkluc3RhbmNlIGZyb20gJy4uL2luc3RhbmNlL1Jlc2V0QnV0dG9uSW5zdGFuY2UuanMnO1xuaW1wb3J0IEJ1dHRvblByb3BlcnR5IGZyb20gJy4uL3Byb3BlcnR5L0J1dHRvblByb3BlcnR5LmpzJztcbmltcG9ydCBVdGlscyBmcm9tICcuLi9VdGlscy5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBSZXNldEJ1dHRvbkNvbXBvbmVudCBleHRlbmRzIENvbXBvbmVudHtcbiAgICBjb25zdHJ1Y3RvcihvcHRpb25zKXtcbiAgICAgICAgc3VwZXIob3B0aW9ucyk7XG4gICAgICAgIHRoaXMucHJvcGVydHk9bmV3IEJ1dHRvblByb3BlcnR5KCk7XG4gICAgfVxuICAgIG5ld0luc3RhbmNlKCl7XG4gICAgICAgIHZhciBzZXE9VXRpbHMuc2VxKHRoaXMuaWQpO1xuICAgICAgICByZXR1cm4gbmV3IFJlc2V0QnV0dG9uSW5zdGFuY2UoXCLph43nva5cIitzZXEpO1xuICAgIH1cbiAgICBnZXRUeXBlKCl7XG4gICAgICAgIHJldHVybiBSZXNldEJ1dHRvbkluc3RhbmNlLlRZUEU7XG4gICAgfVxuICAgIGdldElkKCl7XG4gICAgICAgIHRoaXMuaWQ9XCJyZXNldF9idXR0b25cIjtcbiAgICAgICAgcmV0dXJuIHRoaXMuaWQ7XG4gICAgfVxufSIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0yMC5cbiAqL1xuaW1wb3J0IENvbXBvbmVudCBmcm9tICcuL0NvbXBvbmVudC5qcyc7XG5pbXBvcnQgU2VsZWN0UHJvcGVydHkgZnJvbSAnLi4vcHJvcGVydHkvU2VsZWN0UHJvcGVydHkuanMnO1xuaW1wb3J0IFV0aWxzIGZyb20gJy4uL1V0aWxzLmpzJztcbmltcG9ydCBTZWxlY3RJbnN0YW5jZSBmcm9tICcuLi9pbnN0YW5jZS9TZWxlY3RJbnN0YW5jZS5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBTZWxlY3RDb21wb25lbnQgZXh0ZW5kcyBDb21wb25lbnR7XG4gICAgY29uc3RydWN0b3Iob3B0aW9ucyl7XG4gICAgICAgIHN1cGVyKG9wdGlvbnMpO1xuICAgICAgICB0aGlzLnByb3BlcnR5PW5ldyBTZWxlY3RQcm9wZXJ0eSgpO1xuICAgIH1cbiAgICBuZXdJbnN0YW5jZSgpe1xuICAgICAgICB2YXIgc2VxPVV0aWxzLnNlcSh0aGlzLmlkKTtcbiAgICAgICAgcmV0dXJuIG5ldyBTZWxlY3RJbnN0YW5jZShzZXEpO1xuICAgIH1cbiAgICBnZXRUeXBlKCl7XG4gICAgICAgIHJldHVybiBTZWxlY3RJbnN0YW5jZS5UWVBFO1xuICAgIH1cbiAgICBnZXRJZCgpe1xuICAgICAgICB0aGlzLmlkPSBcInNpbmdsZV9zZWxlY3RcIjtcbiAgICAgICAgcmV0dXJuIHRoaXMuaWQ7XG4gICAgfVxufSIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0yMC5cbiAqL1xuaW1wb3J0IENvbXBvbmVudCBmcm9tICcuL0NvbXBvbmVudC5qcyc7XG5pbXBvcnQgU3VibWl0QnV0dG9uSW5zdGFuY2UgZnJvbSAnLi4vaW5zdGFuY2UvU3VibWl0QnV0dG9uSW5zdGFuY2UuanMnO1xuaW1wb3J0IEJ1dHRvblByb3BlcnR5IGZyb20gJy4uL3Byb3BlcnR5L0J1dHRvblByb3BlcnR5LmpzJztcbmltcG9ydCBVdGlscyBmcm9tICcuLi9VdGlscy5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBTdWJtaXRCdXR0b25Db21wb25lbnQgZXh0ZW5kcyBDb21wb25lbnR7XG4gICAgY29uc3RydWN0b3Iob3B0aW9ucyl7XG4gICAgICAgIHN1cGVyKG9wdGlvbnMpO1xuICAgICAgICB0aGlzLnByb3BlcnR5PW5ldyBCdXR0b25Qcm9wZXJ0eSgpO1xuICAgIH1cbiAgICBuZXdJbnN0YW5jZSgpe1xuICAgICAgICB2YXIgc2VxPVV0aWxzLnNlcSh0aGlzLmlkKTtcbiAgICAgICAgcmV0dXJuIG5ldyBTdWJtaXRCdXR0b25JbnN0YW5jZShcIuaPkOS6pFwiK3NlcSk7XG4gICAgfVxuICAgIGdldFR5cGUoKXtcbiAgICAgICAgcmV0dXJuIFN1Ym1pdEJ1dHRvbkluc3RhbmNlLlRZUEU7XG4gICAgfVxuICAgIGdldElkKCl7XG4gICAgICAgIHRoaXMuaWQ9XCJzdWJtaXRfYnV0dG9uXCI7XG4gICAgICAgIHJldHVybiB0aGlzLmlkO1xuICAgIH1cbn0iLCIvKipcbiAqIENyZWF0ZWQgYnkgSmFja3kuR2FvIG9uIDIwMTctMTAtMTYuXG4gKi9cbmltcG9ydCBDb21wb25lbnQgZnJvbSAnLi9Db21wb25lbnQuanMnO1xuaW1wb3J0IFRleHRJbnN0YW5jZSBmcm9tICcuLi9pbnN0YW5jZS9UZXh0SW5zdGFuY2UuanMnO1xuaW1wb3J0IFRleHRQcm9wZXJ0eSBmcm9tICcuLi9wcm9wZXJ0eS9UZXh0UHJvcGVydHkuanMnO1xuaW1wb3J0IFV0aWxzIGZyb20gJy4uL1V0aWxzLmpzJztcbmV4cG9ydCBkZWZhdWx0IGNsYXNzIFRleHRDb21wb25lbnQgZXh0ZW5kcyBDb21wb25lbnR7XG4gICAgY29uc3RydWN0b3Iob3B0aW9ucyl7XG4gICAgICAgIHN1cGVyKG9wdGlvbnMpO1xuICAgICAgICB0aGlzLnByb3BlcnR5PW5ldyBUZXh0UHJvcGVydHkoKTtcbiAgICB9XG4gICAgbmV3SW5zdGFuY2UoKXtcbiAgICAgICAgdmFyIHNlcT1VdGlscy5zZXEodGhpcy5pZCk7XG4gICAgICAgIHJldHVybiBuZXcgVGV4dEluc3RhbmNlKFwi6L6T5YWl5qGGXCIrc2VxKTtcbiAgICB9XG4gICAgZ2V0VHlwZSgpe1xuICAgICAgICByZXR1cm4gVGV4dEluc3RhbmNlLlRZUEU7XG4gICAgfVxuICAgIGdldElkKCl7XG4gICAgICAgIHRoaXMuaWQ9XCJjb21wb25lbnRfdGV4dGVkaXRvclwiO1xuICAgICAgICByZXR1cm4gdGhpcy5pZDtcbiAgICB9XG59XG4iLCIvKipcbiAqIENyZWF0ZWQgYnkgSmFja3kuR2FvIG9uIDIwMTctMTAtMTIuXG4gKi9cbmltcG9ydCBDb250YWluZXIgZnJvbSAnLi9Db250YWluZXIuanMnO1xuZXhwb3J0IGRlZmF1bHQgY2xhc3MgQ2FudmFzQ29udGFpbmVyIGV4dGVuZHMgQ29udGFpbmVye1xuICAgIGNvbnN0cnVjdG9yKGNhbnZhcyl7XG4gICAgICAgIHN1cGVyKCk7XG4gICAgICAgIHRoaXMuY29udGFpbmVyPWNhbnZhcztcbiAgICAgICAgdGhpcy5jb250YWluZXIudW5pcXVlSWQoKTtcbiAgICAgICAgdGhpcy5pZD10aGlzLmNvbnRhaW5lci5wcm9wKFwiaWRcIik7XG4gICAgfVxuICAgIGFkZEVsZW1lbnQoZWxlbWVudCl7XG4gICAgICAgIHRoaXMuY29udGFpbmVyLmFwcGVuZChlbGVtZW50KTtcbiAgICB9XG4gICAgdG9Kc29uKCl7XG4gICAgICAgIHZhciBjaGlsZHJlbj1bXTtcbiAgICAgICAgJC5lYWNoKHRoaXMuZ2V0Q2hpbGRyZW4oKSxmdW5jdGlvbihpbmRleCxjaGlsZCl7XG4gICAgICAgICAgICBjaGlsZHJlbi5wdXNoKGNoaWxkLnRvSnNvbigpKTtcbiAgICAgICAgfSk7XG4gICAgICAgIHJldHVybiBjaGlsZHJlbjtcbiAgICB9XG4gICAgdG9YbWwoKXtcbiAgICAgICAgbGV0IHhtbD0nJztcbiAgICAgICAgJC5lYWNoKHRoaXMuZ2V0Q2hpbGRyZW4oKSxmdW5jdGlvbihpbmRleCxjaGlsZCl7XG4gICAgICAgICAgICB4bWwrPWNoaWxkLnRvWG1sKCk7XG4gICAgICAgIH0pO1xuICAgICAgICByZXR1cm4geG1sO1xuICAgIH1cbiAgICBnZXRUeXBlKCl7XG4gICAgICAgIHJldHVybiBcIkNhbnZhc1wiO1xuICAgIH1cbiAgICB0b0h0bWwoKXtcbiAgICAgICAgdmFyIGRpdj0kKFwiPGRpdiBjbGFzcz0nY29udGFpbmVyJyBzdHlsZT0nd2lkdGg6IDEwMCU7Oyc+XCIpO1xuICAgICAgICB2YXIgcm93PSQoXCI8ZGl2IGNsYXNzPSdyb3cnPlwiKTtcbiAgICAgICAgdmFyIGNvbD0kKFwiPGRpdiBjbGFzcz0nY29sLW1kLTEyJz5cIik7XG4gICAgICAgIHJvdy5hcHBlbmQoY29sKTtcbiAgICAgICAgZGl2LmFwcGVuZChyb3cpO1xuICAgICAgICB0aGlzLmJ1aWxkQ2hpbGRyZW5IdG1sKGNvbCk7XG4gICAgICAgIHJldHVybiBkaXY7XG4gICAgfVxufSIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0xNS5cbiAqL1xuaW1wb3J0IENvbnRhaW5lciBmcm9tICcuL0NvbnRhaW5lci5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBDb2xDb250YWluZXIgZXh0ZW5kcyBDb250YWluZXJ7XG4gICAgY29uc3RydWN0b3IoY29sc2l6ZSl7XG4gICAgICAgIHN1cGVyKCk7XG4gICAgICAgIHRoaXMuY29sc2l6ZT1jb2xzaXplO1xuICAgICAgICB0aGlzLmNvbnRhaW5lcj0kKFwiPGRpdiBzdHlsZT0nbWluLWhlaWdodDo4MHB4O3BhZGRpbmc6IDFweCc+XCIpO1xuICAgICAgICB0aGlzLmNvbnRhaW5lci5hZGRDbGFzcyhcImNvbC1tZC1cIitjb2xzaXplK1wiXCIpO1xuICAgICAgICB0aGlzLmNvbnRhaW5lci5hZGRDbGFzcyhcInBiLWRyb3BhYmxlLWdyaWRcIik7XG4gICAgfVxuICAgIHRvSnNvbigpe1xuICAgICAgICBjb25zdCBqc29uPXtcbiAgICAgICAgICAgIHNpemU6dGhpcy5jb2xzaXplLFxuICAgICAgICAgICAgY2hpbGRyZW46W11cbiAgICAgICAgfTtcbiAgICAgICAgZm9yKGxldCBjaGlsZCBvZiB0aGlzLmdldENoaWxkcmVuKCkpe1xuICAgICAgICAgICAganNvbi5jaGlsZHJlbi5wdXNoKGNoaWxkLnRvSnNvbigpKTtcbiAgICAgICAgfVxuICAgICAgICByZXR1cm4ganNvbjtcbiAgICB9XG4gICAgdG9YbWwoKXtcbiAgICAgICAgbGV0IHhtbD1gPGNvbCBzaXplPVwiJHt0aGlzLmNvbHNpemV9XCI+YDtcbiAgICAgICAgZm9yKGxldCBjaGlsZCBvZiB0aGlzLmdldENoaWxkcmVuKCkpe1xuICAgICAgICAgICAgeG1sKz1jaGlsZC50b1htbCgpO1xuICAgICAgICB9XG4gICAgICAgIHhtbCs9YDwvY29sPmA7XG4gICAgICAgIHJldHVybiB4bWw7XG4gICAgfVxuICAgIGFkZEVsZW1lbnQoZWxlbWVudCl7XG4gICAgICAgIHRoaXMuY29udGFpbmVyLmFwcGVuZChlbGVtZW50KTtcbiAgICB9XG4gICAgaW5pdEZyb21Kc29uKGpzb24pe1xuICAgICAgICB2YXIgY2hpbGRyZW49anNvbi5jaGlsZHJlbjtcbiAgICAgICAgZm9ybUJ1aWxkZXIuYnVpbGRQYWdlRWxlbWVudHMoY2hpbGRyZW4sdGhpcyk7XG4gICAgfVxuICAgIGdldFR5cGUoKXtcbiAgICAgICAgcmV0dXJuIFwiQ29sXCI7XG4gICAgfVxuICAgIHRvSHRtbCgpe1xuICAgICAgICB2YXIgY29sPSQoXCI8ZGl2IGNsYXNzPSdjb2wtbWQtXCIrdGhpcy5jb2xzaXplK1wiJz5cIik7XG4gICAgICAgIHRoaXMuYnVpbGRDaGlsZHJlbkh0bWwoY29sKTtcbiAgICAgICAgcmV0dXJuIGNvbDtcbiAgICB9XG59XG4iLCIvKipcbiAqIENyZWF0ZWQgYnkgSmFja3kuR2FvIG9uIDIwMTctMTAtMTIuXG4gKi9cbmV4cG9ydCBkZWZhdWx0IGNsYXNzIENvbnRhaW5lcntcbiAgICBjb25zdHJ1Y3Rvcigpe1xuICAgICAgICB0aGlzLmNoaWxkcmVuPVtdO1xuICAgICAgICB0aGlzLm9yZGVyQXJyYXk9W107XG4gICAgfVxuICAgIGJ1aWxkQ2hpbGRyZW5IdG1sKGh0bWwpe1xuICAgICAgICB2YXIgY2hpbGRyZW49dGhpcy5nZXRDaGlsZHJlbigpO1xuICAgICAgICAkLmVhY2goY2hpbGRyZW4sZnVuY3Rpb24oaW5kZXgsY2hpbGQpe1xuICAgICAgICAgICAgaHRtbC5hcHBlbmQoY2hpbGQudG9IdG1sKCkpO1xuICAgICAgICB9KTtcbiAgICAgICAgcmV0dXJuIGNoaWxkcmVuO1xuICAgIH1cbiAgICBnZXRDaGlsZHJlbigpe1xuICAgICAgICBmb3IodmFyIGk9KHRoaXMub3JkZXJBcnJheS5sZW5ndGgtMSk7aT4tMTtpLS0pe1xuICAgICAgICAgICAgdmFyIGlkPXRoaXMub3JkZXJBcnJheVtpXTtcbiAgICAgICAgICAgIHZhciB0YXJnZXQ9Q29udGFpbmVyLnNlYXJjaEFuZFJlbW92ZUNoaWxkKGlkLHRoaXMuY2hpbGRyZW4pO1xuICAgICAgICAgICAgaWYodGFyZ2V0KXtcbiAgICAgICAgICAgICAgICB0aGlzLmNoaWxkcmVuLnVuc2hpZnQodGFyZ2V0KTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfVxuICAgICAgICByZXR1cm4gdGhpcy5jaGlsZHJlbjtcbiAgICB9XG4gICAgYWRkQ2hpbGQoY2hpbGQpe1xuICAgICAgICBpZigkLmluQXJyYXkoY2hpbGQsdGhpcy5jaGlsZHJlbik9PT0tMSl7XG4gICAgICAgICAgICB0aGlzLmNoaWxkcmVuLnB1c2goY2hpbGQpO1xuICAgICAgICB9XG4gICAgfVxuICAgIGdldENvbnRhaW5lcigpe1xuICAgICAgICBpZighdGhpcy5pZCl7XG4gICAgICAgICAgICB0aGlzLmlkPXRoaXMuY29udGFpbmVyLnByb3AoXCJpZFwiKTtcbiAgICAgICAgICAgIGlmKCF0aGlzLmlkKXtcbiAgICAgICAgICAgICAgICB0aGlzLmNvbnRhaW5lci51bmlxdWVJZCgpO1xuICAgICAgICAgICAgICAgIHRoaXMuaWQ9dGhpcy5jb250YWluZXIucHJvcChcImlkXCIpO1xuICAgICAgICAgICAgfVxuICAgICAgICB9XG4gICAgICAgIHJldHVybiB0aGlzLmNvbnRhaW5lcjtcbiAgICB9XG4gICAgcmVtb3ZlQ2hpbGQoY2hpbGQpe1xuICAgICAgICB2YXIgaWQ9Y2hpbGQucHJvcChcImlkXCIpO1xuICAgICAgICBpZighaWQgfHwgaWQ9PT1cIlwiKXJldHVybjtcbiAgICAgICAgdmFyIHBvcz0tMTtcbiAgICAgICAgJC5lYWNoKHRoaXMuY2hpbGRyZW4sZnVuY3Rpb24oaW5kZXgsaXRlbSl7XG4gICAgICAgICAgICBpZihpdGVtLmlkPT09aWQpe1xuICAgICAgICAgICAgICAgIHBvcz1pbmRleDtcbiAgICAgICAgICAgICAgICByZXR1cm4gZmFsc2U7XG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuICAgICAgICBpZihwb3M+LTEpe1xuICAgICAgICAgICAgdGhpcy5jaGlsZHJlbi5zcGxpY2UocG9zLDEpO1xuICAgICAgICB9XG4gICAgfVxuICAgIG5ld09yZGVyKG9yZGVyQXJyYXkpe1xuICAgICAgICB0aGlzLm9yZGVyQXJyYXk9b3JkZXJBcnJheTtcbiAgICB9XG4gICAgc3RhdGljIHNlYXJjaEFuZFJlbW92ZUNoaWxkKGlkLGNoaWxkcmVuKXtcbiAgICAgICAgdmFyIHRhcmdldCxwb3M9LTE7XG4gICAgICAgICQuZWFjaChjaGlsZHJlbixmdW5jdGlvbihpbmRleCxpbnN0YW5jZSl7XG4gICAgICAgICAgICBpZihpbnN0YW5jZS5pZD09PWlkKXtcbiAgICAgICAgICAgICAgICB0YXJnZXQ9aW5zdGFuY2U7XG4gICAgICAgICAgICAgICAgcG9zPWluZGV4O1xuICAgICAgICAgICAgICAgIHJldHVybiBmYWxzZTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfSk7XG4gICAgICAgIGlmKHBvcyE9LTEpe1xuICAgICAgICAgICAgY2hpbGRyZW4uc3BsaWNlKHBvcywxKTtcbiAgICAgICAgfVxuICAgICAgICByZXR1cm4gdGFyZ2V0O1xuICAgIH1cbn0iLCIvKipcbiAqIENyZWF0ZWQgYnkgSmFja3kuR2FvIG9uIDIwMTctMTAtMTIuXG4gKi9cbmltcG9ydCBDb250YWluZXIgZnJvbSAnLi9Db250YWluZXIuanMnO1xuXG5leHBvcnQgZGVmYXVsdCBjbGFzcyBUYWJDb250YWluZXIgZXh0ZW5kcyBDb250YWluZXJ7XG4gICAgY29uc3RydWN0b3IoaWQpe1xuICAgICAgICBzdXBlcigpO1xuICAgICAgICB0aGlzLmlkPWlkO1xuICAgICAgICB0aGlzLmNvbnRhaW5lcj0kKFwiPGRpdiBjbGFzcz0ndGFiLXBhbmUgZmFkZSBwYi10YWItZ3JpZCcgaWQ9J1wiK3RoaXMuaWQrXCInPlwiKTtcbiAgICB9XG4gICAgYWRkRWxlbWVudChlbGVtZW50KXtcbiAgICAgICAgdGhpcy5jb250YWluZXIuYXBwZW5kKGVsZW1lbnQpO1xuICAgIH1cbiAgICBpbml0RnJvbUpzb24oanNvbil7XG4gICAgICAgIGZvcm1CdWlsZGVyLmJ1aWxkUGFnZUVsZW1lbnRzKGpzb24sdGhpcyk7XG4gICAgfVxuICAgIHRvSlNPTigpe1xuICAgICAgICB2YXIgY2hpbGRyZW49W107XG4gICAgICAgICQuZWFjaCh0aGlzLmdldENoaWxkcmVuKCksZnVuY3Rpb24oaW5kZXgsY2hpbGQpe1xuICAgICAgICAgICAgY2hpbGRyZW4ucHVzaChjaGlsZC50b0pTT04oKSk7XG4gICAgICAgIH0pO1xuICAgICAgICByZXR1cm4gY2hpbGRyZW47XG4gICAgfVxuICAgIHRvSHRtbCgpe1xuICAgICAgICB2YXIgZGl2PSQoXCI8ZGl2IGNsYXNzPSd0YWItcGFuZSBmYWRlIHBiLXRhYi1ncmlkJyBpZD0nXCIrdGhpcy5pZCtcIjEnPlwiKTtcbiAgICAgICAgZGl2LmFwcGVuZCh0aGlzLmJ1aWxkQ2hpbGRyZW5IdG1sKGRpdikpO1xuICAgICAgICByZXR1cm4gZGl2O1xuICAgIH1cbn1cbiIsIi8vIHN0eWxlLWxvYWRlcjogQWRkcyBzb21lIGNzcyB0byB0aGUgRE9NIGJ5IGFkZGluZyBhIDxzdHlsZT4gdGFnXG5cbi8vIGxvYWQgdGhlIHN0eWxlc1xudmFyIGNvbnRlbnQgPSByZXF1aXJlKFwiISEuLi8uLi8uLi9ub2RlX21vZHVsZXMvY3NzLWxvYWRlci9pbmRleC5qcyEuL2Zvcm0uY3NzXCIpO1xuaWYodHlwZW9mIGNvbnRlbnQgPT09ICdzdHJpbmcnKSBjb250ZW50ID0gW1ttb2R1bGUuaWQsIGNvbnRlbnQsICcnXV07XG4vLyBhZGQgdGhlIHN0eWxlcyB0byB0aGUgRE9NXG52YXIgdXBkYXRlID0gcmVxdWlyZShcIiEuLi8uLi8uLi9ub2RlX21vZHVsZXMvc3R5bGUtbG9hZGVyL2FkZFN0eWxlcy5qc1wiKShjb250ZW50LCB7fSk7XG5pZihjb250ZW50LmxvY2FscykgbW9kdWxlLmV4cG9ydHMgPSBjb250ZW50LmxvY2Fscztcbi8vIEhvdCBNb2R1bGUgUmVwbGFjZW1lbnRcbmlmKG1vZHVsZS5ob3QpIHtcblx0Ly8gV2hlbiB0aGUgc3R5bGVzIGNoYW5nZSwgdXBkYXRlIHRoZSA8c3R5bGU+IHRhZ3Ncblx0aWYoIWNvbnRlbnQubG9jYWxzKSB7XG5cdFx0bW9kdWxlLmhvdC5hY2NlcHQoXCIhIS4uLy4uLy4uL25vZGVfbW9kdWxlcy9jc3MtbG9hZGVyL2luZGV4LmpzIS4vZm9ybS5jc3NcIiwgZnVuY3Rpb24oKSB7XG5cdFx0XHR2YXIgbmV3Q29udGVudCA9IHJlcXVpcmUoXCIhIS4uLy4uLy4uL25vZGVfbW9kdWxlcy9jc3MtbG9hZGVyL2luZGV4LmpzIS4vZm9ybS5jc3NcIik7XG5cdFx0XHRpZih0eXBlb2YgbmV3Q29udGVudCA9PT0gJ3N0cmluZycpIG5ld0NvbnRlbnQgPSBbW21vZHVsZS5pZCwgbmV3Q29udGVudCwgJyddXTtcblx0XHRcdHVwZGF0ZShuZXdDb250ZW50KTtcblx0XHR9KTtcblx0fVxuXHQvLyBXaGVuIHRoZSBtb2R1bGUgaXMgZGlzcG9zZWQsIHJlbW92ZSB0aGUgPHN0eWxlPiB0YWdzXG5cdG1vZHVsZS5ob3QuZGlzcG9zZShmdW5jdGlvbigpIHsgdXBkYXRlKCk7IH0pO1xufSIsIi8vIHN0eWxlLWxvYWRlcjogQWRkcyBzb21lIGNzcyB0byB0aGUgRE9NIGJ5IGFkZGluZyBhIDxzdHlsZT4gdGFnXG5cbi8vIGxvYWQgdGhlIHN0eWxlc1xudmFyIGNvbnRlbnQgPSByZXF1aXJlKFwiISEuLi8uLi8uLi9ub2RlX21vZHVsZXMvY3NzLWxvYWRlci9pbmRleC5qcyEuL2ljb25mb250LmNzc1wiKTtcbmlmKHR5cGVvZiBjb250ZW50ID09PSAnc3RyaW5nJykgY29udGVudCA9IFtbbW9kdWxlLmlkLCBjb250ZW50LCAnJ11dO1xuLy8gYWRkIHRoZSBzdHlsZXMgdG8gdGhlIERPTVxudmFyIHVwZGF0ZSA9IHJlcXVpcmUoXCIhLi4vLi4vLi4vbm9kZV9tb2R1bGVzL3N0eWxlLWxvYWRlci9hZGRTdHlsZXMuanNcIikoY29udGVudCwge30pO1xuaWYoY29udGVudC5sb2NhbHMpIG1vZHVsZS5leHBvcnRzID0gY29udGVudC5sb2NhbHM7XG4vLyBIb3QgTW9kdWxlIFJlcGxhY2VtZW50XG5pZihtb2R1bGUuaG90KSB7XG5cdC8vIFdoZW4gdGhlIHN0eWxlcyBjaGFuZ2UsIHVwZGF0ZSB0aGUgPHN0eWxlPiB0YWdzXG5cdGlmKCFjb250ZW50LmxvY2Fscykge1xuXHRcdG1vZHVsZS5ob3QuYWNjZXB0KFwiISEuLi8uLi8uLi9ub2RlX21vZHVsZXMvY3NzLWxvYWRlci9pbmRleC5qcyEuL2ljb25mb250LmNzc1wiLCBmdW5jdGlvbigpIHtcblx0XHRcdHZhciBuZXdDb250ZW50ID0gcmVxdWlyZShcIiEhLi4vLi4vLi4vbm9kZV9tb2R1bGVzL2Nzcy1sb2FkZXIvaW5kZXguanMhLi9pY29uZm9udC5jc3NcIik7XG5cdFx0XHRpZih0eXBlb2YgbmV3Q29udGVudCA9PT0gJ3N0cmluZycpIG5ld0NvbnRlbnQgPSBbW21vZHVsZS5pZCwgbmV3Q29udGVudCwgJyddXTtcblx0XHRcdHVwZGF0ZShuZXdDb250ZW50KTtcblx0XHR9KTtcblx0fVxuXHQvLyBXaGVuIHRoZSBtb2R1bGUgaXMgZGlzcG9zZWQsIHJlbW92ZSB0aGUgPHN0eWxlPiB0YWdzXG5cdG1vZHVsZS5ob3QuZGlzcG9zZShmdW5jdGlvbigpIHsgdXBkYXRlKCk7IH0pO1xufSIsIm1vZHVsZS5leHBvcnRzID0gXCJkYXRhOmFwcGxpY2F0aW9uL3ZuZC5tcy1mb250b2JqZWN0O2Jhc2U2NCxZQklBQU1nUkFBQUJBQUlBQUFBQUFBSUFCUU1BQUFBQUFBQUJBSkFCQUFBQUFFeFFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBRUFBQUFBQUFBQXpwNW9qd0FBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQWdBWmdCdkFISUFiUUFBQUE0QVVnQmxBR2NBZFFCc0FHRUFjZ0FBQUJZQVZnQmxBSElBY3dCcEFHOEFiZ0FnQURFQUxnQXdBQUFBQ0FCbUFHOEFjZ0J0QUFBQUFBQUFBUUFBQUFzQWdBQURBREJIVTFWQ3NQNno3UUFBQVRnQUFBQkNUMU12TWxidVNOd0FBQUY4QUFBQVZtTnRZWERQcTlVbEFBQUNHQUFBQXFKbmJIbG1CZVJuWHdBQUJPQUFBQW4wYUdWaFpBOCs2V01BQUFEZ0FBQUFObWhvWldFSDNnT1NBQUFBdkFBQUFDUm9iWFI0UStrQUFBQUFBZFFBQUFCRWJHOWpZUlJlRnVnQUFBUzhBQUFBSkcxaGVIQUJKZ0NuQUFBQkdBQUFBQ0J1WVcxbE5TbjZzd0FBRHRRQUFBSTljRzl6ZEdROGdoWUFBQkVVQUFBQXNnQUJBQUFEZ1ArQUFGd0VBQUFBQUFBRUFBQUJBQUFBQUFBQUFBQUFBQUFBQUFBQUVRQUJBQUFBQVFBQWoyaWV6bDhQUFBVQUN3UUFBQUFBQU5ZUDByTUFBQUFBMWcvU3N3QUEvNEFFQUFPQUFBQUFDQUFDQUFBQUFBQUFBQUVBQUFBUkFKc0FDd0FBQUFBQUFnQUFBQW9BQ2dBQUFQOEFBQUFBQUFBQUFRQUFBQW9BSGdBc0FBRkVSa3hVQUFnQUJBQUFBQUFBQUFBQkFBQUFBV3hwWjJFQUNBQUFBQUVBQUFBQkFBUUFCQUFBQUFFQUNBQUJBQVlBQUFBQkFBQUFBQUFCQS84QmtBQUZBQWdDaVFMTUFBQUFqd0tKQXN3QUFBSHJBRElCQ0FBQUFnQUZBd0FBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFCUVprVmtBRUFBZU9ickE0RC9nQUJjQTRBQWdBQUFBQUVBQUFBQUFBQUVBQUFBQStrQUFBUUFBQUFFQUFBQUJBQUFBQVFBQUFBRUFBQUFCQUFBQUFRQUFBQUVBQUFBQkFBQUFBUUFBQUFFQUFBQUJBQUFBQVFBQUFBRUFBQUFCQUFBQUFBQUFBVUFBQUFEQUFBQUxBQUFBQVFBQUFIU0FBRUFBQUFBQU13QUF3QUJBQUFBTEFBREFBb0FBQUhTQUFRQW9BQUFBQndBRUFBREFBd0FlT1lENWdibURlWVM1aFRtSCtaSjVrdm1jT2JNNXVqbTYvLy9BQUFBZU9ZQzVnYm1EZVlTNWhUbUgrWko1a3ZtY09iTTV1Zm02di8vQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQVFBY0FCd0FIZ0FlQUI0QUhnQWVBQjRBSGdBZUFCNEFIZ0FnQUFBQUFRQU9BQWtBQkFBRkFBY0FBd0FJQUJBQURRQUtBQVlBQWdBUEFBc0FEQUFBQVFZQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUVBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBREFBQUFBQUEwQUFBQUFBQUFBQVFBQUFBZUFBQUFIZ0FBQUFCQUFEbUFnQUE1Z0lBQUFBT0FBRG1Bd0FBNWdNQUFBQUpBQURtQmdBQTVnWUFBQUFFQUFEbURRQUE1ZzBBQUFBRkFBRG1FZ0FBNWhJQUFBQUhBQURtRkFBQTVoUUFBQUFEQUFEbUh3QUE1aDhBQUFBSUFBRG1TUUFBNWtrQUFBQVFBQURtU3dBQTVrc0FBQUFOQUFEbWNBQUE1bkFBQUFBS0FBRG16QUFBNXN3QUFBQUdBQURtNXdBQTV1Y0FBQUFDQUFEbTZBQUE1dWdBQUFBUEFBRG02Z0FBNXVvQUFBQUxBQURtNndBQTV1c0FBQUFNQUFBQUFBQUFBSFlBdGdEaUFTSUJSQUlvQW00Q3hBTUdBendEamdQQUEvUUVMZ1RNQlBvQUJRQUEvK0VEdkFNWUFCTUFLQUF4QUVRQVVBQUFBUVlyQVNJT0FoMEJJU2MwTGdJckFSVWhCUlVYRkE0REp5TW5JUWNqSWk0RFBRRVhJZ1lVRmpJMk5DWVhCZ2NHRHdFT0FSNEJNeUV5TmljdUFpY0JOVFErQWpzQk1oWWRBUUVaR3hwVEVpVWNFZ09RQVFvWUp4NkYva29Db2dFVkh5TWNEejR0L2trc1B4UXlJQk1JZHd3U0Voa1NFb3dJQmdVRkNBSUNCQThPQVcwWEZna0ZDUW9HL3FRRkR4b1Z2QjhwQWg4QkRCa25Ha3haRFNBYkVtR0lORzRkSlJjSkFRR0FnQUVUR3lBT3B6OFJHaEVSR2hGOEdoWVRFaGtIRUEwSUdCb05JeVFVQVhma0N4Z1REQjBtNHdBQUFBQUVBQUFBQUFRQUF3QUFEd0FaQUIwQUp3QUFFeUV5RmhVUkZBWWpJU0ltTlJFME5oTVJJeUlHRlJFVUZqTWhFU0VSQVNNUk16STJOUkUwSm9BREFEVkxTelg5QURWTFMrQ3JFaGtaRWdJQS93QUNBS3VyRWhrWkF3QkxOZjRBTlV0TE5RSUFOVXY5VlFKV0dSTCtBQklaQWxiOXFnSlcvYW9aRWdJQUVoa0FBQU1BQUFBQUE0d0MzZ0FQQUJNQUZ3QUFBU0VpQmdjUkhnRXpJVEkyTnhFdUFRTWhFU2tCTXhFakEzYjlGQWtNQVFFTUNRTHNDUXdCQVF3aS9VWUN1djR2TGk0QzNRMEovWElKRFEwSkFvNEpEZjEwQWw3OW9nQUVBQUQvbEFQc0Eyd0FBZ0FEQUJNQUl3QUFBU0VKQVJNaERnRUhFUjRCRnlFK0FUY1JMZ0VURkFZaklTSW1OUkUwTmpNaE1oWVZBMTc5UkFGZUFWNGcvUVF2UFFJQ1BTOEMvQzg5QWdJOUNCNFovUVFaSGg0WkF2d1pIZ0pGL253QmhBRW5BajB2L1FRdlBRSUNQUzhDL0M4OS9KZ1pIaDRaQXZ3WkhoNFpBQU1BQVArQUJBQURnQUFEQUFjQURRQUFHUUVoRVFNaEVTRUhDUUUzRndFRUFFRDhnQU9BUVA0Zy91Qmd3QUdBQTREOEFBUUEvRUFEZ09EK0lBRWdZTUFCZ0FBQUN3QUEvNEFFQUFPQUFBc0FGd0FzQURvQVNnQk9BRjRBYVFCNUFJUUFtZ0FBQlM0Qkp6NEJOeDRCRnc0QkF3NEJCeDRCRno0Qk55NEJFeThCSmljbVBRRTBOanNCTWhZZEFSY2VBUTRCQXk0Qkt3RTFNeDRCRnhFdUFTOEJJeUltUFFFME5qc0JNaFlkQVJRR0pTRVZJUWNqSWlZOUFUUTJPd0V5RmgwQkZBWURJUVlISVNJbVBRRTBOZ00xTkRZeklUSVdIUUVVQmlNaElpWVhMZ0U5QVRRMk15RUdCd0VSSGdFWElSNEJGd1V1QVNjUlBnRTNNeFVqSWdZQzgzS1lBd09ZY25LWUF3T1ljbHg3QWdKN1hGMTdBZ0o3RVlVSUJBSUJEZ3NDQ2c1eENRZ0hFVkFCSFJVME5DczZBUTBaRFprQ0NnNE9DZ0lMRGc3K2RBRk4vck15QWdzT0Rnc0NDZzRPUHdFd0NnWCszd3NPRGc0T0NnSWRDZzRPQ3YzakNnNFpDdzRPQ3dHdEloditLZ0VkRlFGbkJnc0kvb0FyT2dFQk9pczBOQlVkZ0FPWWNuS1hBd09YY25LWUFlTUNlMXhkZXdJQ2UxMWNlLzdRTkFVRkJ3TURnZ29PRGdweExBUVRGQWdDbkJZZE13RTZLLzdtQlJJRDVnNExtd3NPRGd1YkN3NkFNMDBPQzVzTERnNExtd3NPL2swWkdnNEtBZ3NPQVJnRENnNE9DZ01LRGc2b0FRMExBZ29PRlI0QlovMW1GaHdCRWhZTEFRSTVMQUthS3pvQk14MEFBQUFBQXdBQS82UUQzQU5jQUFzQUZ3QWpBQUFCQmdBSEZnQVhOZ0EzSmdBRExnRW5QZ0UzSGdFWERnRUREZ0VISGdFWFBnRTNMZ0VDQU1yKzh3VUZBUTNLeWdFTkJRWCs4OHF0NWdRRTVxMnQ1Z1FFNXExSVlRSUNZVWhJWVFJQ1lRTmNCZjd6eXNyKzh3VUZBUTNLeWdFTi9KSUU1cTJ0NWdRRTVxMnQ1Z0krQW1GSVNHRUNBbUZJU0dFQUFBQUdBQUQvdndQREF6OEFBUUFGQUE4QUhRQXFBRGNBQUJjeEFSRWhFU1VoRVJRV0Z5RStBVGNCTlNFVk16VXVBU2NoRGdFZEFTVVVIUUV6TlM0Qkp5RVZNelVqRkIwQk16VXVBU2NoRlRNMWZ3TUUvUHdEUlB4OEpSc0RCQnNrQWZ5OEFRRkFBU1FiL3Y4Y0pBTkVRQUVrRy83M1FEQkFBU1FiL3ZkQUFRSkEvY0FDUUVEOWdCc2tBUUVrR3dKbW1wYVdHeVFCQVNRYm1sNEdKeTViR3lRQm5sNEdKeTViR3lRQm5sNEFBZ0FBQUFBRDF3S0pBQmNBSndBQUpTRUdMZ0kzTlNZK0FoY2hOaDRDQnhVV0RnSUJKZ1lYRlFZV055RVdOaWMxTmlZSEEwbjliaHcxS0JVQ0FoVW9OUndDa2h3MUtCVUNBaFVvTmYxU0hDZ0RBeWdjQXBJY0tBTURLQng1QVJRcE5CMzBIVFFwRkFFQkZDazBIZlFkTkNrVUFjUUNLQnowSENnQ0FpZ2M5QndvQWdBQUFnQUEvL29Ea2dNakFBc0FIUUFBQVE0QkJ4NEJGejRCTnk0QkF5SXZBU1kwTmpJZkFRRTJNaFlVQndFR0FmMnI1QVVFNDYycjVRUUU1ZUVNQ0tJSUVSWUpqZ0VKQ1JZUkNmN2tDQU1pQk9PdHErUUZCT1NzcStYOXVnbWlDUllSQ0k4QkNRa1NGZ24rNUFrQUJRQUEvNkFENFFOaEFBTUFFd0FlQUNvQU1nQUFGeUVSSVNNME5qTWhNaFlWRVJRR0l5RWlKalVsQmk0QlB3RStBUjRCRHdFT0FTWTJQd0UrQVJZR0J3RVZNeEV6RVRNMVNBTncvSkFvR0JBRGNCQVlHQkQ4a0JBWUF1c0hGQVlHRWdVT0RBRUZaUWdTREFJSVJBY1REQUlJL2M5cE1HZzRBM0FRR0JnUS9KQVFHQmdRZGdjRUV3Z1VCZ0VLRGdZRkNBVUxFd2hOQ0FRTEVna0IwQ24rN3dFUktRQURBQUQvb0FQaEEyRUFBd0FUQUI4QUFCY2hFU0VqTkRZeklUSVdGUkVVQmlNaElpWTFBVE0xSVJVekVTTVZJVFVqU0FOdy9KQW9HQkFEY0JBWUdCRDhrQkFZQWdoNC91aDRlQUVZZURnRGNCQVlHQkQ4a0JBWUdCQUNXQ2dvL3NBb0tBQUFBd0FBQUFBRGdBTUFBQVlBRFFBZEFBQTNJUkVoRVJRV0pSRWhFU0V5TmhNUkZBWWpJU0ltTlJFME5qTWhNaGFRQVREK3dBa0N0LzdBQVRBSENVQXZJZjFnSVM4dklRS2dJUzlBQWtEOTBBY0pFQUl3L2NBSkFtZjlvQ0V2THlFQ1lDRXZMd0FBQUFBRkFBRC93d1FBQXowQUR3QVRBQmNBR3dBZkFBQUJJUTRCRlJFVUZoY2hQZ0UxRVRRbUFTTVJNeE1qRVRNVEl4RXpFeU1STXdQWS9GQVJGeGNSQTdBUkZ4ZjgvcGVYOGFHaDhhR2g1NWVYQXowQkZoSDgxaEVXQVFFV0VRTXFFUmI4MXdMYS9TWUMydjBtQXRyOUpnTGFBQUFBQUFNQUFQK01BL1FEZEFBTEFEMEFaZ0FBQVFZQUJ4WUFGellBTnlZQUF3WUhJaThESmk4QkppOEJKaThCSmk4QkxnRTFJemNYSXhRV0h3RVdGek1XSHdFV0h3UVdOelllQVFZSE55Y3pOQ1luTVNZdkFTWXZBU1l2QVNJakpnY0dMZ0UyTnpZN0FUSWZBeFlmQWg0QkZUTUNBTlgrNWdVRkFSclYxQUViQlFYKzVrbEFTd3dORWh3YkNRa0NIaGdDQndjREJRUUJGQmN3VFU0d0ZSTUNCUVlCRWhnUkJnY0ZFaElQT3pFTEZ3NEVDa3BPTVJZVUhTa0JCd2NFQmdjWENBZzdNUW9YRGdRS1Awc0ZDUW9USEJzMUpRd0JGUll4QTNRRi91WFUxUDdsQlFVQkd0WFZBUnI5VVN3QkFnSUhDZ1FGQVJBWkFRY0lCUVVHQWgxR0ozUjBJVHNZQWdjR0V3MElBd0lCQlFJQkFTSUhCQlFYQ0ZOMUlUd1lJaElCQXdJQ0FRSUVBU0lIQkJVWEJ5d0JBd2NKRnkwUUFoMUdKZ0FBQUFBQkFBRC9tZ01vQTJZQUdRQUFBUTRCQnhFZUFSY2hQZ0UzTlNjVklSRWhFUmNSTGdFbklnY0JRaWs0QVFFM0tnR0VLVGNCWWY1OEFZUmhBVFlxR0tvRFpRRTNLZnozS1RjQkFUY3BabUxIQXduOXZtSUNwQ2szQVFFQUFBQUFFZ0RlQUFFQUFBQUFBQUFBRlFBQUFBRUFBQUFBQUFFQUJBQVZBQUVBQUFBQUFBSUFCd0FaQUFFQUFBQUFBQU1BQkFBZ0FBRUFBQUFBQUFRQUJBQWtBQUVBQUFBQUFBVUFDd0FvQUFFQUFBQUFBQVlBQkFBekFBRUFBQUFBQUFvQUt3QTNBQUVBQUFBQUFBc0FFd0JpQUFNQUFRUUpBQUFBS2dCMUFBTUFBUVFKQUFFQUNBQ2ZBQU1BQVFRSkFBSUFEZ0NuQUFNQUFRUUpBQU1BQ0FDMUFBTUFBUVFKQUFRQUNBQzlBQU1BQVFRSkFBVUFGZ0RGQUFNQUFRUUpBQVlBQ0FEYkFBTUFBUVFKQUFvQVZnRGpBQU1BQVFRSkFBc0FKZ0U1Q2tOeVpXRjBaV1FnWW5rZ2FXTnZibVp2Ym5RS1ptOXliVkpsWjNWc1lYSm1iM0p0Wm05eWJWWmxjbk5wYjI0Z01TNHdabTl5YlVkbGJtVnlZWFJsWkNCaWVTQnpkbWN5ZEhSbUlHWnliMjBnUm05dWRHVnNiRzhnY0hKdmFtVmpkQzVvZEhSd09pOHZabTl1ZEdWc2JHOHVZMjl0QUFvQVF3QnlBR1VBWVFCMEFHVUFaQUFnQUdJQWVRQWdBR2tBWXdCdkFHNEFaZ0J2QUc0QWRBQUtBR1lBYndCeUFHMEFVZ0JsQUdjQWRRQnNBR0VBY2dCbUFHOEFjZ0J0QUdZQWJ3QnlBRzBBVmdCbEFISUFjd0JwQUc4QWJnQWdBREVBTGdBd0FHWUFid0J5QUcwQVJ3QmxBRzRBWlFCeUFHRUFkQUJsQUdRQUlBQmlBSGtBSUFCekFIWUFad0F5QUhRQWRBQm1BQ0FBWmdCeUFHOEFiUUFnQUVZQWJ3QnVBSFFBWlFCc0FHd0Fid0FnQUhBQWNnQnZBR29BWlFCakFIUUFMZ0JvQUhRQWRBQndBRG9BTHdBdkFHWUFid0J1QUhRQVpRQnNBR3dBYndBdUFHTUFid0J0QUFBQUFBSUFBQUFBQUFBQUNnQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBRVFFQ0FRTUJCQUVGQVFZQkJ3RUlBUWtCQ2dFTEFRd0JEUUVPQVE4QkVBRVJBUklBQVhnRU0yTnZiQXBqZFhOMGIyMHRZMjlzQ0dSeWIzQmtiM2R1Q0dOb1pXTnJZbTk0Q0dSaGRHVjBhVzFsQlhKaFpHbHZBM1JoWWdaa1lXNTVaUzBHYzNWaWJXbDBDSFJsZUhSaGNtVmhCM1JsZUhSaWIzZ0VNbU52YkFRMFkyOXNCWEpsYzJWMEJERmpiMndBQUFBQVwiIiwibW9kdWxlLmV4cG9ydHMgPSBcImRhdGE6YXBwbGljYXRpb24veC1mb250LXR0ZjtiYXNlNjQsQUFFQUFBQUxBSUFBQXdBd1IxTlZRckQrcyswQUFBRTRBQUFBUWs5VEx6Slc3a2pjQUFBQmZBQUFBRlpqYldGd3o2dlZKUUFBQWhnQUFBS2laMng1WmdYa1oxOEFBQVRnQUFBSjlHaGxZV1FQUHVsakFBQUE0QUFBQURab2FHVmhCOTREa2dBQUFMd0FBQUFrYUcxMGVFUHBBQUFBQUFIVUFBQUFSR3h2WTJFVVhoYm9BQUFFdkFBQUFDUnRZWGh3QVNZQXB3QUFBUmdBQUFBZ2JtRnRaVFVwK3JNQUFBN1VBQUFDUFhCdmMzUmtQSUlXQUFBUkZBQUFBTElBQVFBQUE0RC9nQUJjQkFBQUFBQUFCQUFBQVFBQUFBQUFBQUFBQUFBQUFBQUFBQkVBQVFBQUFBRUFBSTlvWXdaZkR6ejFBQXNFQUFBQUFBRFdEOUt6QUFBQUFOWVAwck1BQVArQUJBQURnQUFBQUFnQUFnQUFBQUFBQUFBQkFBQUFFUUNiQUFzQUFBQUFBQUlBQUFBS0FBb0FBQUQvQUFBQUFBQUFBQUVBQUFBS0FCNEFMQUFCUkVaTVZBQUlBQVFBQUFBQUFBQUFBUUFBQUFGc2FXZGhBQWdBQUFBQkFBQUFBUUFFQUFRQUFBQUJBQWdBQVFBR0FBQUFBUUFBQUFBQUFRUC9BWkFBQlFBSUFva0N6QUFBQUk4Q2lRTE1BQUFCNndBeUFRZ0FBQUlBQlFNQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFVR1pGWkFCQUFIam02d09BLzRBQVhBT0FBSUFBQUFBQkFBQUFBQUFBQkFBQUFBUHBBQUFFQUFBQUJBQUFBQVFBQUFBRUFBQUFCQUFBQUFRQUFBQUVBQUFBQkFBQUFBUUFBQUFFQUFBQUJBQUFBQVFBQUFBRUFBQUFCQUFBQUFRQUFBQUFBQUFGQUFBQUF3QUFBQ3dBQUFBRUFBQUIwZ0FCQUFBQUFBRE1BQU1BQVFBQUFDd0FBd0FLQUFBQjBnQUVBS0FBQUFBY0FCQUFBd0FNQUhqbUErWUc1ZzNtRXVZVTVoL21TZVpMNW5EbXpPYm81dXYvL3dBQUFIam1BdVlHNWczbUV1WVU1aC9tU2VaTDVuRG16T2JuNXVyLy93QUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBRUFIQUFjQUI0QUhnQWVBQjRBSGdBZUFCNEFIZ0FlQUI0QUlBQUFBQUVBRGdBSkFBUUFCUUFIQUFNQUNBQVFBQTBBQ2dBR0FBSUFEd0FMQUF3QUFBRUdBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQkFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBd0FBQUFBQU5BQUFBQUFBQUFBRUFBQUFIZ0FBQUI0QUFBQUFRQUE1Z0lBQU9ZQ0FBQUFEZ0FBNWdNQUFPWURBQUFBQ1FBQTVnWUFBT1lHQUFBQUJBQUE1ZzBBQU9ZTkFBQUFCUUFBNWhJQUFPWVNBQUFBQndBQTVoUUFBT1lVQUFBQUF3QUE1aDhBQU9ZZkFBQUFDQUFBNWtrQUFPWkpBQUFBRUFBQTVrc0FBT1pMQUFBQURRQUE1bkFBQU9ad0FBQUFDZ0FBNXN3QUFPYk1BQUFBQmdBQTV1Y0FBT2JuQUFBQUFnQUE1dWdBQU9ib0FBQUFEd0FBNXVvQUFPYnFBQUFBQ3dBQTV1c0FBT2JyQUFBQURBQUFBQUFBQUFCMkFMWUE0Z0VpQVVRQ0tBSnVBc1FEQmdNOEE0NER3QVAwQkM0RXpBVDZBQVVBQVAvaEE3d0RHQUFUQUNnQU1RQkVBRkFBQUFFR0t3RWlEZ0lkQVNFbk5DNENLd0VWSVFVVkZ4UU9BeWNqSnlFSEl5SXVBejBCRnlJR0ZCWXlOalFtRndZSEJnOEJEZ0VlQVRNaE1qWW5MZ0luQVRVMFBnSTdBVElXSFFFQkdSc2FVeElsSEJJRGtBRUtHQ2NlaGY1S0FxSUJGUjhqSEE4K0xmNUpMRDhVTWlBVENIY01FaElaRWhLTUNBWUZCUWdDQWdRUERnRnRGeFlKQlFrS0J2NmtCUThhRmJ3ZktRSWZBUXdaSnhwTVdRMGdHeEpoaURSdUhTVVhDUUVCZ0lBQkV4c2dEcWMvRVJvUkVSb1JmQm9XRXhJWkJ4QU5DQmdhRFNNa0ZBRjM1QXNZRXd3ZEp1TUFBQUFBQkFBQUFBQUVBQU1BQUE4QUdRQWRBQ2NBQUJNaE1oWVZFUlFHSXlFaUpqVVJORFlURVNNaUJoVVJGQll6SVJFaEVRRWpFVE15TmpVUk5DYUFBd0ExUzBzMS9RQTFTMHZncXhJWkdSSUNBUDhBQWdDcnF4SVpHUU1BU3pYK0FEVkxTelVDQURWTC9WVUNWaGtTL2dBU0dRSlcvYW9DVnYycUdSSUNBQklaQUFBREFBQUFBQU9NQXQ0QUR3QVRBQmNBQUFFaElnWUhFUjRCTXlFeU5qY1JMZ0VESVJFcEFUTVJJd04yL1JRSkRBRUJEQWtDN0FrTUFRRU1JdjFHQXJyK0x5NHVBdDBOQ2YxeUNRME5DUUtPQ1EzOWRBSmUvYUlBQkFBQS81UUQ3QU5zQUFJQUF3QVRBQ01BQUFFaENRRVRJUTRCQnhFZUFSY2hQZ0UzRVM0QkV4UUdJeUVpSmpVUk5EWXpJVElXRlFOZS9VUUJYZ0ZlSVAwRUx6MENBajB2QXZ3dlBRSUNQUWdlR2YwRUdSNGVHUUw4R1I0Q1JmNThBWVFCSndJOUwvMEVMejBDQWowdkF2d3ZQZnlZR1I0ZUdRTDhHUjRlR1FBREFBRC9nQVFBQTRBQUF3QUhBQTBBQUJrQklSRURJUkVoQndrQk54Y0JCQUJBL0lBRGdFRCtJUDdnWU1BQmdBT0EvQUFFQVB4QUE0RGcvaUFCSUdEQUFZQUFBQXNBQVArQUJBQURnQUFMQUJjQUxBQTZBRW9BVGdCZUFHa0FlUUNFQUpvQUFBVXVBU2MrQVRjZUFSY09BUU1PQVFjZUFSYytBVGN1QVJNdkFTWW5KajBCTkRZN0FUSVdIUUVYSGdFT0FRTXVBU3NCTlRNZUFSY1JMZ0V2QVNNaUpqMEJORFk3QVRJV0hRRVVCaVVoRlNFSEl5SW1QUUUwTmpzQk1oWWRBUlFHQXlFR0J5RWlKajBCTkRZRE5UUTJNeUV5RmgwQkZBWWpJU0ltRnk0QlBRRTBOak1oQmdjQkVSNEJGeUVlQVJjRkxnRW5FVDRCTnpNVkl5SUdBdk55bUFNRG1ISnltQU1EbUhKY2V3SUNlMXhkZXdJQ2V4R0ZDQVFDQVE0TEFnb09jUWtJQnhGUUFSMFZORFFyT2dFTkdRMlpBZ29PRGdvQ0N3NE8vblFCVGY2ek1nSUxEZzRMQWdvT0RqOEJNQW9GL3Q4TERnNE9EZ29DSFFvT0Rncjk0d29PR1FzT0Rnc0JyU0liL2lvQkhSVUJad1lMQ1A2QUt6b0JBVG9yTkRRVkhZQURtSEp5bHdNRGwzSnltQUhqQW50Y1hYc0NBbnRkWEh2KzBEUUZCUWNEQTRJS0RnNEtjU3dFRXhRSUFwd1dIVE1CT2l2KzVnVVNBK1lPQzVzTERnNExtd3NPZ0ROTkRndWJDdzRPQzVzTER2NU5HUm9PQ2dJTERnRVlBd29PRGdvRENnNE9xQUVOQ3dJS0RoVWVBV2Y5WmhZY0FSSVdDd0VDT1N3Q21pczZBVE1kQUFBQUFBTUFBUCtrQTl3RFhBQUxBQmNBSXdBQUFRWUFCeFlBRnpZQU55WUFBeTRCSno0Qk54NEJGdzRCQXc0QkJ4NEJGejRCTnk0QkFnREsvdk1GQlFFTnlzb0JEUVVGL3ZQS3JlWUVCT2F0cmVZRUJPYXRTR0VDQW1GSVNHRUNBbUVEWEFYKzg4cksvdk1GQlFFTnlzb0JEZnlTQk9hdHJlWUVCT2F0cmVZQ1BnSmhTRWhoQWdKaFNFaGhBQUFBQmdBQS83OER3d00vQUFFQUJRQVBBQjBBS2dBM0FBQVhNUUVSSVJFbElSRVVGaGNoUGdFM0FUVWhGVE0xTGdFbklRNEJIUUVsRkIwQk16VXVBU2NoRlRNMUl4UWRBVE0xTGdFbklSVXpOWDhEQlB6OEEwVDhmQ1ViQXdRYkpBSDh2QUVCUUFFa0cvNy9IQ1FEUkVBQkpCdis5MEF3UUFFa0cvNzNRQUVDUVAzQUFrQkEvWUFiSkFFQkpCc0NacHFXbGhza0FRRWtHNXBlQmljdVd4c2tBWjVlQmljdVd4c2tBWjVlQUFJQUFBQUFBOWNDaVFBWEFDY0FBQ1VoQmk0Q056VW1QZ0lYSVRZZUFnY1ZGZzRDQVNZR0Z4VUdGamNoRmpZbk5UWW1Cd05KL1c0Y05TZ1ZBZ0lWS0RVY0FwSWNOU2dWQWdJVktEWDlVaHdvQXdNb0hBS1NIQ2dEQXlnY2VRRVVLVFFkOUIwMEtSUUJBUlFwTkIzMEhUUXBGQUhFQWlnYzlCd29BZ0lvSFBRY0tBSUFBQUlBQVAvNkE1SURJd0FMQUIwQUFBRU9BUWNlQVJjK0FUY3VBUU1pTHdFbU5EWXlId0VCTmpJV0ZBY0JCZ0g5cStRRkJPT3RxK1VFQk9YaERBaWlDQkVXQ1k0QkNRa1dFUW4rNUFnRElnVGpyYXZrQlFUa3JLdmwvYm9Kb2drV0VRaVBBUWtKRWhZSi91UUpBQVVBQVArZ0ErRURZUUFEQUJNQUhnQXFBRElBQUJjaEVTRWpORFl6SVRJV0ZSRVVCaU1oSWlZMUpRWXVBVDhCUGdFZUFROEJEZ0VtTmo4QlBnRVdCZ2NCRlRNUk14RXpOVWdEY1B5UUtCZ1FBM0FRR0JnUS9KQVFHQUxyQnhRR0JoSUZEZ3dCQldVSUVnd0NDRVFIRXd3Q0NQM1BhVEJvT0FOd0VCZ1lFUHlRRUJnWUVIWUhCQk1JRkFZQkNnNEdCUWdGQ3hNSVRRZ0VDeElKQWRBcC91OEJFU2tBQXdBQS82QUQ0UU5oQUFNQUV3QWZBQUFYSVJFaEl6UTJNeUV5RmhVUkZBWWpJU0ltTlFFek5TRVZNeEVqRlNFMUkwZ0RjUHlRS0JnUUEzQVFHQmdRL0pBUUdBSUllUDdvZUhnQkdIZzRBM0FRR0JnUS9KQVFHQmdRQWxnb0tQN0FLQ2dBQUFNQUFBQUFBNEFEQUFBR0FBMEFIUUFBTnlFUklSRVVGaVVSSVJFaE1qWVRFUlFHSXlFaUpqVVJORFl6SVRJV2tBRXcvc0FKQXJmK3dBRXdCd2xBTHlIOVlDRXZMeUVDb0NFdlFBSkEvZEFIQ1JBQ01QM0FDUUpuL2FBaEx5OGhBbUFoTHk4QUFBQUFCUUFBLzhNRUFBTTlBQThBRXdBWEFCc0FId0FBQVNFT0FSVVJGQllYSVQ0Qk5SRTBKZ0VqRVRNVEl4RXpFeU1STXhNakVUTUQyUHhRRVJjWEVRT3dFUmNYL1A2WGwvR2hvZkdob2VlWGx3TTlBUllSL05ZUkZnRUJGaEVES2hFVy9OY0MydjBtQXRyOUpnTGEvU1lDMmdBQUFBQURBQUQvakFQMEEzUUFDd0E5QUdZQUFBRUdBQWNXQUJjMkFEY21BQU1HQnlJdkF5WXZBU1l2QVNZdkFTWXZBUzRCTlNNM0Z5TVVGaDhCRmhjekZoOEJGaDhFRmpjMkhnRUdCemNuTXpRbUp6RW1Md0VtTHdFbUx3RWlJeVlIQmk0Qk5qYzJPd0V5SHdNV0h3SWVBUlV6QWdEVi91WUZCUUVhMWRRQkd3VUYvdVpKUUVzTURSSWNHd2tKQWg0WUFnY0hBd1VFQVJRWE1FMU9NQlVUQWdVR0FSSVlFUVlIQlJJU0R6c3hDeGNPQkFwS1RqRVdGQjBwQVFjSEJBWUhGd2dJT3pFS0Z3NEVDajlMQlFrS0V4d2JOU1VNQVJVV01RTjBCZjdsMU5UKzVRVUZBUnJWMVFFYS9WRXNBUUlDQndvRUJRRVFHUUVIQ0FVRkJnSWRSaWQwZENFN0dBSUhCaE1OQ0FNQ0FRVUNBUUVpQndRVUZ3aFRkU0U4R0NJU0FRTUNBZ0VDQkFFaUJ3UVZGd2NzQVFNSENSY3RFQUlkUmlZQUFBQUFBUUFBLzVvREtBTm1BQmtBQUFFT0FRY1JIZ0VYSVQ0Qk56VW5GU0VSSVJFWEVTNEJKeUlIQVVJcE9BRUJOeW9CaENrM0FXSCtmQUdFWVFFMktoaXFBMlVCTnluODl5azNBUUUzS1daaXh3TUovYjVpQXFRcE53RUJBQUFBQUJJQTNnQUJBQUFBQUFBQUFCVUFBQUFCQUFBQUFBQUJBQVFBRlFBQkFBQUFBQUFDQUFjQUdRQUJBQUFBQUFBREFBUUFJQUFCQUFBQUFBQUVBQVFBSkFBQkFBQUFBQUFGQUFzQUtBQUJBQUFBQUFBR0FBUUFNd0FCQUFBQUFBQUtBQ3NBTndBQkFBQUFBQUFMQUJNQVlnQURBQUVFQ1FBQUFDb0FkUUFEQUFFRUNRQUJBQWdBbndBREFBRUVDUUFDQUE0QXB3QURBQUVFQ1FBREFBZ0F0UUFEQUFFRUNRQUVBQWdBdlFBREFBRUVDUUFGQUJZQXhRQURBQUVFQ1FBR0FBZ0Eyd0FEQUFFRUNRQUtBRllBNHdBREFBRUVDUUFMQUNZQk9RcERjbVZoZEdWa0lHSjVJR2xqYjI1bWIyNTBDbVp2Y20xU1pXZDFiR0Z5Wm05eWJXWnZjbTFXWlhKemFXOXVJREV1TUdadmNtMUhaVzVsY21GMFpXUWdZbmtnYzNabk1uUjBaaUJtY205dElFWnZiblJsYkd4dklIQnliMnBsWTNRdWFIUjBjRG92TDJadmJuUmxiR3h2TG1OdmJRQUtBRU1BY2dCbEFHRUFkQUJsQUdRQUlBQmlBSGtBSUFCcEFHTUFid0J1QUdZQWJ3QnVBSFFBQ2dCbUFHOEFjZ0J0QUZJQVpRQm5BSFVBYkFCaEFISUFaZ0J2QUhJQWJRQm1BRzhBY2dCdEFGWUFaUUJ5QUhNQWFRQnZBRzRBSUFBeEFDNEFNQUJtQUc4QWNnQnRBRWNBWlFCdUFHVUFjZ0JoQUhRQVpRQmtBQ0FBWWdCNUFDQUFjd0IyQUdjQU1nQjBBSFFBWmdBZ0FHWUFjZ0J2QUcwQUlBQkdBRzhBYmdCMEFHVUFiQUJzQUc4QUlBQndBSElBYndCcUFHVUFZd0IwQUM0QWFBQjBBSFFBY0FBNkFDOEFMd0JtQUc4QWJnQjBBR1VBYkFCc0FHOEFMZ0JqQUc4QWJRQUFBQUFDQUFBQUFBQUFBQW9BQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFCRUJBZ0VEQVFRQkJRRUdBUWNCQ0FFSkFRb0JDd0VNQVEwQkRnRVBBUkFCRVFFU0FBRjRCRE5qYjJ3S1kzVnpkRzl0TFdOdmJBaGtjbTl3Wkc5M2JnaGphR1ZqYTJKdmVBaGtZWFJsZEdsdFpRVnlZV1JwYndOMFlXSUdaR0Z1ZVdVdEJuTjFZbTFwZEFoMFpYaDBZWEpsWVFkMFpYaDBZbTk0QkRKamIyd0VOR052YkFWeVpYTmxkQVF4WTI5c0FBQUFBQT09XCIiLCIvLyBzdHlsZS1sb2FkZXI6IEFkZHMgc29tZSBjc3MgdG8gdGhlIERPTSBieSBhZGRpbmcgYSA8c3R5bGU+IHRhZ1xuXG4vLyBsb2FkIHRoZSBzdHlsZXNcbnZhciBjb250ZW50ID0gcmVxdWlyZShcIiEhLi4vLi4vLi4vbm9kZV9tb2R1bGVzL2Nzcy1sb2FkZXIvaW5kZXguanMhLi9ib290c3RyYXAtZGF0ZXRpbWVwaWNrZXIuY3NzXCIpO1xuaWYodHlwZW9mIGNvbnRlbnQgPT09ICdzdHJpbmcnKSBjb250ZW50ID0gW1ttb2R1bGUuaWQsIGNvbnRlbnQsICcnXV07XG4vLyBhZGQgdGhlIHN0eWxlcyB0byB0aGUgRE9NXG52YXIgdXBkYXRlID0gcmVxdWlyZShcIiEuLi8uLi8uLi9ub2RlX21vZHVsZXMvc3R5bGUtbG9hZGVyL2FkZFN0eWxlcy5qc1wiKShjb250ZW50LCB7fSk7XG5pZihjb250ZW50LmxvY2FscykgbW9kdWxlLmV4cG9ydHMgPSBjb250ZW50LmxvY2Fscztcbi8vIEhvdCBNb2R1bGUgUmVwbGFjZW1lbnRcbmlmKG1vZHVsZS5ob3QpIHtcblx0Ly8gV2hlbiB0aGUgc3R5bGVzIGNoYW5nZSwgdXBkYXRlIHRoZSA8c3R5bGU+IHRhZ3Ncblx0aWYoIWNvbnRlbnQubG9jYWxzKSB7XG5cdFx0bW9kdWxlLmhvdC5hY2NlcHQoXCIhIS4uLy4uLy4uL25vZGVfbW9kdWxlcy9jc3MtbG9hZGVyL2luZGV4LmpzIS4vYm9vdHN0cmFwLWRhdGV0aW1lcGlja2VyLmNzc1wiLCBmdW5jdGlvbigpIHtcblx0XHRcdHZhciBuZXdDb250ZW50ID0gcmVxdWlyZShcIiEhLi4vLi4vLi4vbm9kZV9tb2R1bGVzL2Nzcy1sb2FkZXIvaW5kZXguanMhLi9ib290c3RyYXAtZGF0ZXRpbWVwaWNrZXIuY3NzXCIpO1xuXHRcdFx0aWYodHlwZW9mIG5ld0NvbnRlbnQgPT09ICdzdHJpbmcnKSBuZXdDb250ZW50ID0gW1ttb2R1bGUuaWQsIG5ld0NvbnRlbnQsICcnXV07XG5cdFx0XHR1cGRhdGUobmV3Q29udGVudCk7XG5cdFx0fSk7XG5cdH1cblx0Ly8gV2hlbiB0aGUgbW9kdWxlIGlzIGRpc3Bvc2VkLCByZW1vdmUgdGhlIDxzdHlsZT4gdGFnc1xuXHRtb2R1bGUuaG90LmRpc3Bvc2UoZnVuY3Rpb24oKSB7IHVwZGF0ZSgpOyB9KTtcbn0iLCJtb2R1bGUuZXhwb3J0cyA9IFwiZGF0YTppbWFnZS9wbmc7YmFzZTY0LGlWQk9SdzBLR2dvQUFBQU5TVWhFVWdBQUFRQUFBQUR3Q0FRQUFBQkZubkpBQUFBQUFtSkxSMFFBUk5zOHByc0FBQUFKY0VoWmN3QUFBRWdBQUFCSUFFYkphejRBQUJwdFNVUkJWSGphN1oxN2JHVkhmY2MvWjdOTDF0a2t2WWFXeUJaVjlpR2FQbFR0M2RnSVVxWEtkUXZOSmtoZ2IwV3BLbFd5azhndVFnMFFxVkpGS2lXaFF2MkxKQVZGN1Vaa3ZVVUNLUkRoM1lqQ2hqNXNGTlJDc0xOZXBhVlFsQWRTc1ZXVjlycnBId1lsNGZTUDg1bzVaMTdubkh0OXIrK1o3OHA3N3oyL2VmOSs4NXM1ODV2NVRmQnVQSnFNQTRNdWdNZGc0UVdnNGZBQ0lHT0NrSWxCRjJJdjRRVkF4QVJid0ZhVFJLRFhBakQ0L2pOQldEbm1GakNKVGdTU2xBZGZ4eDVDRmdDekFnelRmeWJZK284K2Zoam5QcUVOWThzN1lXSzFHa1RzMzQ1RndKVHlDT2tJVVFCNm9RQW5hNlNReEUzNllYblVpUXNCQWR2QU5nR0JJZVU2ZFJ3NkJPazZRRkxGcEIvb0VFS2hlVVJFNlFTVjRpZTlUSmUvTGUrd1p0bGRVN2JWY1I4aDB3Qm1CZWdLVXgrMER4OVI3aVlXbWxPcDF6ZE5RNUNZY2owOU0yUUlTcThFbW51UnFROW1qVnExNzloVE1QZE5jOW5OT2pCTDJhWm45aFhLdndVRUZ2YnBteVpJLzFXRlBZVnRZODgwNTUxcFAxVWR4SlJIaHYxVk5NQm9ZNEt0VVdLdkhRY0hYWUFody9ab1RPM2M0VmNDR3c0dkFBMkhGNENHd3d0QXcrRUZvT0h3QXRCd2VBRm9PUHgrZ0h6Y1FaZC9qMUZtUDREYmpnQ2JNV2JDdUIvQW5JT2RQWFgyQTlqTjRWSFpSMHBJeXUwSGNMT0FtWnRRejZCSnpYZlh0RzNzdDZXK2xmdlVwejR5dXdHSyt3RWk2RmZEazFBbWU1dWVib3Z0c2gvQVZEcHo3dmI0Wm9SeDJzbm5TRURlRDZENkxtUGJTUXVvdzlqN3A4dCtBRlAvY3l1YlhueEM2MjZETWpudEMyUUNFRWovOUxDTGdLNkpYZFR6ZHJ3dHk0d3RiZXlxWll0bzhxYys5Ukd5Ri9iYUhCd092SEVtMktxc25odG5ETzY5T1hqd0kyTWRnMjdqak1GK0lhang4QUxRY0hnQmFEaThBRFFjWGdBYURpOEFEWWNYZ0liRGJ3dlBJeHpnV2tEOXMxT2x5Myt3WHZTK05NSWdTeEJhUzFEZEZHU1BPWUNheTBOQUNFWmJ2NHQvZ0xBQ3BTeXE1bUVyZjhUNndKaUtYVGlxeG5hcmdVdW9FbkVQU0FGczFYYzUxNmVQSHpnMGtFc1ZRb081MTZWOGdZRW1mNWJMSTZIWVc4QmNRN01BaFpiWVlPdkVVdm5GSWNDMStycmRPdlpHQ25xZzRFT2p0ZDgyaXRZcFFaakdEcFJVTVhkVkxrbjMwcGNoTk5ETmFXZTFNM2NEc1pRQmxKMEVoZzY5eHhiS1JUcXI5WEUzNWdZT3Fac1k1Skp1MVJZd2RVR1h0TzF0VUtDWGVRM01WSnhMS0RYTnJBTHRFbXhPdzZaaTNWUzByUWZaY25lWlJGYUZYY1R0ZEtrRVpUU0FtK3FzTjhzTkhMVk12OG9ZR0JXc1c3cUJRNWlxTUEvVDVnRlFHZmVnSnREZ01PZ1M3T2Y4SzhUMUs0RU5oeGVBaHNNTFFNUGhCYURoOEFMUWNIZ0JhRGk4QURRYzh1SFF4Rm5xNEdEM0I5NVB1TFNBaTBXMFdqemIyV2pYWEVvaEU0RGs1SjZMdS9kcURkQTdWRXZMVm5LM0ZqQzVtczJzY1JPbFk4dUg4L1IxbUtqY0FtR3VsSUQ2Y0tqNURKK0xSWHRDOGF4M0lxQkx5ODAxaEw3OGJpMmd5Mk1pZDd4Mm9sVHNyQVkyRFZUdmFIcmhnSzU4TnRERm5icnRDTGJPbWJMYlRocDdDWFRwMkYyNDIwdnU1cEJlYlRFb3NqYkkwVzBIeXpQbXFNNG5obExLWm9PeHptQWRGTU9VblFUcXF5Q1BYOFUrRktUeHEycUNMSzRxL3kxRktmUmxxSnQ3K1ZYM0xKYXVEVngxc0E2VGltOVdsQk9BT2cxZ2IvNkViYVpMWmJKMHF1VmZWd1QwdVUvRzFHUUVWelBCbEgrUVdoTUREZFdVY25aODNYNEVYcGhubE5zUFlHb0FlUUpqcXI2NWNLWXFtdElRcTJiZjlxVmlnVjBBVFRYWXpqbVEyQzZkZ2h0TTdOMU9yNzF4S3lYbEJNQzk1OXV2akRBVnpuWmppR1BWU3RmRFJRQk5OY2dPbHdlV2EydXFJMG5aUEFpYkJzRGNFWGgvWDRDSUNldU5TU01IZnpCRWhIY1E0ZEUwZUFGb09Md0FOQnhlQUJvT0x3QU5oeGVBWG1PUTV1d0t5TzhIc01Ga3IzS3Ara1JOZTNlL1ViOTBnV1d4ZThqcVh0d1BZSUxKWXU1eUxpK3gyRTA2SEREVm8rN2hLdlB4OE1CNi9OWE9ZRjE4VStvRFFyWVNLQmJMWlNtM2FBN05qaTlXdS8zYjdkeXV5U0poVHlHTWIvOE5MTEYxNWw3VDRkSFFpZXBlMHoyQmVnNmcyM1JndGxkbFEwT1ZUUXRSejdGdm05TDdNQWh4dGZVRkpaNnE4bFhsYnovWlBKVElCRUEyRkphM1NFY1hMK3YzQTlnUWtObnlxaDJnenNMVWRVRlJMViszRUhWOGZQUUJtUUNJMXJRcU1teXp4dG1OclhWSHgwRFFJR29OSVgrcVF2U21GNnR5a0xYSDBPZ0pjUWpJVENFcUJSNHArR2k3bHhyYnh2MEFMc1pXdTNzSU03SzlBT290RmVhbXp3WWh2WGVSSkdSUWtpcUdxTHFocGk5UVd3TzNEQlYwTVphcXdtd3o2V2hzdFhuWGNKdm82ZUxYOFRCaTN0Vm52MHpHZlNEWk02Z0ZvUHdreVNWY0w0eXRMaWxVZDFIaElnSjFTemRrOEN1Qk12WWhDK3ZCQzBERDRRV2c0ZkFDMEhCNEFXZzR2QUEwSE1NbkFLMWhXU1J0QnZJQzRHYkxOcGxzUWllcUxrU0w3cDY4aWczYUQ4TFFvSnk3ZURkVHpIaU44blJKaEtSbERGZlZaWDJHRWJvQnZBNWtBVEN2VXB0WHU1TmUzYUtyRklFd1hTaFZuOTJMVlA5NEdxS3JpWjk4cStaVTNYeUN1WUhJQkVCMjkxNUVaZy9YdVV3UENHTDI3eWp6TXF2MlNQWHZNRzQ0TzV2a3JzNWYzbEdRVDhYTmZVVGprTmdDc2lZemU3eTJXYklTOWxjZngzZWN6RFVxOW90bHk1Yy8yY3lXMFVmb0N2ZzZTQVFnYVZyVGxpa1FMMDFRby82VktEcjlJZWVlTDROOEVVS3hoRnRwcUtTVWpUb0Nxa2RtRFJTYlRPOWd4SDdyaGV1MUVqcDZKQUpoWVI2UkdJTFZYdm5sOHZUS3JYd0RVT1l0SUd0aTNaNDgyNVZNSnZwNFN1MENRVUVQQkVMdXFvSEl2SjlJZGgvaGthTE1XNEI1VTVYTnlabk5kOGRPU25WNWpRd2NueVZ3ZHgvUk1MaGZHR0ViSW5vRDh4eWdEaHA0OXQ4RncrWWd3ak5wanpGOHRnQ1BQWVVYZ0liREMwREQ0UVdnNFJnbEFaaEtWeHFtK3BMK1FRN0gvNFp0Nmx3RGtRRE14ZzIzeW16bGxENWhzZlRiRUhKVzJpMVFsb2xUcktmZjE1V3hwMnFKeDBGZTV5WjIyZVVtWGxlS3dKUzE5TWRqNm5GTkhucTZMU1o4SVBmUG5FUEtwZWg0ZU1oOVhBWmFyQWlCczFleTJmVDVIQmMwU1dmbjZwWTRxMWxNRnBFUE1jOExBZ3RoZ2ZOUzQ2N253ayt6VVlJdWwxQlZ1dU84S1AwK3dVdlM3OFBjeENaandDNXR2czlQdEMyZ3pzUG1UVndPRXpoVHhCQlp5OXZxbUM2ZEo1SjhHWUExelNyY2lrSkExcGhCaHlWTkFjYlNiN3NGMmpLUG84YzZrV09KQ0Z1RXJFczVyRE10aWNCMGptN0hpMExwb2hMbTQyOHl4aEZnS2hZRVhUbGhXa0Z4UDFmVnI4T3pvb2luS1NVQ3NCbC9pdHN3eENKZkJyN1BybkdaOW5EOGVWWWI0cEFoOWdJdnNHaWd6MGt1S09Za1hRV3dJWWlBcXZmbnorY1dHWEtJVjlQdjF5dkxjSVRyQ2JqR1VNbzdEVFN6QmpTYjR3TkZLdVVFUEdIL2IvTVA0dU5zTE90YUV2Zyt1NFV3Y2hFaUFSZzNwS2JyTjJEVEFQQmNLZ0loY3p5bkNKR0lRSkg5RVZxNXoyTHBiakNXdE0yUHVRUDRHbTFsL0ExSVJVaGRndWxZVTAwWEJxeCtJMkgvN3pFbWk0RHJmRmJGL21JRGdsbFJSWDNuWlNYTnBnRVNFVUREZnVMYzBVN3lUdVErOHpqTUQrTnZOeXFvUjdoQ20wMmd6UlhlVXBnRDVDMnBWVDBBMkJXOUxjUWY4SVhDczRqOVN4d0J2Z0tRRE9EdXI0RkY5dWNyZUEzWGNBMWpYS05Wa2xkek5WY0RxaDYyYkdFL1RQQWNjOHp4bkdZNzV4VGJ6RExMZHNWNS9oaS9IUDhiSzVSdmdyY1I4aXB0Mmh3ZzVHM0dMYVhUR3VZZkZmNTBPSXdOdGhDZlZ6eUxoUDRzYitLTEFNeXdGaEdxdjlFV3F6aG1qZk05UTBpYkJwaGdHK0srdjYxby9pbUpyaEtCWDh4OTVwc282aVZ0b2htUnJDZmV5aVlJN3dXYnRBMDdpdW9vK04yYUlkUjdPbDZLNnhmTnoxTDJtd1JBbk1rV2xVNlIvWC9PQzlLdklzd3EwVHdIbUZhODVwV2hBN3cxOTVsdm9taCtjWkQxd2l0Z05ra1duK1NuYWJaSkhqd2wvQld4d0xMd3ZWb0lQVjVLUlZ4Zy96QmRHREVGL0xydyt3WE5SS3EvQ0VIQi9sNm1IbUV3WnUvanZDaXpmNWdFd0dNZ0dDVmJnRWNGZUFGb09Md0FOQnhlQUJvT0x3Q2poVS95eVhJUlpBRm9PUnpNMXNQbDVrMDNUQ2t0NnRPQ0xidjRsajlMS1AyYnpkRVhjM1RWb3BOb0xUL2VCenJBWDFqYXAwT25jcnRkeDhmNU9OZFpRczB6ejN6eVEzd05iTkZsR2xoWDdNMWY1WTU0QmVvaFZqakdpc0ppL1RnYm5BV1dtR0pSb3BlNzh6ZXg3Y3ZocC9sTy9EUks3UjI1cFovSUhwN2diQ0crcXoyK0RVUUxQNzJuSjJFQ1lGN2E3d0RRWVpWeHVzQTQzZno3T2kyNndCS1BzOGhaVUo2ZmVKWmJnVy95bTRiV25ZOFhreFpZNHhWUkFDTDJSOWEwb2dpRXNhTlhmZlBaMWdxajg0UWY1akUrekdQY3g4T2FCcHBpZzFsV0NqWTk4ZWluS2dlWDY5dS94YnU0ekNuK2lkL1ErRGhvYzRXUWdKT0ZsYjZNRGhqb0wvRWFoeml1cENmTnZ3QXNsMnpCaExxVW10dEYrbUxCQ0wra1hGbWRaWVVGTHRCTk50eUk1dUJrT1hXZDZVcU9Xc1M5QW1yTDRTMXM4UVAralIveWdzWW1PTVU2YzF6UW1uVHI0Tzk1SjVkb2M0bGIrRWR0cUdndHZXMmt5MnVXTWw1VDdCVktrUFMrWlNWMVhHbzEzYzRMOVc2TEd4MmVBSHdFR0krSHh4WTcyUnpnckxTYXZzNjBZVnVIRGp2czBLWExqdlp3MXovenUzeWRlYjdDVW1GREJ5VHNYMkdxTDh2QTcrWXBUdk5WVHZNRjVjYU5td0RUK2NpRUhoQndsWloraU1PYWpTL3pFdVB6YS9tZG1QM0o2Y2x1cWJuQS9YeEsrdjBwN2krRU9jcFJPc0RETExQQStZaEw0aHdnaEhnT29GTGc5aUVnSUJ2aFFpWDlYV3h4SzkvbW5YeWJkeGFzMWduNzFiMWZYRWRYelNraUJ6TmRZVU5LbnY0bFBzQ1RmSkFuK1NCZjVvekdoVVFiVUkzaEdmMHEzakRTMWZHenNUZkNzN2s5aUIxVzAxcEY5Wk5uQWEyY1ZpM09BVXdYK29qaUo5SHkxa0I5eitzeXh3V094cXBidng5UWgrbkNHQ2NMZ0puOWNrVnRXeUoybEUvL2hDUGN5dzNjeXpYOEdXY2syaVEvaXI5dHhwOG5la3JQMkg5ZVUrYTFlQWhJYXBabjhJNDBSQlRaM3dMZ204Q3R4T285bDM4MDlxL0owZklhUU96RitVWTNJd3JSaXB1Ly9INllxT0o2OWlkdkFRbFVid0hqcVl1YVloM01rOFJrWlA4WFkrbnEwQU5DQS90VkpWUTUwb3BFUVBVRzhCNHU4U0VlQnhiNUswN3pkeEoxbm1YdW84dHlQbTZtQWNRdGlWVVl1Q1JWWUVrWnhuWWxpNm4zci9NT1FRVGVVYkQvbitCRnVtVFR6eE1LdXZ3N0Q1dlRtRHIwQlN2N1lTYjNHbGpFamlEaWViekc3L01sQUI2bnkydUtFQThEQy9tNHJ1YmdrL0hyajBlLzBZRzhtdTRKNWdHS0l1ajNBelFjM2hiUWNIZ0JhRGk4QURRY1hnQWFqdVlKUUdRMjdpZ29uZFNVZTVORE9qcGIzejd6U0N3S3dNbTBBVTVXVHErNmY0QzZhQkZ5THY1K1RydXI0Y0hZQnJGYUVJSGZZcFV6bk9IdHZKM3Y4U3VGbUZIcmZDeitKZDUwSE9FVTd5WGtGbTRoNUwyY0tzUTN1Ni9JNzJiSTcyY28wbTBoWmd0NXlQU2tJdWxyNEVrMldlTXZnV1ZhcWRtem1JaUxLOG4rN0hxZlo1azJWempKcG1KUkpjcDVtYnM0RjYrMzY5ZkRqL0V5eFpYQ0krbjMxL21wWWkxMG1hZFpvYzJWT0IxNXJkNXVEay9zZTEybElkbTg5aG9xcklQZFhJZ2czU2l6cm5EbEd6SXV4RWpOMlprRzJHU05HUzd3UGxyc0tFN0NEQjdMd0NiemJLSXlxQ1pyN1FuN1ZTYlhvL0duMmhUOUUzN0tMcnY4bE5lVjlLZTVBSXluTnYyMTBqVTRRVGU5RXFPb0I1SjlVRG9kdXNOQ2JHdFYyMXZET0pVcDBHcmh0ekhKSkpQOFYvSWcwd0FoYzF6Z0hBdXhwSnYzcytpeUYxRkdEK2h1S0JCeFVoQkxsWVk2SjVoWWw3bXJRTS9iMC9JOUpPc014M2l4a1BzamZCU0FOVHFnMUVEaWljZGRwUWJRNXg4eUZ1KzRHbE9tRUdtQUxzUW11VmNLbCtza0dtQUtPS3QwNWgzUzVnRHdNeUE5MmlaYkF5UDIzNlgwRkJUbXZsVXo5dWlhM3dWWDByTnhDOG9CNmk1SWU3K2EvZUtlaC94YSs1UDhMTFVQdk1pamhmZ2Y0eUxKM0VHOXFuOUl1UUx2aWlQcDU0K0JuMWVFK0NnQUs4eXh3akdGRGdqSmpxVVdPOVFVOE4vQXp6akFML0E4RDBXUFJRMndReXR1dWxVNkZiWjhZUXpodHVISmxJTk5BM3lPUDB5LzUvZkZ5ZXhYTWZBa0h4RitmWUpYRExtckJDenFvMjhBOEpwV0EraThDSVVjazU2SHZGTFFBS0wraUd4NzVlWUFOd1AvQ2NDUHN0Yk4xRjZiRmpzOHpTeXJkQlJib3JJWnI5cm50L2hVRlNMSS9UTlRWVGxzeHN6THZvdEkyQi9waUZ0NVZxSW03SjlobkVEWmY2L3dHUmJpZjBYMnd5YkwzQWM4UkRUVEtHS0hIZjZQTjNHVjhnVC90TVhCeHJWY3k4dGN5M1h4LzJhb3pnYmI1Z0EzY29BYnVJRWJnSnVURU5rUUVQbS9pRjZTMmtOcCsxdUkzd0l1czZsb2dJajkzK1F1M3M2dFJOc2lSRVRzWHpPa2Y1bGx2Z1g4a29MOUFKOWpqWWRaNDFHNkxJQkNDMENrWmxYWUVMYXhIQytjUDU2TEQ5ZG4vOC9sUXJSU0haSDh6a1BjSnFQZU1xUHdqRkRXR2pqSTEwQXpXblJUeGY4c3QrYXM1dEYyTi90ZUk3VGxqNmFZT3h4ako1NU9Ic2k1aElHNytTNC9aRnZiU2lGQmVsNGdQODA4eksreEhtL0ptK1pmYzF0TFp4VjdLR1dYZmZrUVJZZCtTdjhGM2h3czRpMDhCRHlnNmNYempMTWNpMVdMLytYbkNpSW1RaVVBb2pmRGZ1eDdyZ0F2QUExSDgyd0JIaEs4QURRY1hnQWFqcndBekdyOWhkL0orZFNTZE43b0V0VmpIMEdlQks0d0Mxd292SVBDSC9QcDNKTjcrY3lnQys5Ukg2SUdPQjMzL2xsTzUwTGRtYkkvVzZIN3RGSUxyQk1hM0NTK0hHdVFqclZjOWhCN2ozWEJtcjdYdm43N0JsRUFsb0J4eGlrZTZ6aWpqS3Q2T2dVR1I2MUg0ODlWUzZrNmlnMGJNQjgzL3J3Mm5pMkVqUjRLVjAwVVdUeXQrYjZ2a1EwQnAva2EyZEd3Tzdna05Zd21kdUdKZlNXd3BUaTRLU001SmlrdjNNNUxYakxQSytMWlFzeXp6R20rd1cxYzBxVGc1dERkVnNOOWhVd0RSTDIrRmE4eEwybkMyMjdlWFRMRWpkTHZvajc1MGtrL1ZleVhOM2lvejlkSDJ6VEdZL2NMS3ZwcG51RW5QTU5wVFFyQWNLelA3UjBTQWVqRTQzODN0bG5OVmh5Ri96MyswNkVMYkNvbW1ZblMxN0hmRlJmWTBWNXFBOS9JZldadzJjczRCV3l3d1FiMDZWcXFBU0FaQWtRelNORWtJcXUrT29vd09zTzdrM3Vhc1YzUGZyc3I1aEJZNEFLekxLTlc0S2Q1Qm9EYnVhUk5ZU05sYmo2RWVCWFZ4cWpNQWlJTjBFbC90d1F6WS9iME1XVmMxZE9PWlk0ZkVDajJzaVQzRDVsNi80TG11L3gwbVc3cWhhZEl2OFR0SE9aMkxobDhiVTg1VVVaTUE5Z21lZS9oNndycTcrVE9vR2NwNlRXRDNweHNWLzUySnd1MkVEWjZ5QVpMOFN4Z1hkUEhxOTRFTXFTSUJFQjl6NTZvNXU3bXN6bnFQVHloaU5OaDFUaCttL1lUMk9JT0IwWlNBRnh3RzdQeHRrUjRsQXVLaVZRejBGZ0I4QmhKZUd0Z3crRUZvT0h3QXRCd2VBRm9PTHdBTkJ4ZUFQTG9XTHo1N3pNSEVEYUlBaEFxcmZCSUlYcDFKY1NnY05iaUJMdGozS3ZRc2U1azJIZVFOVUNIVmFzUXFKRUpUMHViUW9mVjJOTFFZYlVnUXJMNExTcm9pMUpLS2hFcytMOG9sR0NSUlVQOTNObnZzcXRwWHlEdkt6akNHZzhwbDJUMTNvUkRLVjVJZmswL2Fyd0E2UEJBM0hpbUZCWTVXNkJIVnlEbzRzczFNUHN3Q0F3MFc4b0pkVVJXQk5WemdBNnIxcnU4ZGZFU1JvdDlOQlNlbWpWTWtvSU9WVFZVVklyaXQ3b3A3WHVvQldDTkdlTlZ6anFzTWNNTWlRYklla2dnUERXYmU1SVVkTERGM3h1TVJOK1BVQlNBdFlxTm5NUnJzY29NUVM2Rk5RSm1XS1ZsU044dDU2cmxrMzBRcUtrenhyZ2lOYXh3WThKUVFwNEQ2TWIrTElUK1JvSGhoM21VajVETlZjcFQ5eVc4TlRDUERxc0dGcHVwK3hCZUFCb092eExZY0hnQmFEaThBRFFjWGdBYURpOEFEVWRlQUV4bmJ6MUdFSmtBdEdKWHFUZHlvOGJYZm1SbmUxQkQ5ZGlYU0FTZ1JUZDFEbk9NcnBMSmJXWjRsQWZvT2x3b2tUZVhMT2F1SzFqY1k3cUhCc2xDMERrVytEUWZJZVJCSGlKVXVrT08wR0tGanRXWmJORlZjZjYzN0NpeDMzUVBEU0lCYU5GbGsxUEFBM3lETlM3VDF0eE92Y3duZUlWVk9wb0xUS053S2wvVlk3RTc5VU84eHFHQ1ArMlE2M21WNndXSDYycjZxMXAvK2hGdE4vNVRlZXozVUNCeUZuMEN1QWdRZTVHL1NKc1RoUjdVNWhUTExIQ01PYm9zOEVpcG5ONmNmbFBmL24xdC9LZStmQjZ1aS8vZWpOb2VmeTB3SHRQSFBmTmRrZGNBRWRRYUlBbTV4Z3puV05DNlExWnJnRitWZm4rM29NTDdTL2ZRSU5JQU82elJZVDQrTkQxUG16WE5qUlFCYzZ4d2xLY05KK3hWV09LN3VkOGJlMHIzMENDWkJFYjNZV3h5a2ZmVFJuMWpRTFliWUFZVVpsR3g5NC9JanJuUmgzaHQzSVB4aStBRkhoektDeU04K2dDL0g2RGg4TGFBaHNNTFFNUGhCYURoOEFMUWNHUUNZTHNQb0M3OU5oNUo2WTl3MjU3VCsxMi9RZE1ySW5rTHNOMEhVSmR1Y3pQWGIzcS82emRvZW1WRUFuQW5mNnVndlpldnh0L3EwbTJPSnZ0TjczZjlCazJ2Z1dnSXlEei9pOGVtemlpK2ljalQ3MG12bHIwblIzKy9rRDZLcCs5WEphK2hCeFhpbjFIR1Y5WFBWbit4L09YamIwZ0gwM1R4VFhTeEZHcXVsRVRlVmF6YUdiVE5sV3ptSVBadTRBbnBTZS9TZDRsdlQxL2xEdHMxZm9ENmdGbFk0bmNvcEZLay94RUFmNjF0ditUcEtaNm5KNGZVUkFHUTdldkZBdGpveWJpYmpjZnFCdFkxa0QzOVFKbWFLd1BzNlpzRm9INzdCT256YXZTYmVWNWdmMDhFNEdEOUpBUzhJZnhmSHFGVkk1Z2hIa2V2a2tJZ2ZGYUpIenJFdFpuSlBtU2czY3hsaWYwOVFhK0hnSG5nUE5XSGdQb3FYbCsrWWdyVlZMZ3BmVGNOWXROUXV2eHY1ckxFL2g1b2dHZ1NhTHNQd0kxK043RE1jdnhOcElzdlpLSGk2Uk1DMVViSFNFZEpsOHNmRnA0K0psRnM5S3J0azlRdnJFeC9YbUsvT3RlU2lBVGdvbFNBQkJjVjMwVGs2WjlORy9Dek9mcVhoZlJSUFAyeUtua05QYXdRLzZJeXZxcCt0dnFMNVM4YlArKzhxaXdka0pTL21pc2xjZFZ4Z0pmNEQ5NlhvOXpEVStuM3V2UWY4RC9ja2FQZnkrZjNqTjd2K2cyYVhnT1JBTUJsMXRqaFhmSFRSL2xUVnFSd2RlblA4UjFlcHgzLytodnVGOWl6Ri9SKzEyL1E5TXJ3RzBJYURtOE5iRGk4QURRY1hnQWFEaThBRFljWGdJYkRDMERESVJxRDNDOVBIMDY2UndYSTFzQ3g5TnV1TW5SZHVzZlFvVGdFMUdQZHJqV0ZlajAzcUoyQ2g0UzhBTmdZdU11dWtaNjRaOURCeGtDZEwrOEVZVVZidjRjR2VRRVlBeU1EeHhnejBpTVBIWHFFbURkTW1DNThBZnVHQ28rU0tBNEJZeFZTa1dPYlU2alhmMjBDNUZFUzhpVFFOdjdYcFhzTUhVUUJzS25XWWFkN1ZJQmZDR280dkFBMEhGNEFHZzR2QUEySEY0Q0d3d3RBdzdGL0JXRENMd2oxQXJJQTFGOW5DNWtpWktydjVaNWdpOG0rNTlJQXlBSXdHZjhOR3JiZUhiRi9lOURGSEFYSUFyQVYvdzBXdHQ3dDJkOUR1R3FBa0luQ1h6bUVoWDlxUk96VmkySENmajhINkFsa1k5QVdBVnVhdTdXTGYrVXc3UlFxWWYra2tiN3Q1d0M5Z2l3QUpnMHdtYkltK1N1bmhOY0xUNHBDTkNHa3J4SXhtZjErRU9nQmhra0RlUFlQQUs0YW9EN3NGemlVWS8rRUpweEhLYmhxZ0wyQVNidW8yTy9uQUQxQXJ6VkF2M2J0SmtvLy8rbFJFN0lBYk1kL3c0ZEE4K2xSRS92WEZ1RFJFL3cvdTNoZWVRdVpDRE1BQUFBbGRFVllkR1JoZEdVNlkzSmxZWFJsQURJd01UWXRNRGN0TVROVU1UQTZNakU2TlRrck1EQTZNREFiQVltTEFBQUFKWFJGV0hSa1lYUmxPbTF2WkdsbWVRQXlNREUyTFRBM0xURXpWREE1T2pJMk9qVTBLekF3T2pBdzg4MmdFQUFBQUJsMFJWaDBVMjltZEhkaGNtVUFRV1J2WW1VZ1NXMWhaMlZTWldGa2VYSEpaVHdBQUFBQVNVVk9SSzVDWUlJPVwiIiwibW9kdWxlLmV4cG9ydHMgPSBcImRhdGE6aW1hZ2UvcG5nO2Jhc2U2NCxpVkJPUncwS0dnb0FBQUFOU1VoRVVnQUFBUUFBQUFEd0NBUUFBQUJGbm5KQUFBQUFBbUpMUjBRQVZiR01oa2tBQUFBSmNFaFpjd0FBQUVnQUFBQklBRWJKYXo0QUFCcHBTVVJCVkhqYTdaMS9iR1ZIZGNjL2Q3TkwxcnRKK2d3dGthMVUyUitpYVl1cWZSczdnbFJiNWJrdFpSTWtZbTlGcVNwVnNwTm9YWVNhUUtSS0ZhbEVOaFhxWHlRcEtHbzNndlVXQ2FRQXdyc1JoWVgrc0ZGUUM4SE9lcFdXUXRIbWgxUnNWYVY5YnZxSGd6Yms5by83YStiZStYWHZmYy92MlhlK0srOTc3NTc1ZmM2Y21UdG41a3p3QVR5YWpEMkRMb0RIWU9FRm9PSHdBaUJqakpDeFFSZGlPK0VGUU1RWTY4QjZrMFNnMXdJdytQNHpSbGc1NWpvd2prNEVrcFFIWDhjZVFoWUFzd0lNMDM4bTJQcVBQbjRZNXo2bURXUExPMkZpdFJwRTdOK0lSY0NVOGk3U0VhSUE5RUlCanRkSUlZbWI5TVB5cUJNWEFnSTJnQTBDQWtQS2RlbzRkQWpTZFlDa2lray8wQ0dFUXZPSWlOSUpLc1ZQZXBrdWYxdmVZYzJ5dTZac3ErTU9RcVlCekFyUUZhWSthQjgrb3R4TkxEU25VcTl2bW9ZZ01lVjZlbWJJRUpSZUNUVDNJbE1mekJxMWF0K3hwMkR1bStheW0zVmdsckpOeit3b2xIOExDQ3pzMHpkTmtQNnJDbnNLRzhhZWFjNDcwMzZxT29ncDd4cjJ3OTRlcHpmNGNYR2pSaGsyR0dkZHk5NGs1Y0hYc1lmb3RRRHNkTlFSbngwSnZ4TFljSGdCYURpOEFEUWNYZ0FhRGk4QURZY1hnSWJEQzBERDRmY0Q1T01PdXZ6YmpETDdBZHgyQk5pTU1XUEcvUURtSE96c3FiTWZ3RzRPajhxK3E0U2szSDRBTnd1WXVRbjFEQnJYZkhkTjI4WitXK3JydVU5OTZydG1OMEJ4UDBBRXZiRWpDV1d5dCtucHR0Z3Urd0ZNcFRQbmJvOXZSaGlublh6dUNzajdBVlRmWld3NGFRRjFHSHYvZE5rUFlPcC9ibVhUaTA5bzNXMVFKcWNkZ1V3QUF1bWZIbllSMERXeGkzcmVpTGRsbWJHdWpWMjFiQkZOL3RTbnZvdk13ZVUzaEpnUkRyeHh4bGl2cko3SERNYmdYUXEvSDZCWGNYY28vRUpRdytFRm9PSHdBdEJ3ZUFGb09Md0FOQnhlQUJvT0x3QU5oOThXbmtjNHdMV0ErbWVuU3BkL2I3M29mV21FUVpZZ3RKYWd1aW5JSG5NQU5aZUhnQkNNdG40WC93QmhCVXBaVk0zRFZ2Nkk5WUV4RmJ0d1ZJM3RWZ09YVUNYaTdwRUMyS3J2Y3E1UEh6OXdhQ0NYS29RR2M2OUwrUUlEVGY0c2wwZVlIaDJ6dFlDNWhtWUJDaTJ4d2RhSnBmS0xRNEJyOVhXN2RleU5GUFJBd1lkR2E3OXRGSzFUZ2pDTkhTaXBZdTZxWEpMdXBTOURhS0NiMDg1cVorNEdZaWtES0RzSkRCMTZqeTJVaTNSVzYrTnV6QTBjVWpjeHlDWGRxaTFnNm9JdWFkdmJvRUF2OHhxWXFUaVhVR3FhV1FYYUpkaWNoazNGdXFsb1d3K3k1ZTR5aWF3S3U0amI2VklKeW1nQU45VlpiNVliT0dxWmZwVXhNQ3BZdDNRRGh6QlZZUjZtelFPZ011NWVUYURCWWRBbDJNbjVWNGpyVndJYkRpOEFEWWNYZ0liREMwREQ0UVdnNGZBQzBIQjRBV2c0NU1PaGliUFV3Y0h1RDd5ZmNHa0JGNHRvdFhpMnM5R3V1WlJDSmdESnlUMFhkKy9WR3FCM3FKYVdyZVJ1TFdCeU5adFo0OFpLeDVZUDUrbnJNRmE1QmNKY0tRSDE0VkR6R1Q0WGkvYVk0bG52UkVDWGxwdHJDSDM1M1ZwQWw4ZFk3bmp0V0tuWVdRMXNHcWplMGZUQ0FWMzViS0NMTzNYYkVXeWRNMlczblRUMkV1alNzYnR3dDVmY3pTRzkybUpRWkcyUW85c09sbWZNVVoxUERLV1V6UVpqbmNFNktJWXBPd25VVjBFZXY0cDlLRWpqVjlVRVdWeFYvdXVLVXVqTFVEZjM4cXZ1V1N4ZEc3anFZQjNHRmQrc0tDY0FkUnJBM3Z3SjIweVh5bVRwVk11L3Jnam9jeCtQcWNrSXJtYUNLZjhndFNZR0dxb3A1ZXo0dXYwSXZERFBLTGNmd05RQThnVEdWSDF6NFV4Vk5LVWhWczIrN1V2RkFyc0FtbXF3a1hNZ3NWRTZCVGVZMkx1UlhudmpWa3JLQ1lCN3o3ZGZHV0Vxbk8zR0VNZXFsYTZIaXdDYWFwQWRMZzhzMTlaVVI1S3llUkEyRFlDNUkvQzlkaEN4c3pGbXZURnAxOEVmREJIaEhVUjROQTFlQUJvT0x3QU5oeGVBaHNNTFFNUGhCYURYR0tRNXV3THkrd0ZzTU5tclhLbytWdFBlM1cvVUwxMWdXZXdlc3JvWDl3T1lZTEtZdTV6TFN5eDI0dzRIVFBXb2U3aktmRHc4c0I1L3RUTllGOStVK29DUTN3OFFHaTNTcmhaelUvd0EzWUtMMjdsZGt6M1IzclNoa2NIWlVtNm9vT2I5aGF2VDFwOGVOcVUrTUtqbkFMcE5CMlo3VmNiNEtwc1dvZ2EyYjV2Uyt6QUljYlgxQlNXZXF2SlZDNGp0WlBOUUloTUEyVkJZM2lJZFhieXMzdzlnUTBCbXk2dDJnRG9MVTljRlJiVjgzVUxVOGZIUkIyUUNJRnJUcXNpd3pScG5ON2JXVlkyQm9FSFVHa0wrVklYb1RTOVc1U0JyajZIUkUrSVFrSTNNS2dVZUtmaG91NWNhRzhiOUFDN0dWcnQ3Q0RNQzR5aHNhL3BzRU5KN0YwbENCaVdwWW9pcUcycjZBclUxY04xUVFSZGpxU3BNZERXN1MzeWJkdzI3a2pXNVVLbmpZY1M4cTg5K21ZejdRTEp0VUF0QStVbVNTN2hlR0Z0ZFVxanVvc0pGQk9xV2JzamdWd0psN0VBVzFvTVhnSWJEQzBERDRRV2c0ZkFDMEhCNEFXZzRoazhBV3NPeVNOb001QVhBelpadE10bUVUbFJkaUJiZGJYa1ZHN1FmaEtGQk9YZnhicWFZMFJybDZaSUlTY3NZcnFyTCtneTc2QWJ3T3BBRndMeEtiVjd0VG5wMWk2NVNCTUowb1ZSOWRpOVMvYU5waUs0bWZ2S3RtbE4xOHdubUJpSVRBTm5kZXhHWlBWem5NajBnaU5tL3FjekxyTm9qMWIvSnFPSHNiSks3T245NVIwRStGVGYzRVkxRFlndkltc3pzOGRwbXlVcllYMzBjMzNReTE2allMNVl0WC81a00xdEczMFZYd05kQklnQkowNHIvcXhCUzE1KzNiZlRXNlE4NTkzd1o1SXNRaWlWY1QwTWxwV3pVRVZBOU1tdWcyR1I2QnlQMld5OWNyNVhRMFNNUkNBdnppTVFRclBiS0w1ZW5WMjdsRzRBeWJ3RzJiWTMySzVsTTlOR1UyZ1dDZ2g0SWhOeFZBNUY1UDVIc1BzSWpSWm0zQVBPbUtwdVRNNXZ2anMyVTZ2SWFHVGcrUytEdVBxSmhjTDh3d2paRTlBYm1PVUFkTlBEc3Z3dUd6VUdFWjlJMlkvaHNBUjdiQ2k4QURZY1hnSWJEQzBERHNac0VZQ0pkYVpqb1MvcDcyUi8vRzdhcGN3MUVBakFkTjl3UzA1VlRlc3hpNmJjaDVLeTBXNkFzRXlkWVNiK3ZLR05QMUJLUHZiekJiV3l4eFcyOG9SU0JDV3ZwajhUVUk1bzg5SFJiVFBoQTdwODVoNVJMa2FQSWtJZTVETFJZRkFKbnIyVFQ2Zk1aTG1pU3pzN1Z6WE5XczVnc0loOWlsaGNGRnNJYzU2WEdYY21GbjJTMUJGMHVvYXAwUjdncS9UN0tTOUx2L2R6R0dpUEFGbTEreU92YUZsRG5ZZk1tTG9jSm5DbGlpS3psYlhWTWw4NFRTYjRNd0xKbUZXNVJJU0RMVEtIRHZLWUFJK20zclFKdGdhZlJZNFhJc1VTRWRVSldwQnhXbUpSRVlESkh0K09xVUxxb2hQbjRhNHh3RUppSUJVRlhUcGhVVU56UFZmWHI4S3dvNG1sS2lRQ3N4Wi9pTmd5eHlKZUJIN0psWEtiZEgzK2UxWWJZWjRnOXg0dWNOdEJuQktFS21aRjBGY0NxSUFLcTNwOC9uMXRreUQ1ZVM3L2ZwQ3pEUVc0aTRJQ2hsUGNZYUdZTmFEYkhCNHBVeWdsNHd2N2Y0aC9FeDlsWTFyVWs4RU8yQ21Ia0lrUUNNR3BJVGRkdndLWUI0UGxVQkVKbWVGNFJJaEdCSXZzanRIS2Z4ZExkYkN4cG01OXdOL0IxMnNyNHE1Q0trTG9FazdHbW1pd01XUDFHd3Y3ZlkwUVdBZGY1cklyOXhRWUVzNktLK3M3TFNwcE5BeVFpZ0liOXhMbWpuZVFkelgzbXNaOVg0MiszS3FnSHVVS2JOYURORmQ1V21BUGtMYWxWUFFEWUZiMHR4Qi93aGNLemlQM3pIQVMrQ3BBTTRPNnZnVVgyNXl0NGdBTWNZSVFEV2lWNVBkZHpQYURxWVFzVzlzTVl6elBERE05cnRuTk9zTUUwMDJ4VW5PZVA4TXZ4djVGQytjYTRoWkRYYU5ObUR5RzNHTGVVVG1xWWYwajQwMkUvTnRoQ2ZGN3hMQkw2czd5Rkx3SXd4WEpFcVA1R1c2emlpRFhPRHd3aGJScGdqQTJJKy82R292a25KTHBLQkg0eDk1bHZvcWlYdElsbVJMS2VlRHRySUx3WHJORTI3Q2lxbytDM2FvWlE3K2w0S2E1Zk5EOUwyVzhTQUhFbVcxUTZSZmIvT1M5S3Y0b3dxMFR6SEdCUzhacFhoZzd3OXR4bnZvbWkrY1ZlVmdxdmdOa2tXWHlTbjZiWkpubndaZUd2aURrV2hPL1ZRdWp4VWlyaUF2dUg2Y0tJQ2VEWGhOOHZhaVpTL1VVSUN2YjNNdlVJZ3pGN0grR3F6UDVoRWdDUGdXQTMyUUk4S3NBTFFNUGhCYURoOEFMUWNIZ0IyRjM0Qko4b0YwRVdnSmJEd1d3OVhHN2VkTU9FMHFJK0tkaXlpMi81MDRUU3Yra2MvWFNPcmxwMEVxM2xSL3BBQi9nTFMvdDA2RlJ1dHh2NUdCL2pSa3VvV1dhWlRYNklyNEV0dWt3Q0s0cTkrVXZjSGE5QW5XR1J3eXdxTE5aUHM4cFpZSjRKVGt2MGNuZitKclo5T2Z3azM0dWZScW5ka1Z2NmllemhDYzRXNHJ2YTQ5dEF0UERUZTNvU0pnQm1wZjBPQUIyV0dLVUxqTkxOdjYvVG9ndk04elNuT1F2Szh4UFBjUUw0TnI5aGFOM1plREZwam1WZUVRVWdZbjlrVFN1S1FCZzdldFUzbjIydE1EcFArR0dlNHNNOHhjTThybW1nQ1ZhWlpyRmcweE9QZnFweWNMbSsvVHU4bThzYzU1LzRkWTJQZ3paWENBazRWbGpweStpQWdmNFMxOWpIRVNVOWFmNDVZS0ZrQ3liVStkVGNMdEpQRjR6dzg4cVYxV2tXbWVNQzNXVERqV2dPVHBaVFY1aXM1S2hGM0N1Z3RoemV5VG8vNHQ5NGxSYzFOc0VKVnBqaGd0YWtXd2Qvejd1NFJKdEwzTWsvYWtORmErbHRJMTFlczVSeFRiRlhLRUhTK3hhVTFGR3AxWFE3TDlTN0xXNTFlQUx3RURBYUQ0OHROck01d0ZscE5YMkZTY08yRGgwMjJhUkxsMDN0NGE1LzVuZjVKck44bGZuQ2hnNUkyTC9JUkYrV2dYK2JMM09TcjNHU0x5ZzNidHdHbU01SEp2U0FnT3UwOUgzczEyeDhtWlVZbjEvTDc4VHNUMDVQZGt2TkJSN2hrOUx2VC9KSUljd2hEdEVCSG1lQk9jNUhYQkxuQUNIRWN3Q1ZBcmNQQVFIWkNCY3E2ZTltblJOOGwzZnhYZDVWc0ZvbjdGZjNmbkVkWFRXbmlCek1kSVVOS1huNmwvZ0F6L0JCbnVHRGZJVlRHaGNTYlVBMWhtZjA2L2laa2E2T240MjlFWjdMN1VIc3NKVFdLcXFmUEF0bzViUnFjUTRnRGlGRkVjN0VUNkxscllINm50ZGxoZ3NjaWxXM2ZqK2dEcE9GTVU0V0FEUDc1WXJhdGtSc0twLytDUWQ1a0p0NWtBUDhHYWNrMmpnL2pyK3R4WjlIZTByUDJIOWVVK2JsZUFoSWFwWm44S1kwUkJUWjN3TGcyOEFKWXZXZXl6OGErNWZsYUhrTklQYmlmS09iRVlWb3hjMWZmajlNVkhFOSs1TzNnQVNxdDREUjFFVk5zUTdtU1dJeXN2K0xzWFIxNkFHaGdmMnFFcW9jYVVVaW9Ib0RlQStYK0JCUEE2ZjVLMDd5ZHhKMWxnVWVwc3RDUG02bUFjUXRpVlVZT0M5VllGNFp4bllsaTZuM3IzQ0hJQUozRk96L1I3bEtsMno2ZVZSQmwzL25ZWE1hVTRjK1oyVS9UT1ZlQTR2WUZFUThqMnY4UGw4QzRHbTZYRk9FZUJ5WXk4ZDFOUWNmaTE5L1BQcU5EdVRWZEU4d0MxQVVRYjhmb09Id3RvQ0d3d3RBdytFRm9PSHdBdEJ3TkU4QUlyTnhSMEhwcEtiYzJ4elMwZG42ZHBoSFlsRUFqcVVOY0t4eWV0WDlBOVJGaTVCejhmZHoybDBOajhZMmlLV0NDUHdtUzV6aUZPL2dIZnlBWHluRWpGcm5vL0V2OGFiakNNZDVIeUYzY2ljaDcrTjRJYjdaZlVWK04wTitQME9SYmdzeFhjaERwaWNWU1Y4RGo3SEdNbjhKTE5CS3paN0ZSRnhjU2ZabjEvc3NDN1M1d2pIV0ZJc3FVYzRMM01lNWVMMWR2eDUrbUpjcHJoUWVUTCsvd1U4VmE2RUxQTXNpYmE3RTZjaHI5WFp6ZUdMZjZ5b055ZWExMTFCaEhlem1RZ1RwUnBrVmhTdmZrRkVoUm1yT3pqVEFHc3RNY1lIMzAySlRjUkptOEZnQTFwaGxEWlZCTlZsclQ5aXZNcmtlaWovVnB1algrU2xiYlBGVDNsRFNuK1VDTUpyYTlKZEwxK0FvM2ZSS2pLSWVTUFpCNlhUb0puT3hyVlZ0YnczalZDWkFxNFZ2WVp4eHh2bXY1RUdtQVVKbXVNQTU1bUpKTis5bjBXVXZvb3dlME4xUUlPS1lJSllxRFhWT01MRXVjRitCbnJlbjVYdEkxaGtPYzdXUSt4TjhCSUJsT3FEVVFPS0p4eTJsQnREbkh6SVM3N2dhVWFZUWFZQXV4Q2E1VndxWDZ5UWFZQUk0cTNUbUhkSm1EL0Fta0I1dGs2MkJFZnZ2VTNvS0NuUGZxaGw3ZE0zdmdpdnAyYmc1NVFCMUg2UzlYODErY2M5RGZxMzlHZDVNN1FOWGViSVEvNk5jSkprN3FGZjE5eWxYNEYxeE1QMzhDZkR6aWhBZkFXQ1JHUlk1ck5BQklkbXgxR0tIbWdEK0czaVRQZndDTDNBbWVpeHFnRTFhY2RNdDBhbXc1UXRqQ0xjTlQ2WWNiQnJnYy94aCtqMi9MMDVtdjRxQngzaEkrUFVZcnhoeVZ3bFkxRWQvQnNBMXJRYlFlUkVLT1N3OUQzbWxvQUZFL1JIWjlzck5BVzRIL2hPQUgyZXRtNm05TmkwMmVaWnBsdWdvdGtSbE0xNjF6Mi94cVNwRWtQdG5wcXB5V0l1WmwzMFhrYkEvMGhFbmVFNmlKdXlmWXBSQTJYK3Y4R25tNG45RjlzTWFDendNbkNHYWFSU3h5U2IveDF1NFRubUNmOUxpWU9NR2J1QmxidURHK0g4elZHZURiWE9BVzluRHpkek16Y0R0U1loc0NJajhYMFF2U2UyaHRQM054VzhCbDFsVE5FREUvbTl6SCsvZ0JORzJDQkVSKzVjTjZWOW1nZThBdjZSZ1A4RG5XT1p4bG5tU0xuT2cwQUlRcVZrVlZvVnRMRWNLNTQ5bjRzUDEyZjh6dVJDdFZFY2t2L01RdDhtb3Q4d29QQ09VdFFZTzhqWFFqQmJkVlBFL3g0bWMxVHphN21iZmE0UzIvTkVVYzVQRGJNYlR5VDA1bHpCd1A5L25WVGEwclJRU3BPY0Y4dFBNL2J5VGxYaEwzaVQvbXR0YU9xM1lReW03N011SEtEcjBVL292OE9aZ0VXL2pEUEJ4VFMrZVpaU0ZXS3hhL0M4L1Z4QXhFU29CRUwwWjltUGZjd1Y0QVdnNG1tY0w4SkRnQmFEaDhBTFFjT1FGWUZyckwvd2V6cWVXcFBOR2w2Z2VPd2p5SkhDUmFlQkM0UjBVL3BoUDVaNDh5S2NIWFhpUCtoQTF3TW00OTA5ek1oZnFucFQ5MlFyZHA1UmFZSVhRNENieDVWaURkS3psc29mWWZxd0kxdlR0OXZYYk40Z0NNQStNTWtyeFdNY3BaVnpWMHdrd09HbzlGSDh1V1VyVlVXellnTm00OFdlMThXd2hiUFJRdUdxaXlPSkp6ZmNkald3SU9Nblh5WTZHM2MwbHFXRTBzUXRQN0N1QkxjWEJUUm5KTVVsNTRYWlc4cEo1WGhIUEZtS1dCVTd5TGU3aWtpWUZONGZ1dGhydUtHUWFJT3IxclhpTmVWNFQzbmJ6N3J3aGJwUitGL1hKbDA3NnFXSy92TUZEZmI0KzJxWXhHcnRmVU5GUDhnMWU1eHVjMUtRQURNZjYzUFloRVlCT1BQNTNZNXZWZE1WUitOL2pQeDI2d0pwaWtwa29mUjM3WFhHQlRlMmxOdkN0M0djR2w3Mk1FOEFxcTZ4Q242NmxHZ0FTQVVoRzVheUgyOFpwTlpZSnJLeWJLbXhtaU5pK1ZKdjlNRTNMY1BIVlhiblBEQzYzaXA4bE1yWk9RQVgzR1VPS1NBQTY2ZStXWUdiTW5qNmxqS3Q2MnJITThRTUN4VjZXNVA0aEUvdm5OTi9scHd0MFV5ODhSZm9sM3N0KzNzc2xnNi90Q1NmS3J0RUF5YTFoR21yOCtSNitxYUQrVHU0TWVwYVN2aS9wemNuMjNtOTNzbUFMWWFPSHJESWZ6d0pXTkRQOXFqZUJEQ2tpQVZEZnM3Y3FOTUg5ZkNaSGZZRFBLdUowV0RJcWNOTitBbHZjNGNDdUZBQVgzTVYwdkMwUm51U0NZaUxWRERSV0FEeDJKYncxc09Id0F0QndlQUZvT0x3QU5CeGVBQm9PTHdCNWRDemUvSGVZQXdnYlJBRUlsVlo0cEJDOXVoSmlVRGhyV2NYdkdHMGduWW9Xa2lHR3JBRTZMRm1GUUkxTWVGcmFGRG9zeFphR0Rrc0ZFWkxGNzdTQ2ZscEtTU1dDQmY4WGhSS2M1clNoZnU3c2Q5blZ0Q05RSEFLcUNrRVNiNU1wbGdvTjFDRmtpU2syRGVtNzVWeGRTQk5qazVySklhR0IvWGxxc0Z0MGdYb08wR0hKZXBlM0x0NFMwV3EvMkVkRDRhbVplVWtLT2xSbmZ0SERRWFhzMUFGUUFiVUFMRE5sdk1wWmgyV21tQ0pxb0NsaHpUd1FucHJOUFVrS090amlidzkya1RXZ0tBRExGUnM1aWRkaWlhbkN0cEJsQXFaWW9tVkkzeTNucXVXVGZSQ29xVlBHdUNJMXJIQmp3bEJDdmk5Z21UUEdwalhkS0REOE1OK29FYUdURGxibHFUc1MzaHFZUjRjbEE0dk4xQjBJTHdBTmgxOEpiRGk4QURRY1hnQWFEaThBRFljWGdJWWpMd0NtczdjZXV4Q1pBTFJpVjZtM2NxdkcxMzVrWjN0VVEvWFlrVWdFb0VVM1BWTjNtSzZTeVcybWVKS1AwM1c0VUNKdkxqbWR1NjdnOURiVFBUUklGb0xPTWNlbmVJaVFSemxEcUhTSEhLSEZJaDJyTTltaXErTDhiOWxSWXIvcEhocEVBdENpeXhySGdZL3pMWmE1VEZ0ek8vVUNqL0VLUzNRMEY1aEc0VlMrcWtkaWQrcjd1TWErZ2ovdGtKdDRqWnNFaCt0cSttdGFmL29SYlN2K1UzbnM5MUFnY2haOUZMZ0lFSHVSdjBpYm80VWUxT1k0Qzh4eG1CbTZ6UEZFcVp6ZW1uNVQzLzU5US95bnZud2Vib3ovM29yYUhuOERNQnJUUnozelhaSFhBQkhVR2lBSnVjd1U1NWpUdWtOV2E0QmZsWDUvdjZEQyswdjMwQ0RTQUpzczAyRTJQalE5UzV0bHpZMFVBVE1zY29obkRTZnNWWmpuKzduZnE5dEs5OUFnbVFSRzkyR3NjWkY3YWFPK01TRGJEVEFGQ3JPbzJQdDMyUm5hM1F2eDJyaEg0eGZCQ3p3NmxCZEdlUFFCZmo5QXcrRnRBUTJIRjRDR3d3dEF3K0VGb09ISUJNQjJIMEJkK2wwOGtkS2ZVTGhxN0RlOTMvVWJOTDBpa3JjQTIzMEFkZWsyTjNQOXB2ZTdmb09tVjBZa0FQZnd0d3JhKy9oYS9LMHUzZVpvc3QvMGZ0ZHYwUFFhaUlhQXpQTy9lR3pxbE9LYmlEejlnZlJxMlFkeTlIdUY5RkU4dlZlVnZJWWVWSWgvU2hsZlZUOWIvY1h5bDQrL0toMU0wOFUzMGNWU3FMbFNFbmxYc1VIaGwweFhGVVIwRUhzLzhGbnBTZS9TZDRsdlQxOE1VVForZ1BxQVdWamlkeWlrVXFUL0VRQi9yVzIvNU9seFhxQW5oOVJFQVpEdDY4VUMyT2pKdUp1Tngrb0cxaldRUGYxQW1ab3JBK3pwbXdXZ2Z2c0U2Zk5xOU50NVFXQi9Ud1JnYi8wa0JQeE0rTDg4UXF0R01FTThqbDRsaFVENHJCSS9kSWhyTTVOOXlFQzduY3NTKzN1Q1hnOEJzOEI1cWc4QjlWVzh2bnpGRktxcGNGUDZiaHJFcHFGMCtkL09aWW45UGRBQTBTVFFkaCtBRy8xK1lJR0YrSnRJRjEvSVFzWFR6d3BVR3gwakhTVmRMbjlZZVBxVVJMSFJxN1pQVXIrd012MEZpZjNxWEVzaUVvQ0xVZ0VTWEZSOEU1R25meVp0d00vazZGOFIwa2Z4OUN1cTVEWDBzRUw4aThyNHF2clo2aStXdjJ6OHZQT3FzblJBVXY1cXJwVEVkZThFZUluLzRQMDV5Z044T2YxZWwvNGovb2U3Yy9RSCtmeTIwZnRkdjBIVGF5QVNBTGpNTXB1OE8zNzZKSC9Lb2hTdUx2MTV2c2NidE9OZmY4TWpBbnUyZzk3ditnMmFYaGwrUTBqRDRhMkJEWWNYZ0liREMwREQ0UVdnNGZBQzBIQjRBV2c0UkdPUSsrWHB3MG4zcUFEWkdqaVNmdHRTaHE1TDl4ZzZGSWVBZXF6YnNxWlFyK2NHdFZQd2tKQVhBQnNEdDlneTBoUDNERHJZR0dpN3hqMnNhT3YzMENBdkFDTmdaT0FJSTBaNjVLRkRqeER6aGduVGhTOWczMURoVVJMRklXQ2tRaXB5YkhNSzlmcXZUWUE4U2tLZUJOckcvN3AwajZHREtBQTIxVHJzZEk4SzhBdEJEWWNYZ0liREMwREQ0UVdnNGZBQzBIQjRBV2c0ZHE0QWpQa0ZvVjVBRm9ENjYyd2hFNFJNOUwzY1k2d3ozdmRjR2dCWkFNYmp2MEhEMXJzajltOE11cGk3QWJJQXJNZC9nNFd0ZDN2Mjl4Q3VHaUJrclBCWERtSGhueG9SZS9WaW1MRGZ6d0Y2QXRrWXRFN0F1dVp1N2VKZk9VdzZoVXJZUDI2a2IvZzVRSzhnQzRCSkE0eW5yRW4reWluaGxjS1RvaENOQ2VtclJFeG12eDhFZW9CaDBnQ2UvUU9BcXdhb0Qvc0ZEdVhZUDZZSjUxRUtyaHBnTzJEU0xpcjIremxBRDlCckRkQ3ZYYnVKMHM5L2V0U0VMQUFiOGQvd0lkQjhldFRFenJVRmVQUUUvdy9BZFZ5N2RpRzlVUUFBQUNWMFJWaDBaR0YwWlRwamNtVmhkR1VBTWpBeE5pMHdOeTB4TTFReE1Eb3lNVG8xT1Nzd01Eb3dNQnNCaVlzQUFBQWxkRVZZZEdSaGRHVTZiVzlrYVdaNUFESXdNVFl0TURjdE1UTlVNRGs2TWpZNk5UUXJNREE2TUREenphQVFBQUFBR1hSRldIUlRiMlowZDJGeVpRQkJaRzlpWlNCSmJXRm5aVkpsWVdSNWNjbGxQQUFBQUFCSlJVNUVya0pnZ2c9PVwiIiwibW9kdWxlLmV4cG9ydHMgPSBcImRhdGE6aW1hZ2UvcG5nO2Jhc2U2NCxpVkJPUncwS0dnb0FBQUFOU1VoRVVnQUFBUUFBQUFEd0NBTUFBQURZU1VyNUFBQUJEbEJNVkVWM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCM2RpQjNkaUIzZGlCVkxrZUpBQUFBV1hSU1RsTUFHUkF6QkFoUXY0S1pMeUpWY1VCbVlCb1RNc3dOSVR3V1FraExJQjVhSXljeFV5eUZOSWVBdzJySXo4WTRSUnk4dUw1OHE3V2xqS3FvclIreUtmMEJubEVrN3dvR0FnT1BvbUtVU3FDdmJkK2NSMk0vYjMrUmFQbEFYdkVBQUFBQllrdEhSQUNJQlIxSUFBQUFDWEJJV1hNQUFBQklBQUFBU0FCR3lXcytBQUFQWkVsRVFWUjQydTFkQzJQYnRoRUd5VWlxNlppU1hibExFNmV4MW1UTzVpWFpxK3U2cm8zYWJHMjZwT2tTZDEzdi8vK1JBWHpoY0llSFdNb1ViZU9UTGVzSUVNQjlQSUIzQUNnTEVSRVJNUUlra095NkNUdldIMGJPUU8vbUplRFhQOEVNcU16REVrSXNFQlJNQW1oN2pIU1ZtdUFqQUt3QzhGUkF6aTgvRG1vUzFBSTVBUWx0ajVGT3J5QWpnSjdPSzJDWmt3RVpZTzIzcStCSjV3d0trdHRmdWkxejRzMjBWVEFMNWsya0Y1aGJpUGNLY3d2d05HQjRDN0NUd3Byb0k0Q2REY3hFUEtVVEV4eCtETmlBajB1OUM5QXVOUHhkWU9lNDZZNVFSRVJFUkVSRXhJaHg2WjdnanYyZ2hFVnJRSjMzaEo1QnN4c0Jmc0lxOE0wSHNBa2hXZnFnbEZnYXdBaGdHV2gyTTF4TVdBV1VBRTkwcVVvZk1oaGk3YmUzMkpOc21WRkpQS2VMd0JRZ2xBUU1OaDNBTFZqWWJOYUkxamFZRDBqTTBudzlhdGNXWUVYaWFYSC8rUURlUTNZNkJvUngzZThDRVJFUkVSRVJFUkc3UXovSFAraWFCc3Z2SFhqMExBRDRjaXAweU4yN2ZYdzdBR3RRb0RUd0grSHFrV1RnV2N6VHdaVm1yOERiQUV1cXYzNWJDVDZDV0RvcmpHbkFxd09TQ0k3RWhsRldIamtCWElrYjFNL0RaUWdSd0NlQXdLOUIrSFJQRmxQQk9qZVpzekt6MHdLOS9GbHplRTNJMjRHRXpVSUk0NWJUL1NZYXJxR0xlc0UrYnRsREJQNzBRSW5rY2tEd2dnUXFBR0d0MDUyNjY3dkFKWjhmdmsxR1JFUkVSRVJFM0ZUMDM1YmEwODFJTEx2UjNVWGEvTkRnVWxXZyttNE4yS2dDZnp6UDFsWXREVURwQWk5T2JlRFZxY3p1NEFTc3kvdThrYXhJZC8yVytKWXE0Q3NickJjVjhTUHc4aVJ2cldXemUrSWxJTEEzWEZqTnpNZUFsNy9FTXQwVG1INHd3dGttSEc0T3NMVnpZa0VzSExaRTQreVJEYkZCQSt5cFZvWko2ZlI4aXcyNFQyY0VzQmJ3NXBucHRJdUZDYkEzd0hrSk4wcG1BYk9iQU92YU9sK2hkMTRBMWdWSUZ3bDJBWHN2VDV3NUdNUGV6UUU4ajhYQWhGbUFZQ3YwQVFMSUlFaFMyYkFVbXNHaDlWdXVrVC9aM2dvSGdac0U3d0VMNEpuSFBSK3c2K2RqSWlJaUlpSWlSbzNMdll0elI0VThLbXM1WTd1T1JiZzQ2SmE5by83QWorRG96M29HWm0yajlYS2lNYzBNVHBHdDdQZ1h2cm9EMkc1eDAzZXMxaVk5VDRjSFhIMUxCbUFLQ3lQNjlCSUM5akw3RXVCK3ZydE04bncvZ0cwK3cxeXZadTMxQlFmTnVlQTZmZXNFTk9HbWk0REVFZzd6cG52aUtaNXVXNTBHa2dyK3pMQkZDaEpMQzFtNEM5aEV3ZHVITGFYUkNSSHZuaFVyQWJSTGJEMjgwNE9hbWt4ZzBabjVmTDhsblFpMmJvOEpZZndFQ0FrUjNoL21qQTZMVHNrVEk0SG9OYlFKS0RULzRKOC91b2E0N3ZwRlJFUkVSRnh2cEZmOFJtWnhPOEMzWEVXOTRWK2kvNWlXQXF6TExLYjNsUVpYQXlFbGhYcEZJVWExR01LMkxnc1VyeWhWVTBoUk1HVEdkeWxVRnFEekMrc1NPQ053TE4wR2VQUkN0OWRML1kzb3pDQUFLaEtNZUphS1dOOEV4a1dBWmZtZEU1UVNtUktBL3dwTDdJYU9KVzBYRzBzWDJNQUNXSDV6eDBaRmtNTUM2SDZGaHU3UjZNOTBaR01BeVdHZG9VbTFsZEF4d0xKQlpqVG1yOXRrU1BpUFk4aEgrVk83UW1ENXBERGdkMlYyWUlEVDBlMGkwWHVnRDhrSUNlaUxMdnBIUkVSRVJOd3NaTXBQeURiUGYyc2ljV3VvMWsxbDQyWlRYNDczQXA0YjdGV3Vra3ZGakNabmZqNXVpUndnRjdkSUFlaU1mU251QzRkTUU4WHRHdVNFUmlVNEtJb3BjdmJLendZaHBWczA1N3VmRzNGUmE3Z3c5RzFiVEdXMnNyVmZwemV0bnVRd21VQStNUm9nV0RCQjk5cGFoZXJBM0Zaakc2UVZSWkZXSUlUTURBSVFBNkJNZEtKcjNETUlrRVVmU3JTdU5EUVc0RnJ2cm9yVEJVNWdjblQwUG1BQ2xzdWwvd2tNZ1FrUUFRTDJEUUpCcVk0T1NFSVNURWpWUUpQd1l3V1hCY0FVMEI5VmNUMEdBR3FnMGVMajh2UmpUY0RSQi91L01naTRjK2NPMng3dmxza0JTb0RTLzBOTWdHbFNJUFVIVGxHS3B2M2dqb0xUQWc2VjZqQTkxUE1BV1duL0xRR3FmRFRGVmhXbkM1UmQ0TzVkM0FXV1FsNEMrZDZla0pXdlgwaUEwdi8ydlEvZEJDVGtnRHlTSkljSkNtSGc1T1RFUFFiQW9XUkE2bzhKS0g5YUFzcEJFQkZ3WDUxOS8zNXo0S2dhQkkrSU91Z0VUZ0I3UkVNUUFqN0M4eFB6eFczNVhyZ0lvQlhDZ3hLb3d0UFRVOUFteWl3Z081eE81WnZ1QXFYc0p1QzBRbjBneWVHRFBGOUJqcDhSUWwxSUh2aDErY0w2VGlnQkUwSUFHQll3MS9wN0NHaUwrN2dFTWJsSlN3QzFnT3l3UkhPSm1BeHFqSjJDMFNmenZMMEw1RTM5dWRNQ09BR2hMb0RUcXpHd2FETzNCR1JtZlcxeGxSOEE3d2tIaUFXRWJvTlZlK2JtSEV5bWI5M0FGUTRNZWd0Y1BUOUFDU2daS01UMmtHV0xFaDE4UGNhaDZicUVzME92YWFYOXJlb2ZFUkVSRVRGeVBIem9UMC9CTzY4TllOdjZTSkRwY1BkUmVadDYxSWgxc04zRzJQTmFucmZuVnE3Si9zYXlFTDhoN1NtODl6VVpiUjJUUS9LMmpmWFBNczNBVEhtUlova1VCVHV5eWZPOTFwR3pVcEhwNDQ5cVY3eGhRSjZzUUZhYVRNOG1WNjdneG5KMVBWb05DdVhNcGUyOVBWWGN6dkUxZlF6d21PaXZIS1VUcmIveXpkdm9ON0U3WWlpY2g5L0sxd0Z1VUNhdmM0YnlHMnVETkxZUXZ4UG40dmM0dnMybGtCdXlNT1hqeVRHU1Zmc1hDMWNEb1hiMmE3a3hPR1J4c3JHTFZMdU8xWXhGRzExeEFrZzRET0xKL2FmUDd0MUgwMGFadE84TXQ4ZEx3Qi9nai9MMUo2eWdjdjJKaklNUEdSdFBjdXI3dG5MdHpLZjIraDQySWhvSFpuQ3drQnhVd2w0elk3UG5JcUFlQlpBRkhNQ2Y0YUZ1a05RZlRkbUZMZUF2NGhQeFZ6MmxkRW9zNEpSWXdDbXhnSVVSZThnZVVBMVNiWHhMNnZ1MGtqNXRHMWdHOHpoMkFEVUdhUDNDQkR5NS85RUQrYkxyWDN2cW1JQVV5bG1uUnY0YmZDWmZmMGM3Sm93K1hzcnZFeG1sbC8xWDRvR0RnQ2E2UzQwR0Vmc1JHT1lvRDVPcE9ESGlSVUpBUmhnbStyYzdJa3dDa1B6NUozZG1kLzd4UlMwZk5zWHRieVl2ektzbldCZW9aU3crZnF4bFpmdnRmS2VWQUVHZzlnaWx3ajBwQ1dTUysxSGRZSDBYVUZ1TWhLdExxTzVPaXZQTGd1alBBL2dVNnkrZWZpbUh2L21YVDFzQ1pQOVBQZWN6UmVkc0VEVW5XZGtrUC9FRDZMUTNrVzNmQU9PVEYxUi9laHNVMWFZdW5WeXVDTnd1MnZPQmxXQWdGMWNRUlljQTMvQ0JJaUlpSWlKMmdDbWVtRmF1SEp5eVBNLzF4MHZlV2xndVJYanZmdENuQlNtczVmc2EzNXJQQUxtYUg4SlhYMzM5Tlh5Qm1uT2c5QzhoUDZ6dXdaTW5jRy9WcEpQOUZzMTBRelBmME1yMFFCdThVYjhwaDlsMCtzSmd3UC9sWWlFc1pGazVpalpCTXJDbTN2aUo5cnorcWZBdjdZcXVwN0tBQlF0dTJuU3lWRXMrMU1HcnppTmR4MHdHTzNweHNFclF3WlZ5ak5md3dySmI5aGNTb0Z3dGRJYlN2ZncxRFVBVDhNMjN6NTkvKzQxdXoxUkFzY0FyTzVRQVk4c0lsSk5SYU1OREtxcXBpbFQ3MnBtYWowRUVQRk5yZGJqQ3RXTGRSUUFOTDdtNkpMMWEzZE1XdFM1bHJYOXE1b2ZTMXZmYjAxL0twQmx5VjJGQ05tU1k1NWZyb0NnRHFNQlR4bk1DVzhCOGp2ZXI1NnVWQ2k4MUFWSi9nYWJBS09NMFdMQ0x4TVRiOWpjMmdQU3ZybUF6Qm53Ryt4THdzczFRRk1iNWNPd240RWgrUEZJL1RiSXlzQ21jSUFzZzBldXpaNGZQVm5EV0Z2aEN0VzYyUFFLb0JYeFh5czJzWEsyL1ZqQmZsemd4VDllRXlVdDZmSHhzRUZCZjJlclBpY1RuOG9kc2VGZzd4NERWU25VQVBBaSttRTVuV3h3RXlSandYVDBHMUF3by9Rc2pIRjJwOXA3bzA5Y0hjSVlZVUFVZG9XR3ZtYnhwOVB2NDQvcUhHSWh6REpobXE5VUtWcGdCZWh2YzlsM2dzWnFZMWUyaG9kdDZQdGNUVm5JRWxEK3BaZ0NNUDgzSC9lWUF2UTJXRmxIQ01RYkFWQUVUWUx1R2ZRZ2dTTXRyLzdqeEF5eDdCTTBSVmxyTGkxU05sTStiMUg4L1NjeXZkUkhscUZGTGsweE42V1hOaG8zdWZzRHVjZlRxMVJFU0Z3ZUtxL1I1eXhodE1OczVHUkVSRWRFTFU3dzcrdlgzYW9qNS92V3VHelVnM2dDOGFZVWZtbEgzaDEwM2F6RGNWZXJlcllYWDFSMUh2V3NiV01JU24vQWZpek1qdHJmemJGbnl2K3hmMEtaNG93S294Z1RlYWdMZXRqbUkyMkR6SXdwTkNWdDZvQWVvREV0MVQxOTZ5NzlFM0swVXZvc3FwNjRIYTA5S0R4VGFLQUliTjVYOGJ2TE9YSjFsMVExSmdCd0JWQWo5eHFqY2JNTWNMNHhWK3V2bHhjTFUzN1oxZDVFdXNIN3Y1TnM3SThOeWh3UVV6ZlV1M0FRVXBNc0RuS2M0RGV0dkl5QTFUS2JjYUQ0eHdtbURnQXlXeStWd25xNVcyRTBBUHdmcEwzVTNCc1hlRmpEc0lGZ2FRUFhRVEtuREswM0FLNVNwOEJlQTAzdVBBY05HYTNUUWU2ckZwemdUT1lrd1lQRFQreTRneElCRDRGSXJYTFhnb2hFdnNJNTBETUJTc2YzZDV6c04xbjlVMDdMdzhzZGR0bUZNc3hVUkVSRVJFUkdYakFKODRtVURac1NSMmVnSmlUN1kyNlA2ZzBlOGZBS0FVR0FRVUthbE9FTXhTOVdia1VHRnpJMDhyeks1dzl1QytNNEZTNFp5aFd4QUFrd0tUQUtxdExiTjVlV1I2dEVNQmdFNG5STkFnMFUrR1dCdXhoMkVBTHdabUJKUVRuL1VqU3ovekhDYjZ3eVlnSmxGcDdER2hyak4veCt3RVFFRFdzQkdCQXhzQWNPT0FSUTdId01Hdmd2dytZNGQzd1ZHZ04zNkFSRVJFUkVSTnh2KzU4aXVPOUwvQ3ZqcGM3UjNVM29wWnpmb2UzTFZjNlR3VTRHZVo4aUxsNVlIS0JyZmhINy9RVmQ1ZEZqRC95UUJBdTFPVnF6TUdBUDB5Vks5WDcrYlBEYWtjQzdFVDRVNHgwOWJyMDlrUkdzK1g2c1ZtUnhQNUUrN2ZSdU96ZjNzU2daVG5xalhaS1R1YlZidm16L1RWeWhmZ05wdGYrQWdvUHhxdE9TdytYNDlTQ0JKMUlGR1BsUXYvZjE3S2wwZVNRNUhTa0JwQVJMbitJcXJjV0Z0N0U1R0JIeFJvVFh4anZMb01DdnZnUXUwNTBVR28xTTRtVG9JdUhhRFlBNXdmbmFPaC8xcU9rS0hwTERsLzNBNU51UnY1UFY1Y3lXZm1vK0lpSWlJNkEzNmZFQklwcHVvdXNwZDYrc3JoMENmRHdqSmRCdGRWN2xyZlgzbDRQV0hGcTgza2VsR3lxNXkxL3I2eWtIUTV3UGU2Z0lhK1VMNWhoZTFYRzJsTGROZnRUSlFXVGpUMytyMHQ4NzZCWGpUMVk1T2tpNW8rd1YrM3NFSDBCVkFLemVGaUhvMStPSUNydzZIOHZOMGxsOHZrZHZTOGVxWi9TOFk3UkUvLy95ek1OdFRQcEc4S1FIR0I0dXNldThGYVRCdUVNc3ZtRUwrL0lTQVlIdEU4K3VRVjVYKzJ5TmdnYjZEemtLQTdXOFhoWUwxV3l6RVp3SHEyMFpXMElHQWNCZFEzNzdWeGNSRFhRUkNCSHE3bENENXFTd1pXTFg1ZzZEUEIxZ0d0V1lRMUlNWUhhU0F5dTVCMVRwSTB2cnBJR3VtTi95NFpOVUhXam1Jb1c5amZXK2pYZVV3aG5aaytqcFNYZVV3aG5abCs3clNYZVdJaUlpSWlJZ0lEMnJINGRMazBZUDgvOEN3ZkEwSkFEOEI1UXNyS1B3RUNQcFBEOGVONmlzSndTTVRncUI1YzhuazM5K05IZEVDYnZ3WWNOUHZBaEVSRVJFUkVSSGJSbkoxUElIZ0xrakl1bTkwVGNqL0J4b3pFaEZvNndZRTBPdDlsZlRmaGdWUWZhK1UvcVlGbE52Ynk1ZURnSGJ0emRUWDRGQ2RmVzNIZ0t5QnFUKys0cFgrVjhjRytscEFsZi9xNnQvWEFxNjgvbjN2QWc3OXIrMFlFSURXLytyWVFOQUN1a0RwM2Z4R1JJd2Mvd2Uwd0lxYWdteTdHQUFBQUNWMFJWaDBaR0YwWlRwamNtVmhkR1VBTWpBeE5pMHdOeTB4TTFReE1Eb3lNVG8xT1Nzd01Eb3dNQnNCaVlzQUFBQWxkRVZZZEdSaGRHVTZiVzlrYVdaNUFESXdNVFl0TURjdE1UTlVNRGs2TWpZNk5UUXJNREE2TUREenphQVFBQUFBR1hSRldIUlRiMlowZDJGeVpRQkJaRzlpWlNCSmJXRm5aVkpsWVdSNWNjbGxQQUFBQUFCSlJVNUVya0pnZ2c9PVwiIiwibW9kdWxlLmV4cG9ydHMgPSBcImRhdGE6aW1hZ2UvcG5nO2Jhc2U2NCxpVkJPUncwS0dnb0FBQUFOU1VoRVVnQUFBUUFBQUFEd0NBUUFBQUJGbm5KQUFBQUFBbUpMUjBRQWQyVHN4NjBBQUFBSmNFaFpjd0FBQUVnQUFBQklBRWJKYXo0QUFCcDBTVVJCVkhqYTdaMTdiR1ZIZmNjL1o3TUw2MnlTWGtOTFpJc3EreEJOSDZyMkpqWktVbTJWNjdhVVRaREEzb3BTVmFwa0o5RzZDRFZBcElxS1ZNcWpRdjJMSkFWRjdVYXczaUtCbElMd2JrUmhvUThiQmJVUTdLeFhhVk5TdEVtUWlxMnF0UGMyL2NORWVaeitjVjR6NTh6cm5IT3Y3N1hQZkszcmMrLzV6Y3labWQ5dmZqTm5mak8vQ1Q2QlI1T3hiOWdaOEJndXZBQTBIRjRBWkV3UU1qSHNUT3drdkFDSW1HQVQyR3lTQ1BSYkFJYmZmaVlJSzhmY0JDYlJpVUNTOHZETDJFZklBbUJXZ0dINlo0S3QvZWpqaC9IVEo3UmhiTTlPbUZpdEJCSDd0MklSTUtXOGgzU0VLQUQ5VUlDVE5WSkk0aWJ0c0R6cXhJV0FnQzFnaTREQWtIS2RNbzRjTWdHd0tjQ29nZ0pMZXJyMjQ0SXM3aVJibFZKd2lldFNDblBLZGNvNGNzZ0V3S3dBWFdGcWcvYnVJM3E2bVlXbVZPcTFUVk1YSktaY1Q4K01HSUxTTTRFaEdGcFFpSjZCV2FXV2I0R3VLVVRNQ1F6eDljOU9HS3ZXSTFuS3BqTHVPcFIvQzdBcFVIM1ZCT2xmVmRoVDJESzJUUE96elYyUW1QS2VZVC9zNzNONjFabmJMMnpWeU1NV2syeHEyWnVrUFB3eTloSDlGb0RkampyaXN5dmhad0liRGk4QURZY1hnSWJEQzBERDRRV2c0ZkFDMEhCNEFXZzQvSHFBZk54aDUzK0hVV1k5Z051S0FKc3hac0s0SHNEOEJEdDc2cXdIc0p2RG83enZLU0VwdHg3QXpRSm1ya0k5Z3lZMTMxM1R0ckhmbHZwbTdxcFBmYytzQmlpdUJ6QVhiOHRaQk5Rd015aEwzV1JzTVZXK0svdlZxZHRNVFp1YTc3c2E4bm9BZS9IY1JFQWR4dDQrWGRZRG1FVEFMVys2MUcwZG5JdUcyblhJQkNDUS92U3dpNEN1aWwxYXpWYThMTXVNVFczc3FubUxhUEpWbjNyRHpjRm1pNWxwUFVCZDJGUFlpaGQwVklsdk5nWm5KZDlUOWtLL0hxQmZjWGNwL0VSUXcrRUZvT0h3QXRCd2VBRm9PTHdBTkJ4ZUFCb09Md0FOaDE4V25rYzR4TG1BK251blN1ZC9mNzNvQTZtRVllWWd0T1lnWWxLVlBOcGpEcUhrY2hjUWd0SFc3K0lmSUt4QUtZdXF6N0RsUDR5bmVrMnAySVdqYW15M0VyaUVLaEYzbnhUQVZueVhmWDM2K0lGREJia1VJVFJzL25USlgyQ2d5ZGR5endqVHJXTzJHakNYMEN4QW9TVTIyQnF4bEgreEMzQXR2bTYxanIyU2dqNG9lQk1EN0wxb25SeUVhZXhBU1JXZnJucEswcnowZVFnTmRIUGFXZW5NelVETVpRQmxCNEdoUSt1eGhYS1J6bXB0M0kyNWdVUHFKZ2E1cEZ1MUJreE4wQ1Z0ZXgwVTZHVmVBek1WNXhKS1RUT3JRTHNFbTlPd3FWZzNGVzFyUWJhbnV3d2lxOEl1NG5hNmxJTXlHc0JOZGRZYjVRYU9XbVpRZVF5TUN0WXQzY0FoVEZXWXUybHpCNmlNdTE4VGFIZ1lkZzUyOC9NcnhQVXpnUTJIRjRDR3d3dEF3K0VGb09Id0F0QndlQUZvT0x3QU5Cenk1dERFV2Vyd1lQY0hQa2k0MUlDTFJiUmFQTnZlYU5lbmxFSnhjNmlMdS9kcUZkQS9WRXZMbG5PM0dqQzVtczJzY1JPbFk4dWI4L1JsbUtoY0EyRXVsNEI2YzZoNUQ1K0xSWHRDY2E5L0lxQkx5ODAxaEQ3L2JqV2dlOFpFYm52dFJLbllXUWxzR3FqZTF2VENCbDE1YzZoNExWc0I1a3dHem5GdDBLMnJjZlVOWUYvdEVGaU5RU3I2Wmt4UGRFd3hON1k2TUdzZ1U4cFpDUEdxUWo2WHBRZUIrZ3FRK3k5VDhhdUtRUmJYeGdCZEZkUVJRL25wNVdmZHMxaTZPbkRWd1RwTUtyNVpVVTRBNmxTQXZmcnRFbHg5UFo2Yzcrb2lvSC82WkV4TjlJZWFDYWJubXpXUUxlVnMrN3A5Qzd3d3ppaTNIc0JVQWZJQXhsUjhjK1pNUlRTbElSYk52dXhMeFFJWEZlcnFxSDV3bStSdDI5Zk4vaFVLSGhUS0NJQjd5N2NmR1dIS25PM0VFTWVpbFM2SGl3Q2FTcEJ0TGpjeG9kNDRLRW5aM0FtYk9zRGNGdmo5aWlEbXFuUEpaTlg0NXYzNTlXTGJVOXJTbmhiaVh2WTZ0VlAzS1pYaStvMGhJcnlEQ0krbXdRdEF3K0VGb09Id0F0QndlQUZvT0x3QTlCdkROR2RYUUg0OWdBMG1lNVZMMFNkcTJyc0hqZnE1Q3l5VDNTTldkcFd6YUQxTTlpcVhmWG5KOGF1VERodE05YWk3dWNxOFBUeXdibisxTTFnWDM1VDZrSkJmRHhBYUxkS3VGbk5UL0FEZGhJdmJ2bDJUUGRGZXRhR1J3ZGxVYnFpZ1p0WVFNNE94bEc2a1JFQTlCakFkSUM5ZTgwV2NzTVEzSWFwZys3SXB2UStERUZkYlgxRGlydXE1YWdHeDdXd2VTV1FDSUJzS3kxdWtvNE9YYllzVzlBakliSG5WTmxCblllcTZvS2oyWExjUWRYeDhEQUNaQUlqV3RDb3liTFBHMlkydGRWVmpJR2dRdFlhUXI2b1EvV25GcWlmSTJtTms5SVRZQldROXMzcEowa1JjTlAyQ0JOTjZBQmRqcTkwOWhCbUJzUmUyVlgzV0NlbTlpeVFoZzVKVU1jUkl1WnhYV3dNM0RRVjBPU3hCRmNac2JKV2ZwS2VFdUNoWmt3dVZPaDVHUkJhV3BZcVVrV0cvVGdES0Q1SmN3dlhEMk9xU1F2VTFCUzRpVURkM0l3WS9FeWhqRjdLd0hyd0FOQnhlQUJvT0x3QU5oeGVBaHNNTFFNTXhlZ0xRR3BWSjBtWWdMd0J1dG15VHlTWjBvdXBDdE9qdXlLdllzUDBnakF6S3VZdDNNOFdNMThoUGwwUklXc1p3VlYzV1o5aERKNERYZ1N3QTVsbHE4MngzMHFwYmRKVWlFS1lUcGVxOWU1SHFIMDlEZERYeGsyL1ZuS3FiZHpBM0VKa0F5TzdlaThqczRUcVg2UUZCelA2ZThsbG0xUjZwL2g3amhyMnp5ZFBWejVkWEZPUlRjWE1mMFRna3RvQ3N5c3dlcjIyV3JJVDkxZnZ4bnBPNVJzVitNVy81L0NlTDJUTDZIam9Ddmc0U0FVaXFWdnl2UWtoZGY5NjIzbHVuUCtTbjUvTWdINFJRek9GbUdpcko1UjQ2QXI0T01tdWdXR1c2TlczWndpaVhZeGRVY1BGbVAwNlBzRENPU0F6QmFxLzhjbjc2NVZhK0FTanpGbUJiMW1nL2tzbEVIMCtwWFNBbzZJRkFlTHFxSXpLdko1TGRSM2lrS1BNV1lGNVVaWE55WnZQZDBVdXBMcStSZ2VPOUJPN3VJeG9HOXdNamJGMUVmMkFlQTlSQkEvZit1MkRVSEVSNEp1MHdSczhXNExHajhBTFFjSGdCYURpOEFEUWNlMGtBcHRLWmhxbUJwTCtmZy9IZnFBMmRheUFTZ05tNDRsYVlyWnpTd3haTHZ3MGhaNlRWQW1XWk9NVmErbjFOR1h1cWxuanM1M1Z1Wkp0dGJ1UjFwUWhNV1hOL05LWWUxVHhEVDdmRmhBL20vc3hQU0xrVWZDS3EvUHU0QkxSWUZnSm5yMlN6NmYwNXptdVN6dmJWTFhKR001a3NJaDlpbnVjRUZzSUM1NlRLWGN1Rm4yYTlCRjNPb1NwM1I3a2kvVDdHaTlMdmc5eklCbVBBTm0xZTRLZmFHbEEvbzlnc1RBZEVCODRVTVVSVzg3WXlwbFBuaVNSZkFtQlZNd3UzckJDUVZXYlFZVkdUZ2JIMDIzYUJ0c1FUNkxGRzVGZ2l3aVloYTlJVDFwaVdSR0E2UjdmamlwQzdLSWY1K0J1TWNRaVlpZ1ZCbDArWVZsRGM5MVVOYXZPc0tPSnBTb2tBYk1SWGNSbUdtT1ZMd0F0c0c2ZHBEOGJYTTlvUUJ3eXhGM2lPMHdiNm5DQlVJWE9TcmdKWUYwUkExZnJ6KzNPTEREbkFLK24zNjVSNU9NUjFCRnh0eU9XZEJwcFpBNXJOOFlFaWxYSUNuckQvTi9rSDhYYldsM1V0Q2J6QWRpR01uSVZJQU1ZTnFlbmFEZGcwQUR5VGlrRElITThvUWlRaVVHUi9oRmJ1V3N6ZDljYWN0dmtKZHdEZm9LMk12dzZwQ0tsek1CMXJxdWxDaHpWb0pPei9YY1prRVhBZHo2cllYNnhBTUN1cXFPMjhwS1RaTkVBaUFtallUL3gwdElPOFk3bHJIZ2Y1VWZ6dEJnWDFFSmRwc3dHMHVjemJDMk9BdkNXMXFnY0F1NkszaGZoOXZsUzRGN0Yva1VQQTF3Q1NEdHo5TmJESS9ud0JyK1pxcm1hTXE3Vks4cTI4bGJjQ3FoYTJaR0UvVFBBTWM4enhqR1k1NXhSYnpETExWc1Z4L2hpL0dQK05GZkkzd1RzSmVZVTJiZllSOGs3amt0SnBEZk1QQ3g4ZERtS0RMY1FYRmZjaW9UL0RXL2diQUdaWWpRalYzMmlMUlJ5enh2bUJJYVJOQTB5d0JYSGIzMUpVLzVSRVY0bkF6K2V1K1NxS1drbWJhRVFrNjRsM3NBSENlOEVHYmNPS29qb0tmcnRtQ1BXYWpoZmo4a1hqczVUOUpnRVFSN0pGcFZOay81L3huUFNyQ0xOS05JOEJwaFd2ZVdYb0FPL0lYZk5WRkkwdjlyTldlQVhNQnNuaW5md3d6VGJJZzY4SW55SVdXQksrVnd1aHg0dXBpQXZzVCtZQlJnRlR3SzhLdjUvVERLUUdpeEFVN085bjZoR0dZL1kreWhXWi9hTWtBQjVEd1Y2eUJYaFVnQmVBaHNNTFFNUGhCYURoOEFLd3QvQXBQbFV1Z2l3QUxZZU4yWHE0bkx6cGhpbWxSWDFhc0dVWDMvSm5DYVcvMlJ6OWRJNnVtblFTcmVWSEIwQUgrSE5ML1hUb1ZLNjNhL2trbitSYVM2aDU1cGxQZm9pdmdTMjZUQU5yaXJYNUs5d1J6MEE5eERKSFdGWllySjlnblRQQUlsT2NsdWpsenZ4TmJQdHkrR20rSDkrTlVudDNidW9uc29jbk9GT0k3MnFQYndQUnhFLy82VW1ZQUppWDFqc0FkRmhobkM0d1RqZi92azZMTHJESUU1em1EQ2ozVHp6TkNlQTcvTHFoZHVmanlhUUZWbmxaRklDSS9aRTFyU2dDWWV6b1ZWOTl0cm5DYUQvaFIzaWNqL0E0OS9HSXBvS21XR2VXNVlKTlQ5ejZxWHFDamNFaEFkL2xWaTV4RS8vRXIybDhITFM1VEVqQThjSk1YMFlIRFBRWGVZMERIRlhTaytwZkFKWksxbUJDWFV6TjdTTDlkTUVJdjZpY1daMWxtUVhPMDAwVzNJam00R1E2ZFkzcFNvNWF4TFVDYXN2aGJXenlRLzZOSC9HY3hpWTR4UnB6bk5lYWRPdmc3N21GaTdTNXlHMzhvelpVTkpmZU50TGxPVXNacnluV0NpVklXdCtTa2pvdTFacHU1WVY2dGNVTkRuY0FQZ3FNeDkxamkxNDJCamdqemFhdk1XMVkxcUZEang1ZHV2UzBtN3YrbWQvaFc4enpOUllMQ3pvZ1lmOHlVd09aQnY0dHZzSkp2czVKdnFSY3VIRWpZTm9mbWRBREFxN1MwZzl3VUxQd1pWNWlmSDR1dnhPelA5azkyUzAxRnJpZlQwdS9QODM5aFRDSE9Vd0hlSVFsRmpnWGNVa2NBNFFRandGVUN0emVCUVJrUFZ5b3BOL0tKaWY0SHJmd1BXNHBXSzBUOXF0YnZ6aVByaHBUUkE1bXVzS0NsRHo5eTN5UUova1FUL0lodnNvcGpRdUpOcURxd3pQNlZieGhwS3ZqWjMxdmhLZHpheEE3cktTbGlzb25qd0phT2ExYUhBT0lYVWhSaERQeE01d2VqcUhsZFpualBJZGoxYTFmRDZqRGRLR1Brd1hBekg2NW9MWWxFVDNsM1QvbUVQZHlQZmR5TlgvS0tZazJ5WS9qYnh2eDlWaGY2Um43ejJueXZCcDNBVW5KOGd6dVNWMUVrZjB0QUw0RG5DQlc3N25uUjMzL3Fod3Ryd0hFVnB5dmRET2lFSzI0K3N1dmg0a0tybWQvOGhhUVFQVVdNSjY2cUNtV3dUeElUSHIyZnpIbXJnNDlJRFN3WDVWRGxTT3RTQVJVYndEdjRTSWY1Z25nTkgvSlNmNU9vczZ6eEgxMFdjckh6VFNBdUNTeENnTVhwUUlzS3NQWWptUXh0ZjQxM2kySXdMc0w5djlqWEtGTE52dzhwcURMdi9Pd09ZMnBRMSt3c2g5bWNxK0JSZlFFRWMvak5YNlBMd1B3QkYxZVU0UjRCRmpJeDNVMUJ4K1BYMzg4Qm8wTzVOVjBYekFQVUJSQnZ4Nmc0ZkMyZ0liREMwREQ0UVdnNGZBQzBIQTBUd0FpczNGSFFlbWtwdHdiSGRMUjJmcDJtVWRpVVFDT3B4Vnd2SEo2MWYwRDFFV0xrTFB4OTdQYVZRMFB4amFJbFlJSS9BWXJuT0lVNytKZC9JQmZLc1NNYXVmajhTL3hwT01JTi9FK1FtN2pOa0xleDAyRitHYjNGZm5WRFBuMURFVzZMY1JzNFJreVBTbEkraHA0bkExVytRdGdpVlpxOWl3bTR1SktjakNyM3VkWm9zMWxqck9obUZTSm5yekVYWnlONTl2MTgrRkhlSW5pVE9HaDlQdnJ2S3FZQzEzaUtaWnBjemxPUjU2cnQ1dkRFL3RlVjJsSU5zKzloZ3JyWURjWElrZ1h5cXdwWFBtR2pBc3hVbk4ycGdFMldHV0c4N3lmRmozRlRwamhZd25ZWUo0TlZBYlZaSzQ5WWIvSzVIbzR2cXBOMFQvbFZiYlo1bFZlVjlLZjRqd3dudHIwVjB1WDRCamQ5RWlNb2g1STFrSHBkR2lQaGRqV3FyYTNobkVxVTZEVnd1OWtra2ttK2Eva1JxWUJRdVk0ejFrV1lrazNyMmZSUFY1RUdUMmdPNkZBeEhGQkxGVWE2cXhnWWwzaXJnSTliMC9MdDVDc01SemhTdUhwai9JeEFGYnBnRklEaVRzZXQ1VWFRUC84a0xGNHhkV1lNb1ZJQTNRaE5zbTlYRGhjSjlFQVU4QVpwVFB2a0RiN2dEZUJkR3ViYkEyTTJIK1gwbE5RbVB0V3pkaWpxMzRYWEU3M3hpMG9PNmk3SUczOWF2YUxheDd5YysxUDhtWnFIN2pDWTRYNEgrY0N5ZGhCUGF0L1FEa0Q3NHBENmZVbndNOHFRbndNZ0dYbVdPYUlRZ2VFWk50U2l3MXFDdmh2NEUzMjhYTTh5MFBSYlZFRDlHakZWYmRDcDhLU0w0d2gzQlk4bVo1ZzB3QmY0QS9TNy9sMWNUTDdWUXc4emtlRlh3L3pzdUhwS2dHTDJ1Z2JBTHltMVFBNkwwSWhSNlQ3SVM4WE5JQ29QeUxiWHJreHdNM0Fmd0x3NDZ4Mk03WFhwa1dQcDVobGhZNWlTVlEyNGxYNy9CYnZxa0lFdVQ4elZmV0VqWmg1MlhjUkNmc2pIWEdDcHlWcXd2NFp4Z21VN2ZjeW4yVWgvaXV5SHpaWTRqN2dJYUtSUmhFOWV2d2ZiK0VxNVE3K2FZdURqV3U0aHBlNGhtdmovMmFvOWdiYnhnQTNzSS9ydVo3cmdadVRFRmtYRVBtL2lGNlMyaU5wKzF1STN3SXVzYUdvZ0lqOTMrRXUzc1VKb21VUklpTDJyeHJTdjhRUzN3VitRY0YrZ0Mrd3lpT3M4aGhkRmtDaEJTQlNzeXFzQzh0WWpoYjJIOC9GbSt1ei8zTzVFSzFVUnlTLzh4Q1h5YWlYekNnOEk1UzFCZzd6TmRDTUZ0MVU4VC9OaVp6VlBGcnVabDlyaERiLzBSQ3p4eEY2OFhCeVg4NGxETnpOOC95SUxXMHRoUVRwZm9IOE1QTWd2OEphdkNSdm1uL05MUzJkVmF5aGxGMzI1VU1VSGZvcC9SZDRjN0NJdC9NUThJQ21GYzh6emxJc1ZpMytsNThwaUpnSWxRQ0kzZ3dIc2U2NUFyd0FOQnpOc3dWNFNQQUMwSEI0QVdnNDhnSXdxL1VYZmlmblVrdlNPYU5MVkk5ZEJIa1F1TXdzY0w3d0RncC94R2R5ZCs3bHM4UE92RWQ5aUJyZ1pOejZaem1aQzNWbnl2NXNodTR6U2kyd1JtaHdrL2hTckVFNjFuelpRK3c4MWdScitrNzcraDBZUkFGWUJNWVpwN2l0NDVReXJ1cnVGQmdjdFI2T3J5dVdYSFVVQ3paZ1BxNzhlVzA4V3dnYlBSU09taWl5ZUZyemZWY2o2d0pPOGcyeXJXRjNjRkdxR0Uzc3doMzdUR0JMc1hGVFJySk5VcDY0blplOFpKNVR4TE9GbUdlSmszeWIyN21vU2NITm9idXRoTHNLbVFhSVduMHJubU5lMUlTM25ieTdhSWdicGQ5RnZmT2xrMTVWN0pjWGVLajMxMGZMTk1aajl3c3Era20reVUvNUppYzFLUUNqTVQrM2MwZ0VvQlAzLzkzWVpqVmJzUmYrOS9palF4ZllVQXd5RTZXdlk3OHJ6dFBUSG1vRDM4NWRNN2lzWlp3QzFsbG5IUVowTE5VUWtBaEEwaXRuTGR6V1Q2dXhTbUJsM1V4aE1VUEU5cFhhN0lkWldvYURyMjdQWFRPNG5DcCtoc2pZT2dVVjNHZU1LQ0lCNktTL1c0S1pNYnY3dURLdTZtN0hNc1lQQ0JScldaTHpoMHpzWDlCOGwrOHUwVTI5OEJUcEYza3ZCM2t2RncyK3RxZWNLSHRHQXlTbmhtbW84ZlU5ZkV0Qi9lM2NIdlFzSlgxYjBwdVQ3YTNmN21UQkZzSkdEMWxuTVI0RnJHbEcrbFZQQWhsUlJBS2dQbWR2WGFpQ3UvbGNqbm9QbjFmRTZiQmlWT0NtOVFTMnVLT0JQU2tBTHJpZDJYaFpJanpHZWNWQXFobG9yQUI0N0VsNGEyREQ0UVdnNGZBQzBIQjRBV2c0dkFBMEhGNEE4dWhZdlBudk1nY1FOb2dDRUNxdDhFZ2grblVreExCd3hqS0wzekhhUURvVkxTUWpERmtEZEZpeENvRWFtZkMwdENsMFdJa3REUjFXQ2lJa2k5OXBCZjIwbEpKS0JBditMd281T00xcFEvbmMyZSt5cW1sWG9OZ0ZWQldDSkY2UEdWWUtGZFFoWklVWmVvYjAzWjVjWFVnVFk1T2F5U0doZ2YxNWFyQlhkSUY2RE5CaHhYcVd0eTdlQ3RGc3Y5aEdRK0d1bVhsSkNqcFVaMzdSdzBGMTdOWU9VQUcxQUt3eVl6ektXWWRWWnBnaHFxQVpZYzQ4RU82YXpUMUpDanJZNHU4TTlwQTFvQ2dBcXhVck9ZblhZb1dad3JLUVZRSm1XS0ZsU04vdHlWWHpKL3NnVUZObmpIRkZhbGpoeElTUmhIeGV3Q29QR2F2V2RLTEE2TU44b2thRVR0cFpsYWZ1U25ocllCNGRWZ3dzTmxOM0lid0FOQngrSnJEaDhBTFFjSGdCYURpOEFEUWNYZ0FhanJ3QW1QYmVldXhCWkFMUWlsMmwzc0FOR2wvN2taM3RRUTNWWTFjaUVZQVczWFJQM1JHNlNpYTNtZUV4SHFEcmNLQkUzbHh5T25kY3dla2RwbnRva0V3RW5XV0J6L0JSUWg3a0lVS2xPK1FJTFpicFdKM0pGbDBWNTMvTGpoSUhUZmZRSUJLQUZsMDJ1QWw0Z0crenlpWGFtdE9wbDNpWWwxbWhvem5BTkFxbjhsVTlGcnRUUDhCckhDajQwdzY1amxlNFRuQzRycWEvb3ZXbkg5RzI0NC9LWTcrSEFwR3o2R1BBQllEWWkvd0YyaHdydEtBMk43SEVBa2VZbzhzQ2o1WjYwdHZTYityVHY2K0pQK3JENStIYStQTTIxUGI0YTREeG1EN3VtZStLdkFhSW9OWUFTY2hWWmpqTGd0WWRzbG9EL0xMMCsvbUNDaDhzM1VPRFNBUDBXS1hEZkx4cGVwNDJxNW9US1FMbVdPWXdUeGwyMkt1d3lQTzUzK3M3U3ZmUUlCa0VSdWRoYkhDQkQ5QkdmV0pBdGhwZ0JoUm1VYkgxNzdFOXRIc1h5WUVSbDJueklMTzBnZk04cUJ6akI5STFVTkxGajhjdWdGOFAwSEI0VzBERDRRV2c0ZkFDMEhCNEFXZzRNZ0d3blFkUWwzNDdqNmIwUnhXdUdnZE5IM1Q1aGsydmlPUXR3SFllUUYyNnpjM2NvT21ETHQrdzZaVVJDY0NkL0syQzlqNitIbityUzdjNW1odzBmZERsR3phOUJxSXVJUFA4TDI2Yk9xWDRKaUpQdnllZElyb25SLytBa0Q2S3V4OVFKYStoQnhYaW4xTEdWNVhQVm40eC8rWGpyMHNiMDNUeFRYUXhGMnF1bEVUZVZXeFErQ1hUVlJrUkhjVGVEWHhldXRPLzlGM2kyOU1YUTVTTkg2RGVZQmFXK0IwS3FSVHBmd2pBWDJuckw3bDdFOC9TbC9sV1VRQmsrM294QXpaNjB1OW0vYkc2Z25VVlpFOC9VS2JteWdCNyttWUJxRjgvUVhxL0d2MW1uaFhZM3hjQjJGOC9DUUZ2Q1AvTEk3UnFCRFBFN2VoVlVoQnRIVlhpaHc1eGJXYXlEeHRvTjNOSlluOWYwTzh1WUI0NFIvVXVvTDZLMStldm1FSTFGVzVLMzAyRDJEU1U3dmszYzBsaWZ4ODBRRFFJdEowSDRFYS9HMWhpS2Y0bTBzVVhzbEJ4OS9NQzFVYkhTRWRKbC9NZkZ1NCtMbEZzOUtyMWs1UXZyRXgvVm1LLytxa2xFUW5BQlNrRENTNG92b25JMHorWFZ1RG5jdlN2Q3VtanVQdFZWZklhZWxnaC9nVmxmRlg1Yk9VWDgxODJmdDU1VlZrNklDbC9OVmRLNHFvVEFDL3lIN3cvUjdtSHI2VGY2OUoveVA5d1I0NStMMS9jTWZxZ3l6ZHNlZzFFQWdDWFdLWEhyZkhkeC9nVGxxVndkZW5QOEgxZXB4My8rbXZ1RjlpekUvUkJsMi9ZOU1yd0MwSWFEbThOYkRpOEFEUWNYZ0FhRGk4QURZY1hnSWJEQzBERElScUQzQTlQSDAyNlJ3WEkxc0N4OU51Mk1uUmR1c2ZJb2RnRjFHUGR0aldGZWkzWGJ6dnJNL0lDWUdQZ050dEdldUtlUVFjYkEyM0h1SWNWYmYwZUd1UUZZQXlNREJ4anpFaVBQSFRvRVdKZU1HRTY4QVhzQ3lvOFNxTFlCWXhWU0VXT2JVNmhYdnUxQ1pCSFNjaURRRnYvWDVmdU1YSVFCY0NtV2tlZDdsRUJmaUtvNGZBQzBIQjRBV2c0dkFBMEhGNEFHZzR2QUEzSDdoV0FDVDhoMUEvSUFsQi9uaTFraXBDcGdlZDdnazBtQi82VUJrQVdnTW40TTJ6WVduZkUvcTFoWjNNdlFCYUF6Zmd6WE5oYXQyZC9IK0dxQVVJbUNwOXlDQXQvYWtUczFZdGh3bjQvQnVnTFpHUFFKZ0dibXJPMTYvb0NubllLbGJCLzBramY4bU9BZmtFV0FKTUdtRXhaazN6S0tlRzF3cDJpRUUwSTZhdEVUR2EvN3dUNmdGSFNBSjc5UTRDckJxZ1Ard0VPNWRnL29Rbm5VUXF1R21BbllOSXVLdmI3TVVBZjBHOE5NS2hWdTRuU3oxODlha0lXZ0szNE0zb0lORmVQbXRpOXRnQ1B2dUQvQVZaSlpoQXVZaFJHQUFBQUpYUkZXSFJrWVhSbE9tTnlaV0YwWlFBeU1ERTJMVEEzTFRFelZERXdPakl4T2pVNUt6QXdPakF3R3dHSml3QUFBQ1YwUlZoMFpHRjBaVHB0YjJScFpua0FNakF4Tmkwd055MHhNMVF3T1RveU5qbzFOQ3N3TURvd01QUE5vQkFBQUFBWmRFVllkRk52Wm5SM1lYSmxBRUZrYjJKbElFbHRZV2RsVW1WaFpIbHh5V1U4QUFBQUFFbEZUa1N1UW1DQ1wiIiwibW9kdWxlLmV4cG9ydHMgPSBcImRhdGE6aW1hZ2UvcG5nO2Jhc2U2NCxpVkJPUncwS0dnb0FBQUFOU1VoRVVnQUFBUUFBQUFEd0NBTUFBQURZU1VyNUFBQUJEbEJNVkVYTUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFETUFBRE1BQURNQUFEUDFYTFBBQUFBV1hSU1RsTUFHUkF6QkFoUXY0S1pMeUpWY1VCbVlCb1RNc3dOSVR3V1FraExJQjVhSXljeFV5eUZOSWVBdzJySXo4WTRSUnk4dUw1OHE3V2xqS3FvclIreUtmMEJubEVrN3dvR0FnT1BvbUtVU3FDdmJkK2NSMk0vYjMrUmFQbEFYdkVBQUFBQllrdEhSQUNJQlIxSUFBQUFDWEJJV1hNQUFBQklBQUFBU0FCR3lXcytBQUFQWkVsRVFWUjQydTFkQzJQYnRoRUd5VWlxNlppU1hibExFNmV4MW1UTzVpWFpxK3U2cm8zYWJHMjZwT2tTZDEzdi8vK1JBWHpoY0llSFdNb1ViZU9UTGVzSUVNQjlQSUIzQUNnTEVSRVJNUUlra095NkNUdldIMGJPUU8vbUplRFhQOEVNcU16REVrSXNFQlJNQW1oN2pIU1ZtdUFqQUt3QzhGUkF6aTgvRG1vUzFBSTVBUWx0ajVGT3J5QWpnSjdPSzJDWmt3RVpZTzIzcStCSjV3d0trdHRmdWkxejRzMjBWVEFMNWsya0Y1aGJpUGNLY3d2d05HQjRDN0NUd3Byb0k0Q2REY3hFUEtVVEV4eCtETmlBajB1OUM5QXVOUHhkWU9lNDZZNVFSRVJFUkVSRXhJaHg2WjdnanYyZ2hFVnJRSjMzaEo1QnN4c0Jmc0lxOE0wSHNBa2hXZnFnbEZnYXdBaGdHV2gyTTF4TVdBV1VBRTkwcVVvZk1oaGk3YmUzMkpOc21WRkpQS2VMd0JRZ2xBUU1OaDNBTFZqWWJOYUkxamFZRDBqTTBudzlhdGNXWUVYaWFYSC8rUURlUTNZNkJvUngzZThDRVJFUkVSRVJFUkc3UXovSFAraWFCc3Z2SFhqMExBRDRjaXAweU4yN2ZYdzdBR3RRb0RUd0grSHFrV1RnV2N6VHdaVm1yOERiQUV1cXYzNWJDVDZDV0RvcmpHbkFxd09TQ0k3RWhsRldIamtCWElrYjFNL0RaUWdSd0NlQXdLOUIrSFJQRmxQQk9qZVpzekt6MHdLOS9GbHplRTNJMjRHRXpVSUk0NWJUL1NZYXJxR0xlc0UrYnRsREJQNzBRSW5rY2tEd2dnUXFBR0d0MDUyNjY3dkFKWjhmdmsxR1JFUkVSRVJFM0ZUMDM1YmEwODFJTEx2UjNVWGEvTkRnVWxXZyttNE4yS2dDZnp6UDFsWXREVURwQWk5T2JlRFZxY3p1NEFTc3kvdThrYXhJZC8yVytKWXE0Q3NickJjVjhTUHc4aVJ2cldXemUrSWxJTEEzWEZqTnpNZUFsNy9FTXQwVG1INHd3dGttSEc0T3NMVnpZa0VzSExaRTQreVJEYkZCQSt5cFZvWko2ZlI4aXcyNFQyY0VzQmJ3NXBucHRJdUZDYkEzd0hrSk4wcG1BYk9iQU92YU9sK2hkMTRBMWdWSUZ3bDJBWHN2VDV3NUdNUGV6UUU4ajhYQWhGbUFZQ3YwQVFMSUlFaFMyYkFVbXNHaDlWdXVrVC9aM2dvSGdac0U3d0VMNEpuSFBSK3c2K2RqSWlJaUlpSWlSbzNMdll0elI0VThLbXM1WTd1T1JiZzQ2SmE5by83QWorRG96M29HWm0yajlYS2lNYzBNVHBHdDdQZ1h2cm9EMkc1eDAzZXMxaVk5VDRjSFhIMUxCbUFLQ3lQNjlCSUM5akw3RXVCK3ZydE04bncvZ0cwK3cxeXZadTMxQlFmTnVlQTZmZXNFTk9HbWk0REVFZzd6cG52aUtaNXVXNTBHa2dyK3pMQkZDaEpMQzFtNEM5aEV3ZHVITGFYUkNSSHZuaFVyQWJSTGJEMjgwNE9hbWt4ZzBabjVmTDhsblFpMmJvOEpZZndFQ0FrUjNoL21qQTZMVHNrVEk0SG9OYlFKS0RULzRKOC91b2E0N3ZwRlJFUkVSRnh2cEZmOFJtWnhPOEMzWEVXOTRWK2kvNWlXQXF6TExLYjNsUVpYQXlFbGhYcEZJVWExR01LMkxnc1VyeWhWVTBoUk1HVEdkeWxVRnFEekMrc1NPQ053TE4wR2VQUkN0OWRML1kzb3pDQUFLaEtNZUphS1dOOEV4a1dBWmZtZEU1UVNtUktBL3dwTDdJYU9KVzBYRzBzWDJNQUNXSDV6eDBaRmtNTUM2SDZGaHU3UjZNOTBaR01BeVdHZG9VbTFsZEF4d0xKQlpqVG1yOXRrU1BpUFk4aEgrVk83UW1ENXBERGdkMlYyWUlEVDBlMGkwWHVnRDhrSUNlaUxMdnBIUkVSRVJOd3NaTXBQeURiUGYyc2ljV3VvMWsxbDQyWlRYNDczQXA0YjdGV3Vra3ZGakNabmZqNXVpUndnRjdkSUFlaU1mU251QzRkTUU4WHRHdVNFUmlVNEtJb3BjdmJLendZaHBWczA1N3VmRzNGUmE3Z3c5RzFiVEdXMnNyVmZwemV0bnVRd21VQStNUm9nV0RCQjk5cGFoZXJBM0Zaakc2UVZSWkZXSUlUTURBSVFBNkJNZEtKcjNETUlrRVVmU3JTdU5EUVc0RnJ2cm9yVEJVNWdjblQwUG1BQ2xzdWwvd2tNZ1FrUUFRTDJEUUpCcVk0T1NFSVNURWpWUUpQd1l3V1hCY0FVMEI5VmNUMEdBR3FnMGVMajh2UmpUY0RSQi91L01naTRjK2NPMng3dmxza0JTb0RTLzBOTWdHbFNJUFVIVGxHS3B2M2dqb0xUQWc2VjZqQTkxUE1BV1duL0xRR3FmRFRGVmhXbkM1UmQ0TzVkM0FXV1FsNEMrZDZla0pXdlgwaUEwdi8ydlEvZEJDVGtnRHlTSkljSkNtSGc1T1RFUFFiQW9XUkE2bzhKS0g5YUFzcEJFQkZ3WDUxOS8zNXo0S2dhQkkrSU91Z0VUZ0I3UkVNUUFqN0M4eFB6eFczNVhyZ0lvQlhDZ3hLb3d0UFRVOUFteWl3Z081eE81WnZ1QXFYc0p1QzBRbjBneWVHRFBGOUJqcDhSUWwxSUh2aDErY0w2VGlnQkUwSUFHQll3MS9wN0NHaUwrN2dFTWJsSlN3QzFnT3l3UkhPSm1BeHFqSjJDMFNmenZMMEw1RTM5dWRNQ09BR2hMb0RUcXpHd2FETzNCR1JtZlcxeGxSOEE3d2tIaUFXRWJvTlZlK2JtSEV5bWI5M0FGUTRNZWd0Y1BUOUFDU2daS01UMmtHV0xFaDE4UGNhaDZicUVzME92YWFYOXJlb2ZFUkVSRVRGeVBIem9UMC9CTzY4TllOdjZTSkRwY1BkUmVadDYxSWgxc04zRzJQTmFucmZuVnE3Si9zYXlFTDhoN1NtODl6VVpiUjJUUS9LMmpmWFBNczNBVEhtUlova1VCVHV5eWZPOTFwR3pVcEhwNDQ5cVY3eGhRSjZzUUZhYVRNOG1WNjdneG5KMVBWb05DdVhNcGUyOVBWWGN6dkUxZlF6d21PaXZIS1VUcmIveXpkdm9ON0U3WWlpY2g5L0sxd0Z1VUNhdmM0YnlHMnVETkxZUXZ4UG40dmM0dnMybGtCdXlNT1hqeVRHU1Zmc1hDMWNEb1hiMmE3a3hPR1J4c3JHTFZMdU8xWXhGRzExeEFrZzRET0xKL2FmUDd0MUgwMGFadE84TXQ4ZEx3Qi9nai9MMUo2eWdjdjJKaklNUEdSdFBjdXI3dG5MdHpLZjIraDQySWhvSFpuQ3drQnhVd2w0elk3UG5JcUFlQlpBRkhNQ2Y0YUZ1a05RZlRkbUZMZUF2NGhQeFZ6MmxkRW9zNEpSWXdDbXhnSVVSZThnZVVBMVNiWHhMNnZ1MGtqNXRHMWdHOHpoMkFEVUdhUDNDQkR5NS85RUQrYkxyWDN2cW1JQVV5bG1uUnY0YmZDWmZmMGM3Sm93K1hzcnZFeG1sbC8xWDRvR0RnQ2E2UzQwR0Vmc1JHT1lvRDVPcE9ESGlSVUpBUmhnbStyYzdJa3dDa1B6NUozZG1kLzd4UlMwZk5zWHRieVl2ektzbldCZW9aU3crZnF4bFpmdnRmS2VWQUVHZzlnaWx3ajBwQ1dTUysxSGRZSDBYVUZ1TWhLdExxTzVPaXZQTGd1alBBL2dVNnkrZWZpbUh2L21YVDFzQ1pQOVBQZWN6UmVkc0VEVW5XZGtrUC9FRDZMUTNrVzNmQU9PVEYxUi9laHNVMWFZdW5WeXVDTnd1MnZPQmxXQWdGMWNRUlljQTMvQ0JJaUlpSWlKMmdDbWVtRmF1SEp5eVBNLzF4MHZlV2xndVJYanZmdENuQlNtczVmc2EzNXJQQUxtYUg4SlhYMzM5Tlh5Qm1uT2c5QzhoUDZ6dXdaTW5jRy9WcEpQOUZzMTBRelBmME1yMFFCdThVYjhwaDlsMCtzSmd3UC9sWWlFc1pGazVpalpCTXJDbTN2aUo5cnorcWZBdjdZcXVwN0tBQlF0dTJuU3lWRXMrMU1HcnppTmR4MHdHTzNweHNFclF3WlZ5ak5md3dySmI5aGNTb0Z3dGRJYlN2ZncxRFVBVDhNMjN6NTkvKzQxdXoxUkFzY0FyTzVRQVk4c0lsSk5SYU1OREtxcXBpbFQ3MnBtYWowRUVQRk5yZGJqQ3RXTGRSUUFOTDdtNkpMMWEzZE1XdFM1bHJYOXE1b2ZTMXZmYjAxL0twQmx5VjJGQ05tU1k1NWZyb0NnRHFNQlR4bk1DVzhCOGp2ZXI1NnVWQ2k4MUFWSi9nYWJBS09NMFdMQ0x4TVRiOWpjMmdQU3ZybUF6Qm53Ryt4THdzczFRRk1iNWNPd240RWgrUEZJL1RiSXlzQ21jSUFzZzBldXpaNGZQVm5EV0Z2aEN0VzYyUFFLb0JYeFh5czJzWEsyL1ZqQmZsemd4VDllRXlVdDZmSHhzRUZCZjJlclBpY1RuOG9kc2VGZzd4NERWU25VQVBBaSttRTVuV3h3RXlSandYVDBHMUF3by9Rc2pIRjJwOXA3bzA5Y0hjSVlZVUFVZG9XR3ZtYnhwOVB2NDQvcUhHSWh6REpobXE5VUtWcGdCZWh2YzlsM2dzWnFZMWUyaG9kdDZQdGNUVm5JRWxEK3BaZ0NNUDgzSC9lWUF2UTJXRmxIQ01RYkFWQUVUWUx1R2ZRZ2dTTXRyLzdqeEF5eDdCTTBSVmxyTGkxU05sTStiMUg4L1NjeXZkUkhscUZGTGsweE42V1hOaG8zdWZzRHVjZlRxMVJFU0Z3ZUtxL1I1eXhodE1OczVHUkVSRWRFTFU3dzcrdlgzYW9qNS92V3VHelVnM2dDOGFZVWZtbEgzaDEwM2F6RGNWZXJlcllYWDFSMUh2V3NiV01JU24vQWZpek1qdHJmemJGbnl2K3hmMEtaNG93S294Z1RlYWdMZXRqbUkyMkR6SXdwTkNWdDZvQWVvREV0MVQxOTZ5NzlFM0swVXZvc3FwNjRIYTA5S0R4VGFLQUliTjVYOGJ2TE9YSjFsMVExSmdCd0JWQWo5eHFqY2JNTWNMNHhWK3V2bHhjTFUzN1oxZDVFdXNIN3Y1TnM3SThOeWh3UVV6ZlV1M0FRVXBNc0RuS2M0RGV0dkl5QTFUS2JjYUQ0eHdtbURnQXlXeStWd25xNVcyRTBBUHdmcEwzVTNCc1hlRmpEc0lGZ2FRUFhRVEtuREswM0FLNVNwOEJlQTAzdVBBY05HYTNUUWU2ckZwemdUT1lrd1lQRFQreTRneElCRDRGSXJYTFhnb2hFdnNJNTBETUJTc2YzZDV6c04xbjlVMDdMdzhzZGR0bUZNc3hVUkVSRVJFUkdYakFKODRtVURac1NSMmVnSmlUN1kyNlA2ZzBlOGZBS0FVR0FRVUthbE9FTXhTOVdia1VHRnpJMDhyeks1dzl1QytNNEZTNFp5aFd4QUFrd0tUQUtxdExiTjVlV1I2dEVNQmdFNG5STkFnMFUrR1dCdXhoMkVBTHdabUJKUVRuL1VqU3ovekhDYjZ3eVlnSmxGcDdER2hyak4veCt3RVFFRFdzQkdCQXhzQWNPT0FSUTdId01Hdmd2dytZNGQzd1ZHZ04zNkFSRVJFUkVSTnh2KzU4aXVPOUwvQ3ZqcGM3UjNVM29wWnpmb2UzTFZjNlR3VTRHZVo4aUxsNVlIS0JyZmhINy9RVmQ1ZEZqRC95UUJBdTFPVnF6TUdBUDB5Vks5WDcrYlBEYWtjQzdFVDRVNHgwOWJyMDlrUkdzK1g2c1ZtUnhQNUUrN2ZSdU96ZjNzU2daVG5xalhaS1R1YlZidm16L1RWeWhmZ05wdGYrQWdvUHhxdE9TdytYNDlTQ0JKMUlGR1BsUXYvZjE3S2wwZVNRNUhTa0JwQVJMbitJcXJjV0Z0N0U1R0JIeFJvVFh4anZMb01DdnZnUXUwNTBVR28xTTRtVG9JdUhhRFlBNXdmbmFPaC8xcU9rS0hwTERsLzNBNU51UnY1UFY1Y3lXZm1vK0lpSWlJNkEzNmZFQklwcHVvdXNwZDYrc3JoMENmRHdqSmRCdGRWN2xyZlgzbDRQV0hGcTgza2VsR3lxNXkxL3I2eWtIUTV3UGU2Z0lhK1VMNWhoZTFYRzJsTGROZnRUSlFXVGpUMytyMHQ4NzZCWGpUMVk1T2tpNW8rd1YrM3NFSDBCVkFLemVGaUhvMStPSUNydzZIOHZOMGxsOHZrZHZTOGVxWi9TOFk3UkUvLy95ek1OdFRQcEc4S1FIR0I0dXNldThGYVRCdUVNc3ZtRUwrL0lTQVlIdEU4K3VRVjVYKzJ5TmdnYjZEemtLQTdXOFhoWUwxV3l6RVp3SHEyMFpXMElHQWNCZFEzNzdWeGNSRFhRUkNCSHE3bENENXFTd1pXTFg1ZzZEUEIxZ0d0V1lRMUlNWUhhU0F5dTVCMVRwSTB2cnBJR3VtTi95NFpOVUhXam1Jb1c5amZXK2pYZVV3aG5aaytqcFNYZVV3aG5abCs3clNYZVdJaUlpSWlJZ0lEMnJINGRMazBZUDgvOEN3ZkEwSkFEOEI1UXNyS1B3RUNQcFBEOGVONmlzSndTTVRncUI1YzhuazM5K05IZEVDYnZ3WWNOUHZBaEVSRVJFUkVSSGJSbkoxUElIZ0xrakl1bTkwVGNqL0J4b3pFaEZvNndZRTBPdDlsZlRmaGdWUWZhK1UvcVlGbE52Ynk1ZURnSGJ0emRUWDRGQ2RmVzNIZ0t5QnFUKys0cFgrVjhjRytscEFsZi9xNnQvWEFxNjgvbjN2QWc3OXIrMFlFSURXLytyWVFOQUN1a0RwM2Z4R1JJd2Mvd2Uwd0lxYWdteTdHQUFBQUNWMFJWaDBaR0YwWlRwamNtVmhkR1VBTWpBeE5pMHdOeTB4TTFReE1Eb3lNVG8xT1Nzd01Eb3dNQnNCaVlzQUFBQWxkRVZZZEdSaGRHVTZiVzlrYVdaNUFESXdNVFl0TURjdE1UTlVNRGs2TWpZNk5UUXJNREE2TUREenphQVFBQUFBR1hSRldIUlRiMlowZDJGeVpRQkJaRzlpWlNCSmJXRm5aVkpsWVdSNWNjbGxQQUFBQUFCSlJVNUVya0pnZ2c9PVwiIiwibW9kdWxlLmV4cG9ydHMgPSBcImRhdGE6aW1hZ2UvcG5nO2Jhc2U2NCxpVkJPUncwS0dnb0FBQUFOU1VoRVVnQUFBUUFBQUFEd0NBUUFBQUJGbm5KQUFBQUFBbUpMUjBRQS80ZVB6TDhBQUFBSmNFaFpjd0FBQUVnQUFBQklBRWJKYXo0QUFCZTRTVVJCVkhqYTdWMWRpQ1hIZGY1NnZiWm1WbDZueHdLRk8yeXlxMW1NNHFBd003b0RzUjZDN2lZSUtlc0gzVjFRSGd5QnU1WVlKd0hqckI5TlFDdUJ5SXRoSGJBZ2E2VFp4ZUJnSE1KS0lTWitTREliMW9RZ1J0b1ZndGpHeUQ4UG1TR1FNSXBmSm1DTGs0ZitxNm8rZGFxNis5NjVQMVZmTTNQdjdWTjE2dWQ4VmQxZHA2bzZJVVNFakJQVHprREVkQkVKRURnaUFYVDBRT2hOT3hQSGlVZ0FGVDNzQTlnUGlRTGpKc0QwMjA4UGJlOXJNL092d2thQlF2UDB5emhHNkFTUU8wQXFEd211OW1PUFQzbnFQV3NZVjlxRkVkdVZJRFAvUVU0QlNmTUM5UkVxQWNiUkFhNTIwRkRFTGRwaGMzU0pDeVJJY0FEZ0FBa1NRWE9YTXM0Y2tySXhGRVVzMm9FTkJOU3FSMFdtSjJrVnYyaGx0dlJkYVZQSHZQdHFkcFZ4amxEMUFISUg2QXVwRGJvdkgxbnFrZ2xsTGQzYXBuUUpValYzNjJkbURFbmpPeWE1RlVsdHNFcXFiZHR4YTVEYnBweDN1UStzTkx2Nm1ibENjd0xJb0tsWFRRLzdyUWttWDRJS3pkTXY0eGd4YmdMTU8zcllYeVR6dWhFSkVEamlTR0RnaUFRSUhKRUFnU01TSUhCRUFnU09TSURBRVFrUU9PSjhBRFB1dFBOL3pHZ3lIOEJ2Um9ETEdkTVQ1d1BJS2JqTjAyVStnTnNkbnVWOW9ValNiRDZBbndkTXJrSzdnVll0MzMxMXU4enYwcjV2Zk5xMUw4eHNnUHA4Z0F6MjBmQWlsT1J2czh0ZHNYM21BMGk1azFOM3g1ZEJ1ZTdpY3lHZ3p3Zmd2dXM0OE9vRitERHU5dWt6SDBCcWYzNTVzOU9IbkxNTm1xUTBGMmpqREpJY3JyTStIMEFpbDZ2L0tVb2UzY0VDcGw4NVhlY0RURHYveDR6b0RnNGNjU0FvY0VRQ0JJNUlnTUFSQ1JBNElnRUNSeVJBNElnRUNCd25wNTJCbVFOTmNaUy8rMWhwNC95ZjdCWjlJcFV3elJ5UU13ZnRYVUh1bUZNb3VYNEpJRUQwOWZ2c0QwQXRKRTNSTmcxWC9qUFRKNklXTnpuYXh2WXJnVStvQm5GUGFBRmN4VTg4Q21DUG4zaFVrRThSU0hEMitPUXZFV1Q2WjdNMENvbTdCdVFTeWdRaVIyekExWWkxL0t1WEFOL2kyMmJydUNzcEdVTUhUNkluMG5VVjdaSURLbU1uckZSTm5VdWxhRjcyUEpBZ2wzVlhwWk9iZ1pyTEJHaDZFMGdlcmNjVnlvZWQ3ZHE0bjNFVEQrMlNnWHowdHEwQnFRbjY2SGJYUVUzZTVER3c2dUo4UXZFeXVRdDBNMWpXNGVwaS9icG9Wd3R5cGU1ekU5a1dib3E3NVZvT1RIZHc2RThCODUxK2k4ZklPQjhnY01TUndNQVJDUkE0SWdFQ1J5UkE0SWdFQ0J5UkFJRWpFaUJ3Nkl0RGk4MVNwd2YzZnVDVGhFOE4rSGhFMjhWenJZMzJUYVVSS2dJVUM2Tjh0bnR2VndIalF6dGRycHo3MVlDMDFXemxqZXMxanAzNUtZdkRYb1plNnhvZ0k1Y0ErTVdoOGhKT0g0OTJqemszUGdyWWRQbHREV0hQdjE4TjJOTG9HY3RyZTQxaVZ5Vnc5VURkbHFiWEZ1anFROEUrMjZtN2xtRGJGb2o2emFSeDU4Q214NzJGdXp2bmZodlM4ejYzdW1rVFErNWFXRjRaaDF1ZlNKcG0yV0ZzYzFnbjlUQk5DZUJmQVVtRHVINDVrS3ZRSjMzMzJuNTcrcTdZTGdLNDYwQTJvSnNBTW9Fc0JHajJGRkJWUUJ1UFZhTG80TFdUOWltbDN3WlZ2dHZlUTBpcHIrYlM0Z3JPN3lBZ3BWL0U1TzhCWEpxcnZSdWs5YzFtTGh2T0I1QXFRTCtCa1lvdlowNHFvcVJETFpwNzJoZG5BamNCcFJJY0dCdElIRFRXNEFmSnZBZmxhMi84Y29tbWx3QS9YelU1dDRpUXI0SmRZdnRWTDE4T3YydHc5eWx0a3A3Mkw3Vngzd093aVBNQlZQU2NiMHhhT0VRQ0JJNDRGQnc0SWdFQ1J5UkE0SWdFQ0J5UkFJRWpFbURjbUxQSEtuTStnQXVTdjhxbjZMMk8vdTVKbzN2dUVzZGc5NHlWdlQ0ZlFJTGtNZmNaSlN3OGRxc2VDMHp0NkxxNFNsNGVuamlYdjdvTmJJc3ZhWjhTcW9FZ05WdnUzWDdyZzQzcTBzaDJiLy8ySFdnR0pJK2RLM1ppRGFlZXRYazdwY1dqNUNYMUwrbXhnTDhIc0UwNmtQMVYxYVdoemFTRnJPVzRwMDNaOXpBZytQcjZrZ1pudVhTNTlOMHJtMmNTNXZzQ3FpcHMrZ1p1OHhLU05JaGJTUDJXUUxzWGI3ZmZRa0l5bzZ1SDhOY3M3UkZ3ektoNkFOVlIyQ1pycmpjSnVKMnRYYStPaWRLRDhEMkUvc21GR0U4cjVsTFFlNDhaTWI5K0NUZ29NOFYxNEZrSG4wMzM0bkVnemdmdzhmYTd0NGVRVWMwRjRLZFV5RlZmWFlUc3U0c1VJWk9HVWpWRTJ3azFFd0h2RFNSckIrN2pMTFhkQkk3RDJlbzNhN0h0amFiUFRTUUFCMEZueHJnK2lPNWdIVFBUTlI4WElnRUNSeHdLRGh5UkFJRWpFaUJ3UkFJRWpraUF3REY3QkVobnkxdTI2REFKNE9mTGxsdzI1Q1cxaFVoeGVDeFA0dFBlQjJGbTBHeTdlSi90cEJPc2RNalBJUXFTcEdLNHRsdldWMWlnTjRCM2dVNEFlWlJhSHUwdVduV0tRNVlDVkE2VThtdjNzcTUvcFF4eGFJbGZmR3UzcWJyYSsvaTlhbjdCVVJGQTMrNjlqc29mYnRzeVBVR1NtLzhETmkyNWE4KzYvZyt3SXF5ZExWTG4wOWRuRkpoYS9MYVBDQTdGVUxEZjZuWUp4VndibS9uTnVUaG1MK0w2TFoydFM4eHc1dllSMGx2T2c0STZKU3hCM1V3bTdGTVovQ1pFMkNlVm1RU3lUY25nYytqYVBrRW4rSUs5QXI0THFoZEdxRlZtMzJERS9kWUwzOWRLMk9RWkJhaDJIMUU0Z3ZsZCtmWDgrTHcySWdJQXR6K0EzOHIxdWwvZXZVT05KTTF1SHVVUXVyYkU2MXdCOXc1Q2djTC9oUkg2ck5qcSs3aGh2NHZvQnYvdEk0TENyTTBIbUpUNUl5eVlOUUpFSERObXp4Y1FjYXlJQkFnY2tRQ0JJeElnY0N3U0FmcWxKNkEvRWYwbnNaUWZ6ZDYzT3RQSUNERE1LMjRYdzlhYVhuWjQrbDBnM05CbUN6UTFZaDk3NWZjOU5uYS9FejFPNGxkNEZFYzR3cVA0RlV1QnZqUDNhN2wwelpLR1hlNktDVHhuSEhJS3h0d09vcXMwb0FFTlNVVVZ0am8vSkZnT0tqKzN0YmhxQ0Y1N2RveW9yOGxIbXJSUEp2cU41SG9PdWR5dEdmSFhEUGtTclJQUkVpMFIwVG90Q1RYQXAxR0hWRVArRWpYRXRuY1p5eEFGays4QkFPNVlKblBjeHBkeEQwQ0syK1c1TzdoZ1plTVhMQ09LeStXM281cnNKcjRodEw4OVpCdExaTmdIWVU5TFlROWJTZzhBYkJseU45NVhjcGZsMEl4L0g4dDRFRUFmOTQyd2VqNkJMVWJpbTVzdUw1K1hzWWIzeSsrbHBvSUE5L05QZFJxR211VjdBSDZFSTNHMnoxTCtlY01hNHFOQzdDdDREOXVDL0pMbVNyNmtVREhEMndvRnR2QzJJU1dvaXpQNUZZWWZ4Uy9LNzU5ZzgvQWdQb0VFcDRSY1hoUmtwa2VTbC9Ndm9FNFlMYzBJWHBqL0QvRFA2dW5xV25ib1VQQWpITlhDNkZuSUNMQWlhTE8xRzhEVkF3QnZsUlFnWE1KYlRJaUNBblh6WjBpTnozcnVmbDNNNlFiK0czOEk0Qit4d2NaL0d5Z3B4T2RnSysrcDlON3FPRkNZLzQrd3JGUEE5MzZXTTMrOUFnRzVvOHJhems5Wm1hc0hLQ2dBaS9tUnB3N3JUZDU1NDlQRUVuNmVmenZMU0IvRXU5akFmUUFiZUJjUDRmOE11VGtMcWUweVUzZEg3d3J4T2Z4TjdWeG0vaS9nUVFEL0FBREZCZHovTWJCdWZyT0FwM0FLcDdDTVU5Wk84Z0U4Z0FjQWNDM3Nwc1A4UUE5djRSSXU0UzNMZE00K0RqREVFQWN0Ny9PWDhWdjVzVnpMWHc5blFQZ0ZOckNCRXlDY0VhZVVibG1NZjA3NXMyRUpMcmhDZklzNWw1SCtCajZHN3dBQUx1Qk9KbWovUkZzdjRySXp6ZytGa0s0ZW9JY0RJRy83QjB6MTl6VTVSNEhmTUQ3TktzcGF5UWF5T3lLOW4zZ1k5d0g4cFB4OUh4dkNqS0l1SGZ4Unh4RDhoTm1mNU9YTDdzOUs4Nk44REV3ZGoxSHVSNWlYTmZuTDFzY1YrMlBNalFrK0JoWVBTZFduTFJkOTVoR1FlNGh6UGVnMWZRd0dqYXlsOXdtaGxwRFhYendFRHRTenMrTU83Z1A0SGVYM2U1WWJxY21DQUp4WFd2cjR0V2VZenRTVU5ieXZ0WDdFK1FEQlk1RjhBUkV0RUFrUU9DSUJBa2NrUU9DSUJGZ3N2SUpYbWtYUUNaQ0M0RnFZYlFlVmpveXVqeFo5Y0I3MUxlWHh0ZTV2R3hxUHZVTkR2bTNJdVVFbjFWdStOZ0U1QVB5bG8zNEdHTFN1dDlQNENyNkMwNDVRSTR3d0tuOHBkWklTVVovNlJKVFdCaEYyYVNrZlJyaEc2elJraDBGdTVBTVIyM1REa011ZWJQUG9zK0czRkU4MkVkRVdPeEJTSFBYNDdvR2NMTXc2cmRQNmhPUkZtR3hReDVRTXFCaVFTODNobXR3NlJOdUV2SFFwby9zdUVSSGRGV3UzR0V3YTBUa0NRVStnbjQraDFkVVQ5UnFOZzNGeUVOR2Y1WDlYclJYVXAyd0NTdCtpSDVZVVhBWW1BdjA3Z2U0UjZOK0luekpCdEo1TDFnVTVSUGxwV3FMVEZubFIvU01hTmE3QkF0dXNmTHNXZTV0TmYwaEVJMHFya1VUVkhWdzRLZmV3MVdxakZuV3VBTzg1ZkFMNytERitnSi9qUFl0UHNJODlYTUliVnBkdUYvd1RmaGZmd3dhK2h5ZndMOVpRMlZqNmhpalh4eXgxL0xMbUo2d3d3azBBeVAvWDYrOVErOFdEbjIxeDF1TU1BSHdKd0VwK2VVenhBY29lNElZeG10Nm5HNDE3QUxXRHMvVUFWNG5vTDRqb0ZiYUY5SW5ZMWordUh1QnZDZlJkQW4yTGxwajBIM1gwQUlVY290emVBNHkwM0ptWGdJR1M2d3lEUnVYN3FpYjdLbE9ENStoY1BYVXppWDVPaEhyMWo0TUFuNkhmcE0vUitmeXZpZm4xV1hFMkFxUkVsRkthWHk5TitYZUk2TnY1MzkvVjVFWDViTmZ3U3Y2NFE4N0hyNjY5MlhHK0lRRk1kMTBxMUpEc2F0TE9td3JVVDEyMlJFTkN5YUVCRTBJbVFOMWYxOFQ4WnZGa0F2RHlzL1JkZXBoMjZXSDZlL3EwSVYrdDVXNXRyUExLL0xBZXVvbFRVVzZUM3MxdkJFMzVpSXByLzI1YkFzQnhVSjZOMURNOGIrQytWYjVsVkREM0ZKQ1dSZWNKWWlkZ2hzY2N1ZXNpaDhQOFBpNzNnZ0lwSTN1S1BzeHYvTGJwUTNxS0ljQlZHdFhqK2lidk5xaCtIOHJmZzBxVGxtWHpteFRZcWtsZDA3cGxPUkZSejJHY0x2S1IwL3l1eDhDS0Fxa2w5blBsOStkcXNhMDlrSzg3ZUIzditnV002SWdCb0h2c3g0UVJBT0NXZVRyT0J3Z2MwUmNRT0NJQkFrY2tRT0NJQkFnYzRSRWdjeHNQR01tZ2ZEWjYxRU9QYlRmRk9idXJWZ213WGxiQWVtdDlOTFVxU0VIWXliL3Z3RGFyNFZxK3FIUzNSb0hmeHk0dTR6SStoVS9oaC9oMExXWldPMWZ6WDltZXBlcSs1NXY0TEFoUDRBa1FQb3ZOV254NSs0cGg3Y0YrNkpDN1FneHJhUmdqUU9iWmRTTGFwU0VONlpBcXB3YzM0Q0VOWnBBelJQdGpsT2RyblIxVXliQkRvQjFyTG9yaGtIUHNVTmVwOHZnWU94YTZROE04QjVtZUFaTytmUnl2OEZLa2xtRXcrVXdWTzdYb3ljWWErN2szQjB6NTFCanJ4WGMxUURaS3ZFTkVoMVlqVHBNQXhWZ1duMFpoOXVxenJ1R2FPTlo1Z2o1Q0NTWDBFVHJCR21SSUlLSkI2ZFBuY2ljVFFQV0htQ3VYTW05SXY2empPZ0dnemFLb0V5RFR1bDJPeVhJRU9FT3IrY0VRWUpoWDR3NDc0OGRGQUhKV2dkdTRjdngxUmNiMVVEdUtuRE4vS3VvblNzcGpqVW45ZWg1cmx4OVNwV3oza09MZ0NXQlBuOG9aVjd3R0tuMmNRd0tkWXdrQWhRQjhuN0pPbTdSWitpc1pBdXprVmNkUCtYSXhYQTdSVE1vVHlPVlRrMXAvTmVNcHc4Q1FmNXVJMXZLRDZEcWpZVkM2YkxuMGlVNTNJc0JEK2RtSHl2OW03S0wvR2hMUk9Vc1BZSysvUGhHZG9UTzBTbWRvazRpdVplZlY5d1Y4Z0JRMzhmbjhGa25lY1I5T3ViU2J1RnZLcGJCZTdtT1NyZEUzOFUzOGNmbjkrL2c5VFpZcU01NkFLL1V4Y2F6alM4cXZsL0V6SWZXc2xzenlyd0Q0RUFEd1MyYURHY0tLTnMvS2ZNSEZJOXA1d3M4TXVUNWY2Q2ErYk16YXlyYnd6eWJMN2pIYitSTWVCL0JmQUlEL1ZHcFg2MkFQYVVoRDJxWFp2QWtzV2grZnhqZkwxcC9ocmhHM243ZjcxS3AvczJ3NzU5alVkK2dxVWQ0T2R4aDU5dm1RNVViUG5HaGp0dC9INkRIanY5eC9OTDhIR0ZLZk5tbVROb2xvczM0SlVLK3hOdlBQL2xQQVhTcm14bkxWTTNDa3NFUGJ0TTFPcDBJZU82TVFSd0VTZnBsbjEyb2hoclVPZkdqRVBGYzdtdDBERUEzTHcwSUEzMVk0TFFMSVI2cTArcnRrZXMyejF1RlRPbHYrczU3bGtGSXFiaWVUV3R6UDAyZW9KOVFTRVpUN0RGMjJsRDhGWlAvTmJlanFCREVwTWhTbGF1bTBNa1ozc0lxSDhCS0FGL0UvckhTRUZkek0zMmFRNG4veGE5cWJEVnozU0lDK21lVWs1ajIzUUNSQTRBalBGeENoSVJJZ2NFUUNCQTZUQUVQcmZ1RVhjYXU4b2J3bGJva2FNVS9RSGhSdUV4SFJiZVlCNW91MWg1QXZUdVZoTHg1alB0UWZ6NVRHZmNZSWRsRjdlaXh3a1ZHNFIwUjcxdVIrNmprZ0E0OFF4My9zS2ZUZm0zcHVKa0NBMjFUNG5Ndys0RFdXQUs4eEN1V0JvTm93aE5YOEhFbmN5NnRjSVZ4eW9yMXl1R2lQbGZ2bGY0Nk82dXN6bW9IMVBzQ0dwZ1FBZ1YyNFdUZC92WjlRMTlieUJuU0ZHQkhSMDdSRVQxczErSlZ1UVFtUVhmOExyN1BlQitnRmw2cGhtMnpMd2xUejd6S1NRZm5KWHlaY3hrRnUrRFFuQWlkL092LzJ0S0JoVDBoamdRa3dJQk9xQWZ3SndIZmV1cVo3akVldWlHY3p2eThCVXJMM01WU09zTmY5OVJYc0JPZ1QwUjd0MFI3NStCWG01REFyd1B6ZWxBQ3VvekJTM2Z5WjJXM21uNFVlWUlGdkFxdjJueXArNThvTVgyY0o4SFZHb2FzSHNCMXFEOFRIbi80OWdKdUNjM2h3UmFzWDhpbFcraFNqMEZVNWRxblUrblVEajFxSDZQb1VrSVdadXRIR1Q0QTkxc0JxRlR4Zmt6NXZOZU5BU0ZDaVI5dmU0M2lQQlNPQXZ6djRTUXp4NS9uM3IrRU4vT3UweHpDbmhMYnZBcHBSeFBrQWdTTjZBd05ISkVEZ2lBUUlISkVBZ1NNU0lIQkVBcGdZZ0ZwTDV4QXFBWWpaTmtFSFlWeXZoSmdXYmdqdk5nZUFBWFpiUytjVHhpZ2QwYTR3R3FkNkEweEpFUysxYWhqUWJ1NElHdEN1b0FHRWZIbVRMdC9XTkxWeDF3NGNnODBEajloRkRkaTF6TlhCVjUvTmhCSUIxSGoxYWg2VWNRYjVDbnRaZzUwQXR2ZytCSkNrc3FPbkxsMlFJV0ZiSWZscEhTNENaQ2FzZnV0U0VFcmp1VFRZQ0dDUDd5WkFVeFBicGZ3R0RITjU4RGVCZDNBQjMyaHhQYm1EQzdpQWJKM2NCV1hNUEZIT1hoQjN3aTAwMk9DS2Z6eFlKRzlBamVXTGZBL2dPZ0s4QjFDZFFZUTdlRWxzWDVTenYvaWNMNmhQTHJiY1ovZjU3YVJ6aWVnTk5ESEFybUJpV1RxSGlBUUlISEVrTUhCRUFnU09TSURBRVFrUU9DSUJBb2RKQUZKZUxCNFJBQ29DcFBsV3FXZHgxckxYZmpaMGRNMGlqWmhMRkFSSWNWaHVEdk1JRGxramIrQUN2b1lYY2VqeFFnbHplR0hiR0lIY1BtWjVoQTE1amUwUTBWOFJpT2hGeXZiRnRZMGVwK0pld3RXb3ZPdDMvMWpsOGJBY2hWR0o3aEVJOUNJTkNIU1BiRytuM3FGemxEbGxVNnRTZnFmYXBYdzc5ZFA1bjduVDdXbVNObHd2NUNUS2w4cS9wZFlPb2NDT2t3Q0E4d0RlQkFDOEJBQjRFeHM0WDl2S2RBT2J1SWtyZUFTWGNJZ3J1TjZvcS9tazB1a2txSTlBZnp6L1M4QXZ2enFkLzMwUy9PYnlId2V3a3N0WEZtdThmcExJZkFFcERuRmZlZEhSUFd4Z1Jkc0p0MENLUTl6QkJlemdDbHZKbE85VVg5K3QvcmUxMy85aDdKVTdhWG1FRFhsWHNFdlZvdWtSOFp1NFpMN3c3SDBWdHBmSzJPYkw2TzhXcjg4M21yUThIcGFqOEFabTc4TzRqemZ4TERiQXY1R2ptZzF3QVdEY29tcnJYN0ExdEl1THloMjhqbXY1ZytBYnVCWmZGaDhLNG55QXdCRjlBWUVqRWlCd1JBSUVqa2lBd0ZFUndQVStnSzd5SjNHOWxGL0hrOGN1bjNUNXBpMXZpMXluNjMwQVhlV3ViZVltTFo5MCthWXRiMzFrSHhlSlEvVStnSzV5MTBhVGs1WlB1bnpUbG5jNHNrdkE1YkpEU0pRUnZNdk1OeFdtL0lVOGJvSVhEUG16aW40d1o1L2wxRnZrU1l2NGw5bjRYUGxjNVZmejN6eisyMGlVRUxiNGtsek5CVytWaHNnR2dxclJvS1QyUzVkekdTbmtDWURuQWJ5dW5SbWZmcC80YnYxcWlLYnhFL0FMektqQmIxSzAxT1YvQWdENGEydjlGV2MzOFE3R3NraE5KWUQrTHVwNkJseHk0QVc4RHVCNXZDWldzSzJDM1BvVFZwdXZBZHo2WlFKMHI1K2tQTjlPL2pqZVVjdy9GZ0tjN0s1Q3dZZksvK1lnWjQ4Z1ExMk8za1pEb255MmlVOGVjVjF1c2o4VlpJL2pubWIrc1dEY2w0QVJnRnRvZndubzNzWGI4MWZYMEs0TGwvVDc5U0N1SHNxVy91TzRwNWwvREQxQWRoUDRLaXQ3bGZrbXlaOEhjQk0zODIrcS9IVWxGakZuWDFla0xqbEVPVmk1bm4rcW5YMVZrN2prYmV1bktCKzFscitqbVo5UHRTbUNlQXliOW1Qb3BPV2R4d0VXZnlCbTJnTlJrNVozSmdEb1NicGVLcjlPVDlhQ2RwVmZwRnVsL0JZemlERnArYVRMTjIxNXl5Tk9DQWtjMFJzWU9DSUJBa2NrUU9DSUJBZ2NrUUNCSXhJZ2NLak9vTHJUVWNlc3l5TmFRUGNHTHBmZmp0alFYZVVSTTRmNkphQ2I2WTZjR3JxMTNLU3poZ2dOSmdGY0JqekNrU2hmeHBIU0Q5VGhNcUE2SllvRHRmVDFSMWhnRW1BWkVBMjRqR1ZSZm9SbGtTQUVlY0pFN3FHd0lxNDdIalBxbDREbEZscjAyTEtHYnUzWFJhQ0lodERmRjZCSmFtRm5YUjdSQXRFYkdEamlRRkRnaUFRSUhKRUFnU01TSUhCRUFnU09TSURBTWI4RTZNVUJvWEZBSjBEM2NUWkNINFQreFBQZHd6NVdKNTVLQU5BSnNKci9UUnV1MXAyWi8yRGEyVndFNkFUWXovK21DMWZyanVZZkkzeDdBRUt2OXRjTXpLb2tGcGw1N1RRc3pCL3ZBY1lDZlViUVBoTHNzMjZXaFBscmhpMnZVSVg1VjBYNVFid0hHQmQwQWtnOXdHcHBtdUt2V1NlOFZ6dFRKMUZQMGM5UlREZC92QWlNQWJQVUEwVHpUd0crUFVCM3VOL2YwY3o4UFV1NGlFYnc3UUdPQTFMdndway8zZ09NQWZxRWtCNE8wSnZKenBYeVR0LzhqT2lJT0NNb2NNeXZMeUJpTFBoL2dqOVFwaGQzdDhnQUFBQWxkRVZZZEdSaGRHVTZZM0psWVhSbEFESXdNVFl0TURjdE1UTlVNVEE2TWpFNk5Ua3JNREE2TURBYkFZbUxBQUFBSlhSRldIUmtZWFJsT20xdlpHbG1lUUF5TURFMkxUQTNMVEV6VkRBNU9qSTJPalUwS3pBd09qQXc4ODJnRUFBQUFCbDBSVmgwVTI5bWRIZGhjbVVBUVdSdlltVWdTVzFoWjJWU1pXRmtlWEhKWlR3QUFBQUFTVVZPUks1Q1lJST1cIiIsIi8vIHN0eWxlLWxvYWRlcjogQWRkcyBzb21lIGNzcyB0byB0aGUgRE9NIGJ5IGFkZGluZyBhIDxzdHlsZT4gdGFnXG5cbi8vIGxvYWQgdGhlIHN0eWxlc1xudmFyIGNvbnRlbnQgPSByZXF1aXJlKFwiISEuLi8uLi8uLi9ub2RlX21vZHVsZXMvY3NzLWxvYWRlci9pbmRleC5qcyEuL2pxdWVyeS11aS5jc3NcIik7XG5pZih0eXBlb2YgY29udGVudCA9PT0gJ3N0cmluZycpIGNvbnRlbnQgPSBbW21vZHVsZS5pZCwgY29udGVudCwgJyddXTtcbi8vIGFkZCB0aGUgc3R5bGVzIHRvIHRoZSBET01cbnZhciB1cGRhdGUgPSByZXF1aXJlKFwiIS4uLy4uLy4uL25vZGVfbW9kdWxlcy9zdHlsZS1sb2FkZXIvYWRkU3R5bGVzLmpzXCIpKGNvbnRlbnQsIHt9KTtcbmlmKGNvbnRlbnQubG9jYWxzKSBtb2R1bGUuZXhwb3J0cyA9IGNvbnRlbnQubG9jYWxzO1xuLy8gSG90IE1vZHVsZSBSZXBsYWNlbWVudFxuaWYobW9kdWxlLmhvdCkge1xuXHQvLyBXaGVuIHRoZSBzdHlsZXMgY2hhbmdlLCB1cGRhdGUgdGhlIDxzdHlsZT4gdGFnc1xuXHRpZighY29udGVudC5sb2NhbHMpIHtcblx0XHRtb2R1bGUuaG90LmFjY2VwdChcIiEhLi4vLi4vLi4vbm9kZV9tb2R1bGVzL2Nzcy1sb2FkZXIvaW5kZXguanMhLi9qcXVlcnktdWkuY3NzXCIsIGZ1bmN0aW9uKCkge1xuXHRcdFx0dmFyIG5ld0NvbnRlbnQgPSByZXF1aXJlKFwiISEuLi8uLi8uLi9ub2RlX21vZHVsZXMvY3NzLWxvYWRlci9pbmRleC5qcyEuL2pxdWVyeS11aS5jc3NcIik7XG5cdFx0XHRpZih0eXBlb2YgbmV3Q29udGVudCA9PT0gJ3N0cmluZycpIG5ld0NvbnRlbnQgPSBbW21vZHVsZS5pZCwgbmV3Q29udGVudCwgJyddXTtcblx0XHRcdHVwZGF0ZShuZXdDb250ZW50KTtcblx0XHR9KTtcblx0fVxuXHQvLyBXaGVuIHRoZSBtb2R1bGUgaXMgZGlzcG9zZWQsIHJlbW92ZSB0aGUgPHN0eWxlPiB0YWdzXG5cdG1vZHVsZS5ob3QuZGlzcG9zZShmdW5jdGlvbigpIHsgdXBkYXRlKCk7IH0pO1xufSIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0yNC5cbiAqL1xuaW1wb3J0IEZvcm1CdWlsZGVyIGZyb20gJy4vRm9ybUJ1aWxkZXIuanMnO1xuXG4kKGRvY3VtZW50KS5yZWFkeShmdW5jdGlvbigpe1xuICAgIChmdW5jdGlvbigkKXtcbiAgICAgICAgJC5mbi5kYXRldGltZXBpY2tlci5kYXRlc1snemgtQ04nXSA9IHtcbiAgICAgICAgICAgIGRheXM6IFtcIuaYn+acn+aXpVwiLCBcIuaYn+acn+S4gFwiLCBcIuaYn+acn+S6jFwiLCBcIuaYn+acn+S4iVwiLCBcIuaYn+acn+Wbm1wiLCBcIuaYn+acn+S6lFwiLCBcIuaYn+acn+WFrVwiLCBcIuaYn+acn+aXpVwiXSxcbiAgICAgICAgICAgIGRheXNTaG9ydDogW1wi5ZGo5pelXCIsIFwi5ZGo5LiAXCIsIFwi5ZGo5LqMXCIsIFwi5ZGo5LiJXCIsIFwi5ZGo5ZubXCIsIFwi5ZGo5LqUXCIsIFwi5ZGo5YWtXCIsIFwi5ZGo5pelXCJdLFxuICAgICAgICAgICAgZGF5c01pbjogIFtcIuaXpVwiLCBcIuS4gFwiLCBcIuS6jFwiLCBcIuS4iVwiLCBcIuWbm1wiLCBcIuS6lFwiLCBcIuWFrVwiLCBcIuaXpVwiXSxcbiAgICAgICAgICAgIG1vbnRoczogW1wi5LiA5pyIXCIsIFwi5LqM5pyIXCIsIFwi5LiJ5pyIXCIsIFwi5Zub5pyIXCIsIFwi5LqU5pyIXCIsIFwi5YWt5pyIXCIsIFwi5LiD5pyIXCIsIFwi5YWr5pyIXCIsIFwi5Lmd5pyIXCIsIFwi5Y2B5pyIXCIsIFwi5Y2B5LiA5pyIXCIsIFwi5Y2B5LqM5pyIXCJdLFxuICAgICAgICAgICAgbW9udGhzU2hvcnQ6IFtcIuS4gOaciFwiLCBcIuS6jOaciFwiLCBcIuS4ieaciFwiLCBcIuWbm+aciFwiLCBcIuS6lOaciFwiLCBcIuWFreaciFwiLCBcIuS4g+aciFwiLCBcIuWFq+aciFwiLCBcIuS5neaciFwiLCBcIuWNgeaciFwiLCBcIuWNgeS4gOaciFwiLCBcIuWNgeS6jOaciFwiXSxcbiAgICAgICAgICAgIHRvZGF5OiBcIuS7iuWkqVwiLFxuICAgICAgICAgICAgc3VmZml4OiBbXSxcbiAgICAgICAgICAgIG1lcmlkaWVtOiBbXCLkuIrljYhcIiwgXCLkuIvljYhcIl1cbiAgICAgICAgfTtcbiAgICB9KGpRdWVyeSkpO1xuICAgIGNvbnN0IGZvcm1CdWlsZGVyPW5ldyBGb3JtQnVpbGRlcigkKFwiI2NvbnRhaW5lclwiKSk7XG4gICAgZm9ybUJ1aWxkZXIuaW5pdERhdGEod2luZG93LnBhcmVudC5fX2N1cnJlbnRfcmVwb3J0X2RlZik7XG59KTtcbiIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0yMC5cbiAqL1xuaW1wb3J0IEluc3RhbmNlIGZyb20gJy4vSW5zdGFuY2UuanMnO1xuXG5leHBvcnQgZGVmYXVsdCBjbGFzcyBCdXR0b25JbnN0YW5jZSBleHRlbmRzIEluc3RhbmNle1xuICAgIGNvbnN0cnVjdG9yKGxhYmVsKXtcbiAgICAgICAgc3VwZXIoKTtcbiAgICAgICAgdGhpcy5lbGVtZW50PSQoJzxkaXY+PC9kaXY+Jyk7XG4gICAgICAgIHRoaXMubGFiZWw9bGFiZWw7XG4gICAgICAgIHRoaXMuc3R5bGU9XCJidG4tZGVmYXVsdFwiO1xuICAgICAgICB0aGlzLmJ1dHRvbj0kKGA8YnV0dG9uIHR5cGU9J2J1dHRvbicgY2xhc3M9J2J0biBidG4tZGVmYXVsdCBidG4tc20nPiR7bGFiZWx9PC9idXR0b24+YCk7XG4gICAgICAgIHRoaXMuZWxlbWVudC5hcHBlbmQodGhpcy5idXR0b24pO1xuICAgICAgICB0aGlzLmVsZW1lbnQudW5pcXVlSWQoKTtcbiAgICAgICAgdGhpcy5pZD10aGlzLmVsZW1lbnQucHJvcChcImlkXCIpO1xuICAgICAgICB0aGlzLmVkaXRvclR5cGU9XCJidXR0b25cIjtcbiAgICAgICAgdGhpcy5hbGlnbj0nbGVmdCc7XG4gICAgfVxuICAgIHNldFN0eWxlKHN0eWxlKXtcbiAgICAgICAgdGhpcy5idXR0b24ucmVtb3ZlQ2xhc3ModGhpcy5zdHlsZSk7XG4gICAgICAgIHRoaXMuYnV0dG9uLmFkZENsYXNzKHN0eWxlKTtcbiAgICAgICAgdGhpcy5zdHlsZT1zdHlsZTtcbiAgICB9XG4gICAgc2V0QWxpZ24oYWxpZ24pe1xuICAgICAgICB0aGlzLmVsZW1lbnQuY3NzKCd0ZXh0LWFsaWduJyxhbGlnbik7XG4gICAgICAgIHRoaXMuYWxpZ249YWxpZ247XG4gICAgfVxuICAgIHNldExhYmVsKGxhYmVsKXtcbiAgICAgICAgdGhpcy5sYWJlbD1sYWJlbDtcbiAgICAgICAgdGhpcy5idXR0b24uaHRtbChsYWJlbCk7XG4gICAgfVxuICAgIGluaXRGcm9tSnNvbihqc29uKXtcbiAgICAgICAgdGhpcy5zZXRMYWJlbChqc29uLmxhYmVsKTtcbiAgICAgICAgdGhpcy5zZXRTdHlsZShqc29uLnN0eWxlKTtcbiAgICAgICAgdGhpcy5zZXRBbGlnbihqc29uLmFsaWduKTtcbiAgICB9XG4gICAgdG9KU09OKCl7XG5cbiAgICB9XG59IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTE2LlxuICovXG5pbXBvcnQgVXRpbHMgZnJvbSAnLi4vVXRpbHMuanMnO1xuaW1wb3J0IENoZWNrYm94SW5zdGFuY2UgZnJvbSAnLi9DaGVja2JveEluc3RhbmNlLmpzJztcbmV4cG9ydCBkZWZhdWx0IGNsYXNzIENoZWNrYm94e1xuICAgIGNvbnN0cnVjdG9yKG9wdGlvbnNJbmxpbmUpe1xuICAgICAgICB2YXIgc2VxPVV0aWxzLnNlcShDaGVja2JveC5JRCk7XG4gICAgICAgIHRoaXMubGFiZWw9XCLpgInpoblcIitzZXE7XG4gICAgICAgIHRoaXMudmFsdWU9dGhpcy5sYWJlbDtcbiAgICAgICAgdGhpcy5jaGVja2JveD0kKFwiPGlucHV0IHR5cGU9J2NoZWNrYm94JyB2YWx1ZT0nXCIrdGhpcy52YWx1ZStcIic+XCIpO1xuICAgICAgICB2YXIgaW5saW5lQ2xhc3M9Q2hlY2tib3hJbnN0YW5jZS5MQUJFTF9QT1NJVElPTlswXTtcbiAgICAgICAgaWYob3B0aW9uc0lubGluZSl7XG4gICAgICAgICAgICBpbmxpbmVDbGFzcz1DaGVja2JveEluc3RhbmNlLkxBQkVMX1BPU0lUSU9OWzFdO1xuICAgICAgICB9XG4gICAgICAgIHRoaXMuZWxlbWVudD0kKFwiPHNwYW4gY2xhc3M9J1wiK2lubGluZUNsYXNzK1wiJz48L3NwYW4+XCIpO1xuICAgICAgICB0aGlzLmVsZW1lbnQuYXBwZW5kKHRoaXMuY2hlY2tib3gpO1xuICAgICAgICB0aGlzLmxhYmVsRWxlbWVudD0kKFwiPHNwYW4gc3R5bGU9J21hcmdpbi1sZWZ0OiAxNXB4Jz5cIit0aGlzLmxhYmVsK1wiPC9zcGFuPlwiKTtcbiAgICAgICAgdGhpcy5lbGVtZW50LmFwcGVuZCh0aGlzLmxhYmVsRWxlbWVudCk7XG4gICAgfVxuICAgIHNldFZhbHVlKGpzb24pe1xuICAgICAgICB0aGlzLmxhYmVsPWpzb24ubGFiZWw7XG4gICAgICAgIHRoaXMudmFsdWU9anNvbi52YWx1ZTtcbiAgICAgICAgdGhpcy5jaGVja2JveC5wcm9wKFwidmFsdWVcIixqc29uLnZhbHVlKTtcbiAgICAgICAgdGhpcy5sYWJlbEVsZW1lbnQuaHRtbChqc29uLmxhYmVsKTtcbiAgICB9XG4gICAgaW5pdEZyb21Kc29uKGpzb24pe1xuICAgICAgICB0aGlzLnNldFZhbHVlKGpzb24pO1xuICAgIH1cbiAgICB0b0pzb24oKXtcbiAgICAgICAgdmFyIGpzb249e1xuICAgICAgICAgICAgdmFsdWU6dGhpcy52YWx1ZSxcbiAgICAgICAgICAgIGxhYmVsOnRoaXMubGFiZWxcbiAgICAgICAgfTtcbiAgICAgICAgcmV0dXJuIGpzb247XG4gICAgfVxufVxuQ2hlY2tib3guSUQ9XCJDaGVja2JveFwiOyIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0xNi5cbiAqL1xuaW1wb3J0IEluc3RhbmNlIGZyb20gJy4uL2luc3RhbmNlL0luc3RhbmNlLmpzJztcbmltcG9ydCBVdGlscyBmcm9tICcuLi9VdGlscy5qcyc7XG5pbXBvcnQgQ2hlY2tib3ggZnJvbSAnLi9DaGVja2JveC5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBDaGVja2JveEluc3RhbmNlIGV4dGVuZHMgSW5zdGFuY2V7XG4gICAgY29uc3RydWN0b3IoKXtcbiAgICAgICAgc3VwZXIoKTtcbiAgICAgICAgdmFyIHNlcT1VdGlscy5zZXEoQ2hlY2tib3hJbnN0YW5jZS5JRCk7XG4gICAgICAgIHZhciBsYWJlbD1cIuWkjemAieahhlwiK3NlcTtcbiAgICAgICAgdGhpcy5lbGVtZW50PXRoaXMubmV3RWxlbWVudChsYWJlbCk7XG4gICAgICAgIHRoaXMuaW5wdXRFbGVtZW50PSQoXCI8ZGl2PlwiKTtcbiAgICAgICAgdGhpcy5lbGVtZW50LmFwcGVuZCh0aGlzLmlucHV0RWxlbWVudCk7XG4gICAgICAgIHRoaXMub3B0aW9ucz1bXTtcbiAgICAgICAgdGhpcy5vcHRpb25zSW5saW5lPWZhbHNlO1xuICAgICAgICB0aGlzLmVsZW1lbnQudW5pcXVlSWQoKTtcbiAgICAgICAgdGhpcy5pZD10aGlzLmVsZW1lbnQucHJvcChcImlkXCIpO1xuICAgICAgICB0aGlzLmFkZE9wdGlvbigpO1xuICAgICAgICB0aGlzLmFkZE9wdGlvbigpO1xuICAgICAgICB0aGlzLmFkZE9wdGlvbigpO1xuICAgIH1cbiAgICBzZXRPcHRpb25zSW5saW5lKG9wdGlvbnNJbmxpbmUpe1xuICAgICAgICBpZihvcHRpb25zSW5saW5lPT09dGhpcy5vcHRpb25zSW5saW5lKXtcbiAgICAgICAgICAgIHJldHVybjtcbiAgICAgICAgfVxuICAgICAgICB0aGlzLm9wdGlvbnNJbmxpbmU9b3B0aW9uc0lubGluZTtcbiAgICAgICAgJC5lYWNoKHRoaXMub3B0aW9ucyxmdW5jdGlvbihpbmRleCxjaGVja2JveCl7XG4gICAgICAgICAgICB2YXIgZWxlbWVudD1jaGVja2JveC5lbGVtZW50O1xuICAgICAgICAgICAgZWxlbWVudC5yZW1vdmVDbGFzcygpO1xuICAgICAgICAgICAgaWYob3B0aW9uc0lubGluZSl7XG4gICAgICAgICAgICAgICAgZWxlbWVudC5hZGRDbGFzcyhDaGVja2JveEluc3RhbmNlLkxBQkVMX1BPU0lUSU9OWzFdKTtcbiAgICAgICAgICAgICAgICBlbGVtZW50LmZpbmQoXCJpbnB1dFwiKS5maXJzdCgpLmNzcyhcIm1hcmdpbi1sZWZ0XCIsXCJcIik7XG4gICAgICAgICAgICB9ZWxzZXtcbiAgICAgICAgICAgICAgICBlbGVtZW50LmFkZENsYXNzKENoZWNrYm94SW5zdGFuY2UuTEFCRUxfUE9TSVRJT05bMF0pO1xuICAgICAgICAgICAgICAgIGVsZW1lbnQuZmluZChcImlucHV0XCIpLmZpcnN0KCkuY3NzKFwibWFyZ2luLWxlZnRcIixcImF1dG9cIik7XG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuICAgIH1cbiAgICByZW1vdmVPcHRpb24ob3B0aW9uKXtcbiAgICAgICAgdmFyIHRhcmdldEluZGV4O1xuICAgICAgICAkLmVhY2godGhpcy5vcHRpb25zLGZ1bmN0aW9uKGluZGV4LGl0ZW0pe1xuICAgICAgICAgICAgaWYoaXRlbT09PW9wdGlvbil7XG4gICAgICAgICAgICAgICAgdGFyZ2V0SW5kZXg9aW5kZXg7XG4gICAgICAgICAgICAgICAgcmV0dXJuIGZhbHNlO1xuICAgICAgICAgICAgfVxuICAgICAgICB9KTtcbiAgICAgICAgdGhpcy5vcHRpb25zLnNwbGljZSh0YXJnZXRJbmRleCwxKTtcbiAgICAgICAgb3B0aW9uLmVsZW1lbnQucmVtb3ZlKCk7XG4gICAgfVxuICAgIGFkZE9wdGlvbihqc29uKXtcbiAgICAgICAgdmFyIGNoZWNrYm94PW5ldyBDaGVja2JveCh0aGlzLm9wdGlvbnNJbmxpbmUpO1xuICAgICAgICBpZihqc29uKXtcbiAgICAgICAgICAgIGNoZWNrYm94LmluaXRGcm9tSnNvbihqc29uKTtcbiAgICAgICAgfVxuICAgICAgICB0aGlzLm9wdGlvbnMucHVzaChjaGVja2JveCk7XG4gICAgICAgIHRoaXMuaW5wdXRFbGVtZW50LmFwcGVuZChjaGVja2JveC5lbGVtZW50KTtcbiAgICAgICAgaWYoIXRoaXMub3B0aW9uc0lubGluZSl7XG4gICAgICAgICAgICBjaGVja2JveC5lbGVtZW50LmZpbmQoXCJpbnB1dFwiKS5maXJzdCgpLmNzcyhcIm1hcmdpbi1sZWZ0XCIsXCJhdXRvXCIpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBjaGVja2JveDtcbiAgICB9XG4gICAgaW5pdEZyb21Kc29uKGpzb24pe1xuICAgICAgICAkLmVhY2godGhpcy5vcHRpb25zLGZ1bmN0aW9uKGluZGV4LGl0ZW0pe1xuICAgICAgICAgICAgaXRlbS5lbGVtZW50LnJlbW92ZSgpO1xuICAgICAgICB9KTtcbiAgICAgICAgdGhpcy5vcHRpb25zLnNwbGljZSgwLHRoaXMub3B0aW9ucy5sZW5ndGgpO1xuICAgICAgICBzdXBlci5mcm9tSnNvbihqc29uKTtcbiAgICAgICAgdmFyIG9wdGlvbnM9anNvbi5vcHRpb25zO1xuICAgICAgICBmb3IodmFyIGk9MDtpPG9wdGlvbnMubGVuZ3RoO2krKyl7XG4gICAgICAgICAgICB0aGlzLmFkZE9wdGlvbihvcHRpb25zW2ldKTtcbiAgICAgICAgfVxuICAgICAgICBpZihqc29uLm9wdGlvbnNJbmxpbmUhPT11bmRlZmluZWQpe1xuICAgICAgICAgICAgdGhpcy5zZXRPcHRpb25zSW5saW5lKGpzb24ub3B0aW9uc0lubGluZSk7XG4gICAgICAgIH1cbiAgICB9XG4gICAgdG9Kc29uKCl7XG4gICAgICAgIGNvbnN0IGpzb249e1xuICAgICAgICAgICAgbGFiZWw6dGhpcy5sYWJlbCxcbiAgICAgICAgICAgIG9wdGlvbnNJbmxpbmU6dGhpcy5vcHRpb25zSW5saW5lLFxuICAgICAgICAgICAgbGFiZWxQb3NpdGlvbjp0aGlzLmxhYmVsUG9zaXRpb24sXG4gICAgICAgICAgICBiaW5kUGFyYW1ldGVyOnRoaXMuYmluZFBhcmFtZXRlcixcbiAgICAgICAgICAgIHR5cGU6Q2hlY2tib3hJbnN0YW5jZS5UWVBFLFxuICAgICAgICAgICAgb3B0aW9uczpbXVxuICAgICAgICB9O1xuICAgICAgICBmb3IobGV0IG9wdGlvbiBvZiB0aGlzLm9wdGlvbnMpe1xuICAgICAgICAgICAganNvbi5vcHRpb25zLnB1c2gob3B0aW9uLnRvSnNvbigpKTtcbiAgICAgICAgfVxuICAgICAgICByZXR1cm4ganNvbjtcbiAgICB9XG4gICAgdG9YbWwoKXtcbiAgICAgICAgbGV0IHhtbD1gPGlucHV0LWNoZWNrYm94IGxhYmVsPVwiJHt0aGlzLmxhYmVsfVwiIHR5cGU9XCIke0NoZWNrYm94SW5zdGFuY2UuVFlQRX1cIiBvcHRpb25zLWlubGluZT1cIiR7dGhpcy5vcHRpb25zSW5saW5lID09PSB1bmRlZmluZWQgPyBmYWxzZSA6IHRoaXMub3B0aW9uc0lubGluZX1cIiBsYWJlbC1wb3NpdGlvbj1cIiR7dGhpcy5sYWJlbFBvc2l0aW9uIHx8ICd0b3AnfVwiIGJpbmQtcGFyYW1ldGVyPVwiJHt0aGlzLmJpbmRQYXJhbWV0ZXIgfHwgJyd9XCI+YDtcbiAgICAgICAgZm9yKGxldCBvcHRpb24gb2YgdGhpcy5vcHRpb25zKXtcbiAgICAgICAgICAgIHhtbCs9YDxvcHRpb24gbGFiZWw9XCIke29wdGlvbi5sYWJlbH1cIiB2YWx1ZT1cIiR7b3B0aW9uLnZhbHVlfVwiPjwvb3B0aW9uPmA7XG4gICAgICAgIH1cbiAgICAgICAgeG1sKz1gPC9pbnB1dC1jaGVja2JveD5gO1xuICAgICAgICByZXR1cm4geG1sO1xuICAgIH1cbn1cbkNoZWNrYm94SW5zdGFuY2UuVFlQRT1cIkNoZWNrYm94XCI7XG5DaGVja2JveEluc3RhbmNlLkxBQkVMX1BPU0lUSU9OPVtcImNoZWNrYm94XCIsXCJjaGVja2JveC1pbmxpbmVcIl07XG5DaGVja2JveEluc3RhbmNlLklEPVwiY2hlY2tfaW5zdGFuY2VcIjsiLCIvKipcbiAqIENyZWF0ZWQgYnkgSmFja3kuR2FvIG9uIDIwMTctMTAtMTIuXG4gKi9cbmltcG9ydCBJbnN0YW5jZSBmcm9tICcuL0luc3RhbmNlLmpzJztcbmV4cG9ydCBkZWZhdWx0IGNsYXNzIENvbnRhaW5lckluc3RhbmNlIGV4dGVuZHMgSW5zdGFuY2V7XG4gICAgY29uc3RydWN0b3IoKXtcbiAgICAgICAgc3VwZXIoKTtcbiAgICAgICAgdGhpcy5jb250YWluZXJzPVtdO1xuICAgICAgICB0aGlzLnZpc2libGU9XCJ0cnVlXCI7XG4gICAgfVxuICAgIGluaXRGcm9tSnNvbihqc29uKXtcbiAgICAgICAgdmFyIGNvbHM9anNvbi5jb2xzO1xuICAgICAgICBmb3IodmFyIGk9MDtpPGNvbHMubGVuZ3RoO2krKyl7XG4gICAgICAgICAgICB2YXIgY29sPWNvbHNbaV07XG4gICAgICAgICAgICB2YXIgYz10aGlzLmNvbnRhaW5lcnNbaV07XG4gICAgICAgICAgICBjLmluaXRGcm9tSnNvbihjb2wpO1xuICAgICAgICB9XG4gICAgICAgIGlmKGpzb24uc2hvd0JvcmRlcil7XG4gICAgICAgICAgICB0aGlzLnNob3dCb3JkZXI9anNvbi5zaG93Qm9yZGVyO1xuICAgICAgICAgICAgdGhpcy5ib3JkZXJXaWR0aD1qc29uLmJvcmRlcldpZHRoO1xuICAgICAgICAgICAgdGhpcy5ib3JkZXJDb2xvcj1qc29uLmJvcmRlckNvbG9yO1xuICAgICAgICAgICAgdGhpcy5zZXRCb3JkZXJXaWR0aCh0aGlzLmJvcmRlcldpZHRoKTtcbiAgICAgICAgfVxuICAgIH1cbn0iLCIvKipcbiAqIENyZWF0ZWQgYnkgSmFja3kuR2FvIG9uIDIwMTctMTAtMjMuXG4gKi9cbmltcG9ydCBJbnN0YW5jZSBmcm9tICcuL0luc3RhbmNlLmpzJztcbmltcG9ydCBVdGlscyBmcm9tICcuLi9VdGlscy5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBEYXRldGltZUluc3RhbmNlIGV4dGVuZHMgSW5zdGFuY2V7XG4gICAgY29uc3RydWN0b3IoKXtcbiAgICAgICAgc3VwZXIoKTtcbiAgICAgICAgdGhpcy5pc0RhdGU9dHJ1ZTtcbiAgICAgICAgdmFyIHNlcT1VdGlscy5zZXEoRGF0ZXRpbWVJbnN0YW5jZS5JRCk7XG4gICAgICAgIHZhciBsYWJlbD1cIuaXpeacn+mAieaLqVwiK3NlcTtcbiAgICAgICAgdGhpcy5lbGVtZW50PXRoaXMubmV3RWxlbWVudChsYWJlbCk7XG4gICAgICAgIHRoaXMuZGF0ZUZvcm1hdD1cInl5eXktbW0tZGRcIjtcbiAgICAgICAgdGhpcy5pbnB1dEVsZW1lbnQ9JChcIjxkaXY+XCIpO1xuICAgICAgICB0aGlzLmVsZW1lbnQuYXBwZW5kKHRoaXMuaW5wdXRFbGVtZW50KTtcbiAgICAgICAgdGhpcy5kYXRlUGlja2VyaW5wdXRHcm91cD0kKFwiPGRpdiBjbGFzcz0naW5wdXQtZ3JvdXAgZGF0ZSc+XCIpO1xuICAgICAgICB0aGlzLmlucHV0RWxlbWVudC5hcHBlbmQodGhpcy5kYXRlUGlja2VyaW5wdXRHcm91cCk7XG4gICAgICAgIHZhciB0ZXh0PSQoXCI8aW5wdXQgdHlwZT0ndGV4dCcgY2xhc3M9J2Zvcm0tY29udHJvbCc+XCIpO1xuICAgICAgICB0aGlzLmRhdGVQaWNrZXJpbnB1dEdyb3VwLmFwcGVuZCh0ZXh0KTtcbiAgICAgICAgdmFyIHBpY2tlckljb249JChcIjxzcGFuIGNsYXNzPSdpbnB1dC1ncm91cC1hZGRvbic+PHNwYW4gY2xhc3M9J2dseXBoaWNvbiBnbHlwaGljb24tY2FsZW5kYXInPjwvc3Bhbj48L3NwYW4+XCIpO1xuICAgICAgICB0aGlzLmRhdGVQaWNrZXJpbnB1dEdyb3VwLmFwcGVuZChwaWNrZXJJY29uKTtcbiAgICAgICAgdGhpcy5kYXRlUGlja2VyaW5wdXRHcm91cC5kYXRldGltZXBpY2tlcih7XG4gICAgICAgICAgICBmb3JtYXQ6dGhpcy5kYXRlRm9ybWF0LFxuICAgICAgICAgICAgYXV0b2Nsb3NlOjEsXG4gICAgICAgICAgICBzdGFydFZpZXc6MixcbiAgICAgICAgICAgIG1pblZpZXc6MlxuICAgICAgICB9KTtcbiAgICAgICAgdGhpcy5lbGVtZW50LnVuaXF1ZUlkKCk7XG4gICAgICAgIHRoaXMuaWQ9dGhpcy5lbGVtZW50LnByb3AoXCJpZFwiKTtcbiAgICB9XG4gICAgc2V0RGF0ZUZvcm1hdChmb3JtYXQpe1xuICAgICAgICBpZih0aGlzLmRhdGVGb3JtYXQ9PT1mb3JtYXQgfHwgZm9ybWF0PT09JycgfHwgZm9ybWF0PT09dW5kZWZpbmVkKXtcbiAgICAgICAgICAgIHJldHVybjtcbiAgICAgICAgfVxuICAgICAgICB0aGlzLmRhdGVGb3JtYXQ9Zm9ybWF0O1xuICAgICAgICB0aGlzLmRhdGVQaWNrZXJpbnB1dEdyb3VwLmRhdGV0aW1lcGlja2VyKCdyZW1vdmUnKTtcbiAgICAgICAgY29uc3Qgb3B0aW9ucz17XG4gICAgICAgICAgICBmb3JtYXQ6dGhpcy5kYXRlRm9ybWF0LFxuICAgICAgICAgICAgYXV0b2Nsb3NlOjFcbiAgICAgICAgfTtcbiAgICAgICAgaWYodGhpcy5kYXRlRm9ybWF0PT09J3l5eXktbW0tZGQnKXtcbiAgICAgICAgICAgIG9wdGlvbnMuc3RhcnRWaWV3PTI7XG4gICAgICAgICAgICBvcHRpb25zLm1pblZpZXc9MjtcbiAgICAgICAgfVxuICAgICAgICB0aGlzLmRhdGVQaWNrZXJpbnB1dEdyb3VwLmRhdGV0aW1lcGlja2VyKG9wdGlvbnMpO1xuICAgIH1cbiAgICBpbml0RnJvbUpzb24oanNvbil7XG4gICAgICAgIHN1cGVyLmZyb21Kc29uKGpzb24pO1xuICAgICAgICB0aGlzLnNldERhdGVGb3JtYXQoanNvbi5mb3JtYXQpO1xuICAgICAgICBpZihqc29uLnNlYXJjaE9wZXJhdG9yKXtcbiAgICAgICAgICAgIHRoaXMuc2VhcmNoT3BlcmF0b3I9anNvbi5zZWFyY2hPcGVyYXRvcjtcbiAgICAgICAgfVxuICAgIH1cbiAgICB0b0pzb24oKXtcbiAgICAgICAgcmV0dXJuIHtcbiAgICAgICAgICAgIGxhYmVsOnRoaXMubGFiZWwsXG4gICAgICAgICAgICBsYWJlbFBvc2l0aW9uOnRoaXMubGFiZWxQb3NpdGlvbixcbiAgICAgICAgICAgIGJpbmRQYXJhbWV0ZXI6dGhpcy5iaW5kUGFyYW1ldGVyLFxuICAgICAgICAgICAgZm9ybWF0OnRoaXMuZGF0ZUZvcm1hdCxcbiAgICAgICAgICAgIHR5cGU6RGF0ZXRpbWVJbnN0YW5jZS5UWVBFXG4gICAgICAgIH07XG4gICAgfVxuICAgIHRvWG1sKCl7XG4gICAgICAgIGxldCB4bWw9YDxpbnB1dC1kYXRldGltZSBsYWJlbD1cIiR7dGhpcy5sYWJlbH1cIiB0eXBlPVwiJHtEYXRldGltZUluc3RhbmNlLlRZUEV9XCIgbGFiZWwtcG9zaXRpb249XCIke3RoaXMubGFiZWxQb3NpdGlvbiB8fCAndG9wJ31cIiBiaW5kLXBhcmFtZXRlcj1cIiR7dGhpcy5iaW5kUGFyYW1ldGVyIHx8ICcnfVwiIGZvcm1hdD1cIiR7dGhpcy5kYXRlRm9ybWF0fVwiPjwvaW5wdXQtZGF0ZXRpbWU+YDtcbiAgICAgICAgcmV0dXJuIHhtbDtcbiAgICB9XG59XG5EYXRldGltZUluc3RhbmNlLlRZUEU9XCJEYXRldGltZVwiO1xuRGF0ZXRpbWVJbnN0YW5jZS5JRD1cImRhdGV0aW1lX2luc3RhbmNlXCI7IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTE1LlxuICovXG5pbXBvcnQgQ29sQ29udGFpbmVyIGZyb20gJy4uL2NvbnRhaW5lci9Db2xDb250YWluZXIuanMnO1xuaW1wb3J0IENvbnRhaW5lckluc3RhbmNlIGZyb20gJy4vQ29udGFpbmVySW5zdGFuY2UuanMnO1xuZXhwb3J0IGRlZmF1bHQgY2xhc3MgR3JpZDJYMkluc3RhbmNlIGV4dGVuZHMgQ29udGFpbmVySW5zdGFuY2V7XG4gICAgY29uc3RydWN0b3IoKXtcbiAgICAgICAgc3VwZXIoKTtcbiAgICAgICAgdGhpcy5lbGVtZW50PSQoXCI8ZGl2IGNsYXNzPVxcXCJyb3dcXFwiIHN0eWxlPVxcXCJtYXJnaW46IDBweDttaW4td2lkdGg6MTAwcHg7XFxcIj5cIik7XG4gICAgICAgIHZhciBjb2wxPW5ldyBDb2xDb250YWluZXIoNik7XG4gICAgICAgIHZhciBjb2wyPW5ldyBDb2xDb250YWluZXIoNik7XG4gICAgICAgIHRoaXMuY29udGFpbmVycy5wdXNoKGNvbDEsY29sMik7XG4gICAgICAgIHRoaXMuZWxlbWVudC5hcHBlbmQoY29sMS5nZXRDb250YWluZXIoKSk7XG4gICAgICAgIHRoaXMuZWxlbWVudC5hcHBlbmQoY29sMi5nZXRDb250YWluZXIoKSk7XG4gICAgICAgIHRoaXMuZWxlbWVudC51bmlxdWVJZCgpO1xuICAgICAgICB0aGlzLmlkPXRoaXMuZWxlbWVudC5wcm9wKFwiaWRcIik7XG4gICAgICAgIHRoaXMuc2hvd0JvcmRlcj1mYWxzZTtcbiAgICAgICAgdGhpcy5ib3JkZXJXaWR0aD0xO1xuICAgICAgICB0aGlzLmJvcmRlckNvbG9yPVwiI2VlZVwiO1xuICAgIH1cbiAgICB0b0pzb24oKXtcbiAgICAgICAgY29uc3QganNvbj17XG4gICAgICAgICAgICBzaG93Qm9yZGVyOnRoaXMuc2hvd0JvcmRlcixcbiAgICAgICAgICAgIGJvcmRlcldpZHRoOnRoaXMuYm9yZGVyV2lkdGgsXG4gICAgICAgICAgICBib3JkZXJDb2xvcjp0aGlzLmJvcmRlckNvbG9yLFxuICAgICAgICAgICAgdHlwZTpHcmlkMlgySW5zdGFuY2UuVFlQRSxcbiAgICAgICAgICAgIGNvbHM6W11cbiAgICAgICAgfTtcbiAgICAgICAgZm9yKGxldCBjb250YWluZXIgb2YgdGhpcy5jb250YWluZXJzKXtcbiAgICAgICAgICAgIGpzb24uY29scy5wdXNoKGNvbnRhaW5lci50b0pzb24oKSk7XG4gICAgICAgIH1cbiAgICAgICAgcmV0dXJuIGpzb247XG4gICAgfVxuICAgIHRvWG1sKCl7XG4gICAgICAgIGxldCB4bWw9YDxncmlkIHNob3ctYm9yZGVyPVwiJHt0aGlzLnNob3dCb3JkZXJ9XCIgdHlwZT1cIiR7R3JpZDJYMkluc3RhbmNlLlRZUEV9XCIgYm9yZGVyLXdpZHRoPVwiJHt0aGlzLmJvcmRlcldpZHRofVwiIGJvcmRlci1jb2xvcj1cIiR7dGhpcy5ib3JkZXJDb2xvcn1cIj5gO1xuICAgICAgICBmb3IobGV0IGNvbnRhaW5lciBvZiB0aGlzLmNvbnRhaW5lcnMpe1xuICAgICAgICAgICAgeG1sKz1jb250YWluZXIudG9YbWwoKTtcbiAgICAgICAgfVxuICAgICAgICB4bWwrPWA8L2dyaWQ+YDtcbiAgICAgICAgcmV0dXJuIHhtbDtcbiAgICB9XG4gICAgc2V0Qm9yZGVyV2lkdGgod2lkdGgpe1xuICAgICAgICB2YXIgc2VsZj10aGlzO1xuICAgICAgICAkLmVhY2godGhpcy5jb250YWluZXJzLGZ1bmN0aW9uKGluZGV4LGNvbnRhaW5lcil7XG4gICAgICAgICAgICBpZih3aWR0aCl7XG4gICAgICAgICAgICAgICAgY29udGFpbmVyLmNvbnRhaW5lci5jc3MoXCJib3JkZXJcIixcInNvbGlkIFwiK3dpZHRoK1wicHggXCIrc2VsZi5ib3JkZXJDb2xvcitcIlwiKTtcbiAgICAgICAgICAgIH1lbHNle1xuICAgICAgICAgICAgICAgIGNvbnRhaW5lci5jb250YWluZXIuY3NzKFwiYm9yZGVyXCIsXCJcIik7XG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuICAgICAgICBpZih3aWR0aCl7XG4gICAgICAgICAgICB0aGlzLmJvcmRlcldpZHRoPXdpZHRoO1xuICAgICAgICB9XG4gICAgfVxuICAgIHNldEJvcmRlckNvbG9yKGNvbG9yKXtcbiAgICAgICAgdmFyIHNlbGY9dGhpcztcbiAgICAgICAgJC5lYWNoKHRoaXMuY29udGFpbmVycyxmdW5jdGlvbihpbmRleCxjb250YWluZXIpe1xuICAgICAgICAgICAgY29udGFpbmVyLmNvbnRhaW5lci5jc3MoXCJib3JkZXJcIixcInNvbGlkIFwiK3NlbGYuYm9yZGVyV2lkdGgrXCJweCBcIitjb2xvcitcIlwiKTtcbiAgICAgICAgfSk7XG4gICAgICAgIHRoaXMuYm9yZGVyQ29sb3I9Y29sb3I7XG4gICAgfVxufVxuR3JpZDJYMkluc3RhbmNlLlRZUEU9XCJHcmlkMlgyXCI7IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTE2LlxuICovXG5pbXBvcnQgQ29udGFpbmVySW5zdGFuY2UgZnJvbSAnLi9Db250YWluZXJJbnN0YW5jZS5qcyc7XG5pbXBvcnQgQ29sQ29udGFpbmVyIGZyb20gJy4uL2NvbnRhaW5lci9Db2xDb250YWluZXIuanMnO1xuZXhwb3J0IGRlZmF1bHQgY2xhc3MgR3JpZDN4M3gzSW5zdGFuY2UgZXh0ZW5kcyBDb250YWluZXJJbnN0YW5jZXtcbiAgICBjb25zdHJ1Y3Rvcigpe1xuICAgICAgICBzdXBlcigpO1xuICAgICAgICB0aGlzLmVsZW1lbnQ9JChcIjxkaXYgY2xhc3M9XFxcInJvd1xcXCIgc3R5bGU9XFxcIm1hcmdpbjogMHB4O21pbi13aWR0aDoxMDBweDtcXFwiPlwiKTtcbiAgICAgICAgdmFyIGNvbDE9bmV3IENvbENvbnRhaW5lcig0KTtcbiAgICAgICAgdmFyIGNvbDI9bmV3IENvbENvbnRhaW5lcig0KTtcbiAgICAgICAgdmFyIGNvbDM9bmV3IENvbENvbnRhaW5lcig0KTtcbiAgICAgICAgdGhpcy5jb250YWluZXJzLnB1c2goY29sMSxjb2wyLGNvbDMpO1xuICAgICAgICB0aGlzLmVsZW1lbnQuYXBwZW5kKGNvbDEuZ2V0Q29udGFpbmVyKCkpO1xuICAgICAgICB0aGlzLmVsZW1lbnQuYXBwZW5kKGNvbDIuZ2V0Q29udGFpbmVyKCkpO1xuICAgICAgICB0aGlzLmVsZW1lbnQuYXBwZW5kKGNvbDMuZ2V0Q29udGFpbmVyKCkpO1xuICAgICAgICB0aGlzLmVsZW1lbnQudW5pcXVlSWQoKTtcbiAgICAgICAgdGhpcy5pZD10aGlzLmVsZW1lbnQucHJvcChcImlkXCIpO1xuICAgICAgICB0aGlzLnNob3dCb3JkZXI9ZmFsc2U7XG4gICAgICAgIHRoaXMuYm9yZGVyV2lkdGg9MTtcbiAgICAgICAgdGhpcy5ib3JkZXJDb2xvcj1cIiNjY2NjY2NcIjtcbiAgICB9XG4gICAgdG9Kc29uKCl7XG4gICAgICAgIGNvbnN0IGpzb249e1xuICAgICAgICAgICAgc2hvd0JvcmRlcjp0aGlzLnNob3dCb3JkZXIsXG4gICAgICAgICAgICBib3JkZXJXaWR0aDp0aGlzLmJvcmRlcldpZHRoLFxuICAgICAgICAgICAgYm9yZGVyQ29sb3I6dGhpcy5ib3JkZXJDb2xvcixcbiAgICAgICAgICAgIHR5cGU6R3JpZDN4M3gzSW5zdGFuY2UuVFlQRSxcbiAgICAgICAgICAgIGNvbHM6W11cbiAgICAgICAgfTtcbiAgICAgICAgZm9yKGxldCBjb250YWluZXIgb2YgdGhpcy5jb250YWluZXJzKXtcbiAgICAgICAgICAgIGpzb24uY29scy5wdXNoKGNvbnRhaW5lci50b0pzb24oKSk7XG4gICAgICAgIH1cbiAgICAgICAgcmV0dXJuIGpzb247XG4gICAgfVxuICAgIHRvWG1sKCl7XG4gICAgICAgIGxldCB4bWw9YDxncmlkIHNob3ctYm9yZGVyPVwiJHt0aGlzLnNob3dCb3JkZXJ9XCIgdHlwZT1cIiR7R3JpZDN4M3gzSW5zdGFuY2UuVFlQRX1cIiBib3JkZXItd2lkdGg9XCIke3RoaXMuYm9yZGVyV2lkdGh9XCIgYm9yZGVyLWNvbG9yPVwiJHt0aGlzLmJvcmRlckNvbG9yfVwiPmA7XG4gICAgICAgIGZvcihsZXQgY29udGFpbmVyIG9mIHRoaXMuY29udGFpbmVycyl7XG4gICAgICAgICAgICB4bWwrPWNvbnRhaW5lci50b1htbCgpO1xuICAgICAgICB9XG4gICAgICAgIHhtbCs9YDwvZ3JpZD5gO1xuICAgICAgICByZXR1cm4geG1sO1xuICAgIH1cbiAgICBzZXRCb3JkZXJXaWR0aCgpe1xuICAgICAgICB2YXIgc2VsZj10aGlzO1xuICAgICAgICAkLmVhY2godGhpcy5jb250YWluZXJzLGZ1bmN0aW9uKGluZGV4LGNvbnRhaW5lcil7XG4gICAgICAgICAgICBpZih3aWR0aCl7XG4gICAgICAgICAgICAgICAgY29udGFpbmVyLmNvbnRhaW5lci5jc3MoXCJib3JkZXJcIixcInNvbGlkIFwiK3dpZHRoK1wicHggXCIrc2VsZi5ib3JkZXJDb2xvcitcIlwiKTtcbiAgICAgICAgICAgIH1lbHNle1xuICAgICAgICAgICAgICAgIGNvbnRhaW5lci5jb250YWluZXIuY3NzKFwiYm9yZGVyXCIsXCJcIik7XG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuICAgICAgICBpZih3aWR0aCl7XG4gICAgICAgICAgICB0aGlzLmJvcmRlcldpZHRoPXdpZHRoO1xuICAgICAgICB9XG4gICAgfVxuICAgIHNldEJvcmRlckNvbG9yKGNvbG9yKXtcbiAgICAgICAgdmFyIHNlbGY9dGhpcztcbiAgICAgICAgJC5lYWNoKHRoaXMuY29udGFpbmVycyxmdW5jdGlvbihpbmRleCxjb250YWluZXIpe1xuICAgICAgICAgICAgY29udGFpbmVyLmNvbnRhaW5lci5jc3MoXCJib3JkZXJcIixcInNvbGlkIFwiK3NlbGYuYm9yZGVyV2lkdGgrXCJweCBcIitjb2xvcitcIlwiKTtcbiAgICAgICAgfSk7XG4gICAgICAgIHRoaXMuYm9yZGVyQ29sb3I9Y29sb3I7XG4gICAgfVxufVxuR3JpZDN4M3gzSW5zdGFuY2UuVFlQRT1cIkdyaWQzeDN4M1wiOyIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0xNi5cbiAqL1xuaW1wb3J0IENvbnRhaW5lckluc3RhbmNlIGZyb20gJy4vQ29udGFpbmVySW5zdGFuY2UuanMnO1xuaW1wb3J0IENvbENvbnRhaW5lciBmcm9tICcuLi9jb250YWluZXIvQ29sQ29udGFpbmVyLmpzJztcbmV4cG9ydCBkZWZhdWx0IGNsYXNzIEdyaWQ0eDR4NHg0SW5zdGFuY2UgZXh0ZW5kcyBDb250YWluZXJJbnN0YW5jZXtcbiAgICBjb25zdHJ1Y3Rvcigpe1xuICAgICAgICBzdXBlcigpO1xuICAgICAgICB0aGlzLmVsZW1lbnQ9JChcIjxkaXYgY2xhc3M9XFxcInJvd1xcXCIgc3R5bGU9XFxcIm1hcmdpbjogMHB4O21pbi13aWR0aDoxMDBweDtcXFwiPlwiKTtcbiAgICAgICAgdmFyIGNvbDE9bmV3IENvbENvbnRhaW5lcigzKTtcbiAgICAgICAgdmFyIGNvbDI9bmV3IENvbENvbnRhaW5lcigzKTtcbiAgICAgICAgdmFyIGNvbDM9bmV3IENvbENvbnRhaW5lcigzKTtcbiAgICAgICAgdmFyIGNvbDQ9bmV3IENvbENvbnRhaW5lcigzKTtcbiAgICAgICAgdGhpcy5jb250YWluZXJzLnB1c2goY29sMSxjb2wyLGNvbDMsY29sNCk7XG4gICAgICAgIHRoaXMuZWxlbWVudC5hcHBlbmQoY29sMS5nZXRDb250YWluZXIoKSk7XG4gICAgICAgIHRoaXMuZWxlbWVudC5hcHBlbmQoY29sMi5nZXRDb250YWluZXIoKSk7XG4gICAgICAgIHRoaXMuZWxlbWVudC5hcHBlbmQoY29sMy5nZXRDb250YWluZXIoKSk7XG4gICAgICAgIHRoaXMuZWxlbWVudC5hcHBlbmQoY29sNC5nZXRDb250YWluZXIoKSk7XG4gICAgICAgIHRoaXMuZWxlbWVudC51bmlxdWVJZCgpO1xuICAgICAgICB0aGlzLmlkPXRoaXMuZWxlbWVudC5wcm9wKFwiaWRcIik7XG4gICAgICAgIHRoaXMuc2hvd0JvcmRlcj1mYWxzZTtcbiAgICAgICAgdGhpcy5ib3JkZXJXaWR0aD0xO1xuICAgICAgICB0aGlzLmJvcmRlckNvbG9yPVwiI2NjY2NjY1wiO1xuICAgIH1cbiAgICB0b0pzb24oKXtcbiAgICAgICAgY29uc3QganNvbj17XG4gICAgICAgICAgICBzaG93Qm9yZGVyOnRoaXMuc2hvd0JvcmRlcixcbiAgICAgICAgICAgIGJvcmRlcldpZHRoOnRoaXMuYm9yZGVyV2lkdGgsXG4gICAgICAgICAgICBib3JkZXJDb2xvcjp0aGlzLmJvcmRlckNvbG9yLFxuICAgICAgICAgICAgdHlwZTpHcmlkNHg0eDR4NEluc3RhbmNlLlRZUEUsXG4gICAgICAgICAgICBjb2xzOltdXG4gICAgICAgIH07XG4gICAgICAgIGZvcihsZXQgY29udGFpbmVyIG9mIHRoaXMuY29udGFpbmVycyl7XG4gICAgICAgICAgICBqc29uLmNvbHMucHVzaChjb250YWluZXIudG9Kc29uKCkpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBqc29uO1xuICAgIH1cbiAgICB0b1htbCgpe1xuICAgICAgICBsZXQgeG1sPWA8Z3JpZCBzaG93LWJvcmRlcj1cIiR7dGhpcy5zaG93Qm9yZGVyfVwiIHR5cGU9XCIke0dyaWQ0eDR4NHg0SW5zdGFuY2UuVFlQRX1cIiBib3JkZXItd2lkdGg9XCIke3RoaXMuYm9yZGVyV2lkdGh9XCIgYm9yZGVyLWNvbG9yPVwiJHt0aGlzLmJvcmRlckNvbG9yfVwiPmA7XG4gICAgICAgIGZvcihsZXQgY29udGFpbmVyIG9mIHRoaXMuY29udGFpbmVycyl7XG4gICAgICAgICAgICB4bWwrPWNvbnRhaW5lci50b1htbCgpO1xuICAgICAgICB9XG4gICAgICAgIHhtbCs9YDwvZ3JpZD5gO1xuICAgICAgICByZXR1cm4geG1sO1xuICAgIH1cbiAgICBzZXRCb3JkZXJXaWR0aCh3aWR0aCl7XG4gICAgICAgIHZhciBzZWxmPXRoaXM7XG4gICAgICAgICQuZWFjaCh0aGlzLmNvbnRhaW5lcnMsZnVuY3Rpb24oaW5kZXgsY29udGFpbmVyKXtcbiAgICAgICAgICAgIGlmKHdpZHRoKXtcbiAgICAgICAgICAgICAgICBjb250YWluZXIuY29udGFpbmVyLmNzcyhcImJvcmRlclwiLFwic29saWQgXCIrd2lkdGgrXCJweCBcIitzZWxmLmJvcmRlckNvbG9yK1wiXCIpO1xuICAgICAgICAgICAgfWVsc2V7XG4gICAgICAgICAgICAgICAgY29udGFpbmVyLmNvbnRhaW5lci5jc3MoXCJib3JkZXJcIixcIlwiKTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfSk7XG4gICAgICAgIGlmKHdpZHRoKXtcbiAgICAgICAgICAgIHRoaXMuYm9yZGVyV2lkdGg9d2lkdGg7XG4gICAgICAgIH1cbiAgICB9XG4gICAgc2V0Qm9yZGVyQ29sb3IoY29sb3Ipe1xuICAgICAgICB2YXIgc2VsZj10aGlzO1xuICAgICAgICAkLmVhY2godGhpcy5jb250YWluZXJzLGZ1bmN0aW9uKGluZGV4LGNvbnRhaW5lcil7XG4gICAgICAgICAgICBjb250YWluZXIuY29udGFpbmVyLmNzcyhcImJvcmRlclwiLFwic29saWQgXCIrc2VsZi5ib3JkZXJXaWR0aCtcInB4IFwiK2NvbG9yK1wiXCIpO1xuICAgICAgICB9KTtcbiAgICAgICAgdGhpcy5ib3JkZXJDb2xvcj1jb2xvcjtcbiAgICB9XG59XG5HcmlkNHg0eDR4NEluc3RhbmNlLlRZUEU9XCJHcmlkNHg0eDR4NFwiOyIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0xNi5cbiAqL1xuaW1wb3J0IENvbnRhaW5lckluc3RhbmNlIGZyb20gJy4vQ29udGFpbmVySW5zdGFuY2UuanMnO1xuaW1wb3J0IENvbENvbnRhaW5lciBmcm9tICcuLi9jb250YWluZXIvQ29sQ29udGFpbmVyLmpzJztcbmV4cG9ydCBkZWZhdWx0IGNsYXNzIEdyaWRDdXN0b21JbnN0YW5jZSBleHRlbmRzIENvbnRhaW5lckluc3RhbmNle1xuICAgIGNvbnN0cnVjdG9yKGNvbHNKc29uKXtcbiAgICAgICAgc3VwZXIoKTtcbiAgICAgICAgdGhpcy5lbGVtZW50PSQoXCI8ZGl2IGNsYXNzPVxcXCJyb3dcXFwiIHN0eWxlPVxcXCJtYXJnaW46IDBweDttaW4td2lkdGg6MTAwcHg7XFxcIj5cIik7XG4gICAgICAgIHZhciB2YWx1ZTtcbiAgICAgICAgaWYoIWNvbHNKc29uKXtcbiAgICAgICAgICAgIHdoaWxlKCF2YWx1ZSl7XG4gICAgICAgICAgICAgICAgdmFsdWU9cHJvbXB0KFwi6K+36L6T5YWl5YiX5L+h5oGvLOWIl+S5i+mXtOeUqOKAnCzigJ3liIbpmpQs5YiX5pWw5LmL5ZKM5Li6MTLvvIzlpoLigJwyLDgsMuKAne+8jOihqOekuuacieS4ieWIl++8jOavlOmHjeS4ujIsOCwyXCIsXCIyLDgsMlwiKTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfWVsc2V7XG4gICAgICAgICAgICB2YWx1ZT1cIlwiO1xuICAgICAgICAgICAgZm9yKHZhciBpPTA7aTxjb2xzSnNvbi5sZW5ndGg7aSsrKXtcbiAgICAgICAgICAgICAgICB2YXIgc2l6ZT1jb2xzSnNvbltpXS5zaXplO1xuICAgICAgICAgICAgICAgIGlmKHZhbHVlLmxlbmd0aD4wKXtcbiAgICAgICAgICAgICAgICAgICAgdmFsdWUrPVwiLFwiO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICB2YWx1ZSs9c2l6ZTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfVxuICAgICAgICB2YXIgY29scz12YWx1ZS5zcGxpdChcIixcIik7XG4gICAgICAgIGZvcih2YXIgaT0wO2k8Y29scy5sZW5ndGg7aSsrKXtcbiAgICAgICAgICAgIHZhciBjb2xOdW09cGFyc2VJbnQoY29sc1tpXSk7XG4gICAgICAgICAgICBpZighY29sTnVtKXtcbiAgICAgICAgICAgICAgICBjb2xOdW09MTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIHZhciBjb2w9bmV3IENvbENvbnRhaW5lcihjb2xOdW0pO1xuICAgICAgICAgICAgdGhpcy5jb250YWluZXJzLnB1c2goY29sKTtcbiAgICAgICAgICAgIHRoaXMuZWxlbWVudC5hcHBlbmQoY29sLmdldENvbnRhaW5lcigpKTtcbiAgICAgICAgfVxuICAgICAgICB0aGlzLmVsZW1lbnQudW5pcXVlSWQoKTtcbiAgICAgICAgdGhpcy5pZD10aGlzLmVsZW1lbnQucHJvcChcImlkXCIpO1xuICAgICAgICB0aGlzLnNob3dCb3JkZXI9ZmFsc2U7XG4gICAgICAgIHRoaXMuYm9yZGVyV2lkdGg9MTtcbiAgICAgICAgdGhpcy5ib3JkZXJDb2xvcj1cIiNjY2NjY2NcIjtcbiAgICB9XG4gICAgZ2V0RWxlbWVudCgpe1xuICAgICAgICByZXR1cm4gdGhpcy5lbGVtZW50O1xuICAgIH1cbiAgICB0b0pzb24oKXtcbiAgICAgICAgY29uc3QganNvbj17XG4gICAgICAgICAgICBzaG93Qm9yZGVyOnRoaXMuc2hvd0JvcmRlcixcbiAgICAgICAgICAgIGJvcmRlcldpZHRoOnRoaXMuYm9yZGVyV2lkdGgsXG4gICAgICAgICAgICBib3JkZXJDb2xvcjp0aGlzLmJvcmRlckNvbG9yLFxuICAgICAgICAgICAgdHlwZTpHcmlkQ3VzdG9tSW5zdGFuY2UuVFlQRSxcbiAgICAgICAgICAgIGNvbHM6W11cbiAgICAgICAgfTtcbiAgICAgICAgZm9yKGxldCBjb250YWluZXIgb2YgdGhpcy5jb250YWluZXJzKXtcbiAgICAgICAgICAgIGpzb24uY29scy5wdXNoKGNvbnRhaW5lci50b0pzb24oKSk7XG4gICAgICAgIH1cbiAgICAgICAgcmV0dXJuIGpzb247XG4gICAgfVxuICAgIHRvWG1sKCl7XG4gICAgICAgIGxldCB4bWw9YDxncmlkIHNob3ctYm9yZGVyPVwiJHt0aGlzLnNob3dCb3JkZXJ9XCIgdHlwZT1cIiR7R3JpZEN1c3RvbUluc3RhbmNlLlRZUEV9XCIgYm9yZGVyLXdpZHRoPVwiJHt0aGlzLmJvcmRlcldpZHRofVwiIGJvcmRlci1jb2xvcj1cIiR7dGhpcy5ib3JkZXJDb2xvcn1cIj5gO1xuICAgICAgICBmb3IobGV0IGNvbnRhaW5lciBvZiB0aGlzLmNvbnRhaW5lcnMpe1xuICAgICAgICAgICAgeG1sKz1jb250YWluZXIudG9YbWwoKTtcbiAgICAgICAgfVxuICAgICAgICB4bWwrPWA8L2dyaWQ+YDtcbiAgICAgICAgcmV0dXJuIHhtbDtcbiAgICB9XG4gICAgc2V0Qm9yZGVyV2lkdGgod2lkdGgpe1xuICAgICAgICB2YXIgc2VsZj10aGlzO1xuICAgICAgICAkLmVhY2godGhpcy5jb250YWluZXJzLGZ1bmN0aW9uKGluZGV4LGNvbnRhaW5lcil7XG4gICAgICAgICAgICBpZih3aWR0aCl7XG4gICAgICAgICAgICAgICAgY29udGFpbmVyLmNvbnRhaW5lci5jc3MoXCJib3JkZXJcIixcInNvbGlkIFwiK3dpZHRoK1wicHggXCIrc2VsZi5ib3JkZXJDb2xvcitcIlwiKTtcbiAgICAgICAgICAgIH1lbHNle1xuICAgICAgICAgICAgICAgIGNvbnRhaW5lci5jb250YWluZXIuY3NzKFwiYm9yZGVyXCIsXCJcIik7XG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuICAgICAgICBpZih3aWR0aCl7XG4gICAgICAgICAgICB0aGlzLmJvcmRlcldpZHRoPXdpZHRoO1xuICAgICAgICB9XG4gICAgfVxuICAgIHNldEJvcmRlckNvbG9yKGNvbG9yKXtcbiAgICAgICAgdmFyIHNlbGY9dGhpcztcbiAgICAgICAgJC5lYWNoKHRoaXMuY29udGFpbmVycyxmdW5jdGlvbihpbmRleCxjb250YWluZXIpe1xuICAgICAgICAgICAgY29udGFpbmVyLmNvbnRhaW5lci5jc3MoXCJib3JkZXJcIixcInNvbGlkIFwiK3NlbGYuYm9yZGVyV2lkdGgrXCJweCBcIitjb2xvcitcIlwiKTtcbiAgICAgICAgfSk7XG4gICAgICAgIHRoaXMuYm9yZGVyQ29sb3I9Y29sb3I7XG4gICAgfVxufVxuR3JpZEN1c3RvbUluc3RhbmNlLlRZUEU9XCJHcmlkQ3VzdG9tXCI7IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTE2LlxuICovXG5pbXBvcnQgQ29udGFpbmVySW5zdGFuY2UgZnJvbSAnLi9Db250YWluZXJJbnN0YW5jZS5qcyc7XG5pbXBvcnQgQ29sQ29udGFpbmVyIGZyb20gJy4uL2NvbnRhaW5lci9Db2xDb250YWluZXIuanMnO1xuZXhwb3J0IGRlZmF1bHQgY2xhc3MgR3JpZFNpbmdsZUluc3RhbmNlIGV4dGVuZHMgQ29udGFpbmVySW5zdGFuY2V7XG4gICAgY29uc3RydWN0b3IoKXtcbiAgICAgICAgc3VwZXIoKTtcbiAgICAgICAgdGhpcy5lbGVtZW50PSQoXCI8ZGl2IGNsYXNzPVxcXCJyb3dcXFwiIHN0eWxlPVxcXCJtYXJnaW46IDBweDttaW4td2lkdGg6MTAwcHg7XFxcIj5cIik7XG4gICAgICAgIHRoaXMuY29sMT1uZXcgQ29sQ29udGFpbmVyKDEyKTtcbiAgICAgICAgdGhpcy5jb250YWluZXJzLnB1c2godGhpcy5jb2wxKTtcbiAgICAgICAgdGhpcy5lbGVtZW50LmFwcGVuZCh0aGlzLmNvbDEuZ2V0Q29udGFpbmVyKCkpO1xuICAgICAgICB0aGlzLmVsZW1lbnQudW5pcXVlSWQoKTtcbiAgICAgICAgdGhpcy5pZD10aGlzLmVsZW1lbnQucHJvcChcImlkXCIpO1xuICAgICAgICB0aGlzLnNob3dCb3JkZXI9ZmFsc2U7XG4gICAgICAgIHRoaXMuYm9yZGVyV2lkdGg9MTtcbiAgICAgICAgdGhpcy5ib3JkZXJDb2xvcj1cIiNjY2NjY2NcIjtcbiAgICB9XG4gICAgdG9Kc29uKCl7XG4gICAgICAgIGNvbnN0IGpzb249e1xuICAgICAgICAgICAgc2hvd0JvcmRlcjp0aGlzLnNob3dCb3JkZXIsXG4gICAgICAgICAgICBib3JkZXJXaWR0aDp0aGlzLmJvcmRlcldpZHRoLFxuICAgICAgICAgICAgYm9yZGVyQ29sb3I6dGhpcy5ib3JkZXJDb2xvcixcbiAgICAgICAgICAgIHR5cGU6R3JpZFNpbmdsZUluc3RhbmNlLlRZUEUsXG4gICAgICAgICAgICBjb2xzOltdXG4gICAgICAgIH07XG4gICAgICAgIGZvcihsZXQgY29udGFpbmVyIG9mIHRoaXMuY29udGFpbmVycyl7XG4gICAgICAgICAgICBqc29uLmNvbHMucHVzaChjb250YWluZXIudG9Kc29uKCkpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBqc29uO1xuICAgIH1cbiAgICB0b1htbCgpe1xuICAgICAgICBsZXQgeG1sPWA8Z3JpZCBzaG93LWJvcmRlcj1cIiR7dGhpcy5zaG93Qm9yZGVyfVwiIHR5cGU9XCIke0dyaWRTaW5nbGVJbnN0YW5jZS5UWVBFfVwiIGJvcmRlci13aWR0aD1cIiR7dGhpcy5ib3JkZXJXaWR0aH1cIiBib3JkZXItY29sb3I9XCIke3RoaXMuYm9yZGVyQ29sb3J9XCI+YDtcbiAgICAgICAgZm9yKGxldCBjb250YWluZXIgb2YgdGhpcy5jb250YWluZXJzKXtcbiAgICAgICAgICAgIHhtbCs9Y29udGFpbmVyLnRvWG1sKCk7XG4gICAgICAgIH1cbiAgICAgICAgeG1sKz1gPC9ncmlkPmA7XG4gICAgICAgIHJldHVybiB4bWw7XG4gICAgfVxuICAgIHNldEJvcmRlcldpZHRoKHdpZHRoKXtcbiAgICAgICAgdmFyIHNlbGY9dGhpcztcbiAgICAgICAgJC5lYWNoKHRoaXMuY29udGFpbmVycyxmdW5jdGlvbihpbmRleCxjb250YWluZXIpe1xuICAgICAgICAgICAgY29udGFpbmVyLmNvbnRhaW5lci5jc3MoXCJib3JkZXJcIixcInNvbGlkIFwiK3dpZHRoK1wicHggXCIrc2VsZi5ib3JkZXJDb2xvcitcIlwiKTtcbiAgICAgICAgfSk7XG4gICAgICAgIHRoaXMuYm9yZGVyV2lkdGg9d2lkdGg7XG4gICAgfVxuICAgIHNldEJvcmRlckNvbG9yKGNvbG9yKXtcbiAgICAgICAgdmFyIHNlbGY9dGhpcztcbiAgICAgICAgJC5lYWNoKHRoaXMuY29udGFpbmVycyxmdW5jdGlvbihpbmRleCxjb250YWluZXIpe1xuICAgICAgICAgICAgY29udGFpbmVyLmNvbnRhaW5lci5jc3MoXCJib3JkZXJcIixcInNvbGlkIFwiK3NlbGYuYm9yZGVyV2lkdGgrXCJweCBcIitjb2xvcitcIlwiKTtcbiAgICAgICAgfSk7XG4gICAgICAgIHRoaXMuYm9yZGVyQ29sb3I9Y29sb3I7XG4gICAgfVxufVxuR3JpZFNpbmdsZUluc3RhbmNlLlRZUEU9XCJHcmlkU2luZ2xlXCI7IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTEyLlxuICovXG5pbXBvcnQgVXRpbHMgZnJvbSAnLi4vVXRpbHMuanMnO1xuXG5leHBvcnQgZGVmYXVsdCBjbGFzcyBJbnN0YW5jZXtcbiAgICBjb25zdHJ1Y3Rvcigpe1xuICAgICAgICB0aGlzLmxhYmVsUG9zaXRpb249SW5zdGFuY2UuVE9QO1xuICAgICAgICB0aGlzLmVuYWJsZT1cInRydWVcIjtcbiAgICAgICAgdGhpcy52aXNpYmxlPVwidHJ1ZVwiO1xuICAgIH1cbiAgICBuZXdFbGVtZW50KGxhYmVsKXtcbiAgICAgICAgdGhpcy5lbGVtZW50PSQoXCI8ZGl2IGNsYXNzPSdmb3JtLWdyb3VwIHJvdycgc3R5bGU9J21hcmdpbjowcHgnPlwiKTtcbiAgICAgICAgdGhpcy5sYWJlbD1sYWJlbDtcbiAgICAgICAgdGhpcy5sYWJlbEVsZW1lbnQ9JChcIjxzcGFuIGNsYXNzPSdjb250cm9sLWxhYmVsJyBzdHlsZT0nZm9udC1zaXplOiAxM3B4Jz48L3NwYW4+XCIpO1xuICAgICAgICB0aGlzLmVsZW1lbnQuYXBwZW5kKHRoaXMubGFiZWxFbGVtZW50KTtcbiAgICAgICAgdGhpcy5sYWJlbEVsZW1lbnQudGV4dChsYWJlbCk7XG4gICAgICAgIHJldHVybiB0aGlzLmVsZW1lbnQ7XG4gICAgfVxuICAgIHNldExhYmVsKGxhYmVsKXtcbiAgICAgICAgdGhpcy5sYWJlbD1sYWJlbDtcbiAgICAgICAgaWYodGhpcy5pc1JlcXVpcmVkKXtcbiAgICAgICAgICAgIHRoaXMubGFiZWxFbGVtZW50Lmh0bWwodGhpcy5sYWJlbCtcIjxzcGFuIHN0eWxlPSdjb2xvcjpyZWQnPio8L3NwYW4+XCIpO1xuICAgICAgICB9ZWxzZXtcbiAgICAgICAgICAgIHRoaXMubGFiZWxFbGVtZW50Lmh0bWwodGhpcy5sYWJlbCk7XG4gICAgICAgIH1cbiAgICB9XG4gICAgc2V0TGFiZWxQb3NpdGlvbihwb3NpdGlvbil7XG4gICAgICAgIGlmKHRoaXMubGFiZWxQb3NpdGlvbj09PXBvc2l0aW9uKXtcbiAgICAgICAgICAgIHJldHVybjtcbiAgICAgICAgfVxuICAgICAgICB0aGlzLmxhYmVsUG9zaXRpb249cG9zaXRpb247XG4gICAgICAgIGlmKHBvc2l0aW9uPT09SW5zdGFuY2UuVE9QKXtcbiAgICAgICAgICAgIHRoaXMubGFiZWxFbGVtZW50LnJlbW92ZUNsYXNzKEluc3RhbmNlLlBPU19DTEFTU0VTWzBdKTtcbiAgICAgICAgICAgIHRoaXMuaW5wdXRFbGVtZW50LnJlbW92ZUNsYXNzKEluc3RhbmNlLlBPU19DTEFTU0VTWzFdKTtcbiAgICAgICAgfWVsc2UgaWYocG9zaXRpb249PT1JbnN0YW5jZS5MRUZUKXtcbiAgICAgICAgICAgIHRoaXMubGFiZWxFbGVtZW50LmFkZENsYXNzKEluc3RhbmNlLlBPU19DTEFTU0VTWzBdKTtcbiAgICAgICAgICAgIHRoaXMuaW5wdXRFbGVtZW50LmFkZENsYXNzKEluc3RhbmNlLlBPU19DTEFTU0VTWzFdKTtcbiAgICAgICAgfVxuICAgIH1cbiAgICBzZXRCaW5kUGFyYW1ldGVyKGJpbmRQYXJhbWV0ZXIpe1xuICAgICAgICB0aGlzLmJpbmRQYXJhbWV0ZXI9YmluZFBhcmFtZXRlcjtcbiAgICB9XG4gICAgZ2V0RWxlbWVudElkKCl7XG4gICAgICAgIGlmKFV0aWxzLmJpbmRpbmcpe1xuICAgICAgICAgICAgaWYoIXRoaXMuYmluZFRhYmxlTmFtZSl7XG4gICAgICAgICAgICAgICAgdGhpcy5iaW5kVGFibGVOYW1lPWZvcm1CdWlsZGVyLmJpbmRUYWJsZS5uYW1lO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgaWYodGhpcy5iaW5kVGFibGVOYW1lICYmIHRoaXMuYmluZEZpZWxkKXtcbiAgICAgICAgICAgICAgICByZXR1cm4gdGhpcy5iaW5kVGFibGVOYW1lK1wiLlwiK3RoaXMuYmluZEZpZWxkO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgcmV0dXJuIG51bGw7XG4gICAgICAgIH1lbHNle1xuICAgICAgICAgICAgcmV0dXJuIHRoaXMubGFiZWw7XG4gICAgICAgIH1cbiAgICB9XG4gICAgZnJvbUpzb24oanNvbil7XG4gICAgICAgIHRoaXMuc2V0TGFiZWwoanNvbi5sYWJlbCk7XG4gICAgICAgIHRoaXMuc2V0TGFiZWxQb3NpdGlvbihqc29uLmxhYmVsUG9zaXRpb24pO1xuICAgICAgICB0aGlzLnNldEJpbmRQYXJhbWV0ZXIoanNvbi5iaW5kUGFyYW1ldGVyKTtcbiAgICB9XG4gICAgaW5pdEZyb21Kc29uKGpzb24pe1xuXG4gICAgfVxufVxuSW5zdGFuY2UuTEVGVD1cImxlZnRcIjtcbkluc3RhbmNlLlRPUD1cInRvcFwiO1xuSW5zdGFuY2UuUE9TX0NMQVNTRVM9W1wiY29sLW1kLTNcIixcImNvbC1tZC05XCJdOyIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNS8xMi80LlxuICovXG5leHBvcnQgZGVmYXVsdCBjbGFzcyBPcHRpb257XG4gICAgY29uc3RydWN0b3IobGFiZWwpe1xuICAgICAgICB0aGlzLmxhYmVsPWxhYmVsO1xuICAgICAgICB0aGlzLnZhbHVlPWxhYmVsO1xuICAgICAgICB0aGlzLmVsZW1lbnQ9JChcIjxvcHRpb24gdmFsdWU9J1wiK2xhYmVsK1wiJz5cIitsYWJlbCtcIjwvb3B0aW9uPlwiKTtcbiAgICB9XG4gICAgaW5pdEZyb21Kc29uKGpzb24pe1xuICAgICAgICB0aGlzLnNldFZhbHVlKGpzb24pO1xuICAgIH1cbiAgICB0b0pzb24oKXtcbiAgICAgICAgcmV0dXJuIHtcbiAgICAgICAgICAgIGxhYmVsOnRoaXMubGFiZWwsXG4gICAgICAgICAgICB2YWx1ZTp0aGlzLnZhbHVlXG4gICAgICAgIH07XG4gICAgfVxuICAgIHNldFZhbHVlKGpzb24pe1xuICAgICAgICB0aGlzLnZhbHVlPWpzb24udmFsdWU7XG4gICAgICAgIHRoaXMuZWxlbWVudC5wcm9wKFwidmFsdWVcIixqc29uLnZhbHVlKTtcbiAgICAgICAgdGhpcy5sYWJlbD1qc29uLmxhYmVsO1xuICAgICAgICB0aGlzLmVsZW1lbnQudGV4dChqc29uLmxhYmVsKTtcbiAgICB9XG4gICAgcmVtb3ZlKCl7XG4gICAgICAgIHRoaXMuZWxlbWVudC5yZW1vdmUoKTtcbiAgICB9XG59IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTE2LlxuICovXG5pbXBvcnQgVXRpbHMgZnJvbSAnLi4vVXRpbHMuanMnO1xuaW1wb3J0IENoZWNrYm94SW5zdGFuY2UgZnJvbSAnLi9DaGVja2JveEluc3RhbmNlLmpzJztcbmV4cG9ydCBkZWZhdWx0IGNsYXNzIFJhZGlve1xuICAgIGNvbnN0cnVjdG9yKG9wdGlvbnNJbmxpbmUpe1xuICAgICAgICB2YXIgc2VxPVV0aWxzLnNlcShSYWRpby5JRCk7XG4gICAgICAgIHRoaXMubGFiZWw9XCLpgInpoblcIitzZXE7XG4gICAgICAgIHRoaXMudmFsdWU9dGhpcy5sYWJlbDtcbiAgICAgICAgdGhpcy5yYWRpbz0kKFwiPGlucHV0IHR5cGU9J3JhZGlvJz5cIik7XG4gICAgICAgIHZhciBpbmxpbmVDbGFzcz1DaGVja2JveEluc3RhbmNlLkxBQkVMX1BPU0lUSU9OWzBdO1xuICAgICAgICBpZihvcHRpb25zSW5saW5lKXtcbiAgICAgICAgICAgIGlubGluZUNsYXNzPUNoZWNrYm94SW5zdGFuY2UuTEFCRUxfUE9TSVRJT05bMV07XG4gICAgICAgIH1cbiAgICAgICAgdGhpcy5lbGVtZW50PSQoXCI8c3BhbiBjbGFzcz0nXCIraW5saW5lQ2xhc3MrXCInPjwvc3Bhbj5cIik7XG4gICAgICAgIHRoaXMuZWxlbWVudC5hcHBlbmQodGhpcy5yYWRpbyk7XG4gICAgICAgIHRoaXMubGFiZWxFbGVtZW50PSQoXCI8c3Bhbj5cIit0aGlzLmxhYmVsK1wiPC9zcGFuPlwiKTtcbiAgICAgICAgdGhpcy5lbGVtZW50LmFwcGVuZCh0aGlzLmxhYmVsRWxlbWVudCk7XG4gICAgfVxuICAgIHNldFZhbHVlKGpzb24pe1xuICAgICAgICB0aGlzLmxhYmVsPWpzb24ubGFiZWw7XG4gICAgICAgIHRoaXMudmFsdWU9anNvbi52YWx1ZTtcbiAgICAgICAgdGhpcy5yYWRpby5wcm9wKFwidmFsdWVcIix0aGlzLnZhbHVlKTtcbiAgICAgICAgdGhpcy5sYWJlbEVsZW1lbnQuaHRtbChqc29uLmxhYmVsKTtcbiAgICB9XG4gICAgaW5pdEZyb21Kc29uKGpzb24pe1xuICAgICAgICB0aGlzLnNldFZhbHVlKGpzb24pO1xuICAgIH1cbiAgICB0b0pzb24oKXtcbiAgICAgICAgcmV0dXJuIHtsYWJlbDp0aGlzLmxhYmVsLHZhbHVlOnRoaXMudmFsdWV9O1xuICAgIH1cbn1cblJhZGlvLklEPVwiUmFkaW9cIjsiLCIvKipcbiAqIENyZWF0ZWQgYnkgSmFja3kuR2FvIG9uIDIwMTctMTAtMTYuXG4gKi9cbmltcG9ydCBJbnN0YW5jZSBmcm9tICcuL0luc3RhbmNlLmpzJztcbmltcG9ydCBVdGlscyBmcm9tICcuLi9VdGlscy5qcyc7XG5pbXBvcnQgUmFkaW8gZnJvbSAnLi9SYWRpby5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBSYWRpb0luc3RhbmNlIGV4dGVuZHMgSW5zdGFuY2V7XG4gICAgY29uc3RydWN0b3Ioc2VxKXtcbiAgICAgICAgc3VwZXIoKTtcbiAgICAgICAgdGhpcy5zZXE9VXRpbHMuc2VxKFJhZGlvSW5zdGFuY2UuSUQpO1xuICAgICAgICB0aGlzLmxhYmVsPVwi5Y2V6YCJ5qGGXCIrdGhpcy5zZXE7XG4gICAgICAgIHRoaXMuZWxlbWVudD10aGlzLm5ld0VsZW1lbnQodGhpcy5sYWJlbCk7XG4gICAgICAgIHRoaXMuaW5wdXRFbGVtZW50PSQoXCI8ZGl2PlwiKTtcbiAgICAgICAgdGhpcy5lbGVtZW50LmFwcGVuZCh0aGlzLmlucHV0RWxlbWVudCk7XG4gICAgICAgIHRoaXMub3B0aW9ucz1bXTtcbiAgICAgICAgdGhpcy5lbGVtZW50LnVuaXF1ZUlkKCk7XG4gICAgICAgIHRoaXMuaWQ9dGhpcy5lbGVtZW50LnByb3AoXCJpZFwiKTtcbiAgICAgICAgdGhpcy5vcHRpb25zSW5saW5lPWZhbHNlO1xuICAgICAgICB0aGlzLmFkZE9wdGlvbigpO1xuICAgICAgICB0aGlzLmFkZE9wdGlvbigpO1xuICAgICAgICB0aGlzLmFkZE9wdGlvbigpO1xuICAgIH1cbiAgICBzZXRPcHRpb25zSW5saW5lKG9wdGlvbnNJbmxpbmUpe1xuICAgICAgICBpZihvcHRpb25zSW5saW5lPT09dGhpcy5vcHRpb25zSW5saW5lKXtcbiAgICAgICAgICAgIHJldHVybjtcbiAgICAgICAgfVxuICAgICAgICB0aGlzLm9wdGlvbnNJbmxpbmU9b3B0aW9uc0lubGluZTtcbiAgICAgICAgJC5lYWNoKHRoaXMub3B0aW9ucyxmdW5jdGlvbihpbmRleCxyYWRpbyl7XG4gICAgICAgICAgICB2YXIgZWxlbWVudD1yYWRpby5lbGVtZW50O1xuICAgICAgICAgICAgZWxlbWVudC5yZW1vdmVDbGFzcygpO1xuICAgICAgICAgICAgaWYob3B0aW9uc0lubGluZSl7XG4gICAgICAgICAgICAgICAgZWxlbWVudC5hZGRDbGFzcyhSYWRpb0luc3RhbmNlLkxBQkVMX1BPU0lUSU9OWzFdKTtcbiAgICAgICAgICAgICAgICBlbGVtZW50LmNzcyhcInBhZGRpbmctbGVmdFwiLFwiMHB4XCIpO1xuICAgICAgICAgICAgfWVsc2V7XG4gICAgICAgICAgICAgICAgZWxlbWVudC5hZGRDbGFzcyhSYWRpb0luc3RhbmNlLkxBQkVMX1BPU0lUSU9OWzBdKTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfSk7XG4gICAgfVxuICAgIHJlbW92ZU9wdGlvbihvcHRpb24pe1xuICAgICAgICB2YXIgdGFyZ2V0SW5kZXg7XG4gICAgICAgICQuZWFjaCh0aGlzLm9wdGlvbnMsZnVuY3Rpb24oaW5kZXgsaXRlbSl7XG4gICAgICAgICAgICBpZihpdGVtPT09b3B0aW9uKXtcbiAgICAgICAgICAgICAgICB0YXJnZXRJbmRleD1pbmRleDtcbiAgICAgICAgICAgICAgICByZXR1cm4gZmFsc2U7XG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuICAgICAgICB0aGlzLm9wdGlvbnMuc3BsaWNlKHRhcmdldEluZGV4LDEpO1xuICAgICAgICBvcHRpb24uZWxlbWVudC5yZW1vdmUoKTtcbiAgICB9XG4gICAgYWRkT3B0aW9uKGpzb24pe1xuICAgICAgICB2YXIgcmFkaW89bmV3IFJhZGlvKHRoaXMub3B0aW9uc0lubGluZSk7XG4gICAgICAgIGlmKGpzb24pe1xuICAgICAgICAgICAgcmFkaW8uaW5pdEZyb21Kc29uKGpzb24pO1xuICAgICAgICB9XG4gICAgICAgIHRoaXMub3B0aW9ucy5wdXNoKHJhZGlvKTtcbiAgICAgICAgdGhpcy5pbnB1dEVsZW1lbnQuYXBwZW5kKHJhZGlvLmVsZW1lbnQpO1xuICAgICAgICB2YXIgaW5wdXQ9cmFkaW8uZWxlbWVudC5maW5kKFwiaW5wdXRcIikuZmlyc3QoKTtcbiAgICAgICAgaWYoIXRoaXMub3B0aW9uc0lubGluZSl7XG4gICAgICAgICAgICBpbnB1dC5jc3MoXCJtYXJnaW4tbGVmdFwiLFwiYXV0b1wiKTtcbiAgICAgICAgfVxuICAgICAgICBpbnB1dC5wcm9wKFwibmFtZVwiLFwicmFkaW9vcHRpb25cIit0aGlzLnNlcSk7XG4gICAgICAgIHJldHVybiByYWRpbztcbiAgICB9XG4gICAgaW5pdEZyb21Kc29uKGpzb24pe1xuICAgICAgICAkLmVhY2godGhpcy5vcHRpb25zLGZ1bmN0aW9uKGluZGV4LGl0ZW0pe1xuICAgICAgICAgICAgaXRlbS5lbGVtZW50LnJlbW92ZSgpO1xuICAgICAgICB9KTtcbiAgICAgICAgdGhpcy5vcHRpb25zLnNwbGljZSgwLHRoaXMub3B0aW9ucy5sZW5ndGgpO1xuICAgICAgICBzdXBlci5mcm9tSnNvbihqc29uKTtcbiAgICAgICAgdmFyIG9wdGlvbnM9anNvbi5vcHRpb25zO1xuICAgICAgICBmb3IodmFyIGk9MDtpPG9wdGlvbnMubGVuZ3RoO2krKyl7XG4gICAgICAgICAgICB0aGlzLmFkZE9wdGlvbihvcHRpb25zW2ldKTtcbiAgICAgICAgfVxuICAgICAgICBpZihqc29uLm9wdGlvbnNJbmxpbmUhPT11bmRlZmluZWQpe1xuICAgICAgICAgICAgdGhpcy5zZXRPcHRpb25zSW5saW5lKGpzb24ub3B0aW9uc0lubGluZSk7XG4gICAgICAgIH1cbiAgICB9XG4gICAgdG9Kc29uKCl7XG4gICAgICAgIGNvbnN0IGpzb249e1xuICAgICAgICAgICAgbGFiZWw6dGhpcy5sYWJlbCxcbiAgICAgICAgICAgIG9wdGlvbnNJbmxpbmU6dGhpcy5vcHRpb25zSW5saW5lLFxuICAgICAgICAgICAgbGFiZWxQb3NpdGlvbjp0aGlzLmxhYmVsUG9zaXRpb24sXG4gICAgICAgICAgICBiaW5kUGFyYW1ldGVyOnRoaXMuYmluZFBhcmFtZXRlcixcbiAgICAgICAgICAgIHR5cGU6UmFkaW9JbnN0YW5jZS5UWVBFLFxuICAgICAgICAgICAgb3B0aW9uczpbXVxuICAgICAgICB9O1xuICAgICAgICBmb3IobGV0IG9wdGlvbiBvZiB0aGlzLm9wdGlvbnMpe1xuICAgICAgICAgICAganNvbi5vcHRpb25zLnB1c2gob3B0aW9uLnRvSnNvbigpKTtcbiAgICAgICAgfVxuICAgICAgICByZXR1cm4ganNvbjtcbiAgICB9XG4gICAgdG9YbWwoKXtcbiAgICAgICAgbGV0IHhtbD1gPGlucHV0LXJhZGlvIGxhYmVsPVwiJHt0aGlzLmxhYmVsfVwiIHR5cGU9XCIke1JhZGlvSW5zdGFuY2UuVFlQRX1cIiBvcHRpb25zLWlubGluZT1cIiR7dGhpcy5vcHRpb25zSW5saW5lfVwiIGxhYmVsLXBvc2l0aW9uPVwiJHt0aGlzLmxhYmVsUG9zaXRpb24gfHwgJ3RvcCd9XCIgYmluZC1wYXJhbWV0ZXI9XCIke3RoaXMuYmluZFBhcmFtZXRlciB8fCAnJ31cIj5gO1xuICAgICAgICBmb3IobGV0IG9wdGlvbiBvZiB0aGlzLm9wdGlvbnMpe1xuICAgICAgICAgICAgeG1sKz1gPG9wdGlvbiBsYWJlbD1cIiR7b3B0aW9uLmxhYmVsfVwiIHZhbHVlPVwiJHtvcHRpb24udmFsdWV9XCI+PC9vcHRpb24+YDtcbiAgICAgICAgfVxuICAgICAgICB4bWwrPWA8L2lucHV0LXJhZGlvPmA7XG4gICAgICAgIHJldHVybiB4bWw7XG4gICAgfVxufVxuUmFkaW9JbnN0YW5jZS5UWVBFPVwiUmFkaW9cIjtcblJhZGlvSW5zdGFuY2UuTEFCRUxfUE9TSVRJT049W1wiY2hlY2tib3hcIixcImNoZWNrYm94LWlubGluZVwiXTtcblJhZGlvSW5zdGFuY2UuSUQ9XCJyYWRpb19pbnN0YW5jZVwiOyIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0yMC5cbiAqL1xuaW1wb3J0IEJ1dHRvbkluc3RhbmNlIGZyb20gJy4vQnV0dG9uSW5zdGFuY2UuanMnO1xuXG5leHBvcnQgZGVmYXVsdCBjbGFzcyBSZXNldEJ1dHRvbkluc3RhbmNlIGV4dGVuZHMgQnV0dG9uSW5zdGFuY2V7XG4gICAgY29uc3RydWN0b3IobGFiZWwpe1xuICAgICAgICBzdXBlcihsYWJlbCk7XG4gICAgICAgIHRoaXMuZWRpdG9yVHlwZT1cInJlc2V0LWJ1dHRvblwiO1xuICAgIH1cbiAgICB0b0pzb24oKXtcbiAgICAgICAgcmV0dXJuIHtcbiAgICAgICAgICAgIGxhYmVsOnRoaXMubGFiZWwsXG4gICAgICAgICAgICBzdHlsZTp0aGlzLnN0eWxlLFxuICAgICAgICAgICAgYWxpZ246dGhpcy5hbGlnbixcbiAgICAgICAgICAgIHR5cGU6UmVzZXRCdXR0b25JbnN0YW5jZS5UWVBFXG4gICAgICAgIH07XG4gICAgfVxuICAgIHRvWG1sKCl7XG4gICAgICAgIHJldHVybiBgPGJ1dHRvbi1yZXNldCBsYWJlbD1cIiR7dGhpcy5sYWJlbH1cIiBhbGlnbj1cIiR7dGhpcy5hbGlnbn1cIiB0eXBlPVwiJHtSZXNldEJ1dHRvbkluc3RhbmNlLlRZUEV9XCIgc3R5bGU9XCIke3RoaXMuc3R5bGV9XCI+PC9idXR0b24tcmVzZXQ+YDtcbiAgICB9XG59XG5SZXNldEJ1dHRvbkluc3RhbmNlLlRZUEU9J1Jlc2V0LWJ1dHRvbic7IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTIwLlxuICovXG5pbXBvcnQgSW5zdGFuY2UgZnJvbSAnLi9JbnN0YW5jZS5qcyc7XG5pbXBvcnQgT3B0aW9uIGZyb20gJy4vT3B0aW9uLmpzJztcbmV4cG9ydCBkZWZhdWx0IGNsYXNzIFNlbGVjdEluc3RhbmNlIGV4dGVuZHMgSW5zdGFuY2V7XG4gICAgY29uc3RydWN0b3Ioc2VxKXtcbiAgICAgICAgc3VwZXIoKTtcbiAgICAgICAgdmFyIGxhYmVsPVwi5Y2V6YCJ5YiX6KGoXCIrc2VxO1xuICAgICAgICB0aGlzLmVsZW1lbnQ9dGhpcy5uZXdFbGVtZW50KGxhYmVsKTtcbiAgICAgICAgdGhpcy5pbnB1dEVsZW1lbnQ9JChcIjxkaXY+XCIpO1xuICAgICAgICB0aGlzLnNlbGVjdD0kKFwiPHNlbGVjdCBjbGFzcz0nZm9ybS1jb250cm9sJz5cIik7XG4gICAgICAgIHRoaXMuaW5wdXRFbGVtZW50LmFwcGVuZCh0aGlzLnNlbGVjdCk7XG4gICAgICAgIHRoaXMuZWxlbWVudC5hcHBlbmQodGhpcy5pbnB1dEVsZW1lbnQpO1xuICAgICAgICB0aGlzLm9wdGlvbnM9W107XG4gICAgICAgIHRoaXMub3B0aW9uTnVtPTE7XG4gICAgICAgIGZvcih2YXIgaT0xO2k8NTtpKyspe1xuICAgICAgICAgICAgdGhpcy5hZGRPcHRpb24oKTtcbiAgICAgICAgfVxuICAgICAgICB0aGlzLmVsZW1lbnQudW5pcXVlSWQoKTtcbiAgICAgICAgdGhpcy5pZD10aGlzLmVsZW1lbnQucHJvcChcImlkXCIpO1xuICAgIH1cbiAgICBhZGRPcHRpb24oanNvbil7XG4gICAgICAgIHZhciBvcHRpb249bmV3IE9wdGlvbihcIumAiemhuVwiKyh0aGlzLm9wdGlvbk51bSsrKSk7XG4gICAgICAgIGlmKGpzb24pe1xuICAgICAgICAgICAgb3B0aW9uLmluaXRGcm9tSnNvbihqc29uKTtcbiAgICAgICAgfVxuICAgICAgICB0aGlzLm9wdGlvbnMucHVzaChvcHRpb24pO1xuICAgICAgICB0aGlzLnNlbGVjdC5hcHBlbmQob3B0aW9uLmVsZW1lbnQpO1xuICAgICAgICByZXR1cm4gb3B0aW9uO1xuICAgIH1cbiAgICByZW1vdmVPcHRpb24ob3B0aW9uKXtcbiAgICAgICAgdmFyIHRhcmdldEluZGV4O1xuICAgICAgICAkLmVhY2godGhpcy5vcHRpb25zLGZ1bmN0aW9uKGluZGV4LGl0ZW0pe1xuICAgICAgICAgICAgaWYoaXRlbT09PW9wdGlvbil7XG4gICAgICAgICAgICAgICAgdGFyZ2V0SW5kZXg9aW5kZXg7XG4gICAgICAgICAgICAgICAgcmV0dXJuIGZhbHNlO1xuICAgICAgICAgICAgfVxuICAgICAgICB9KTtcbiAgICAgICAgdGhpcy5vcHRpb25zLnNwbGljZSh0YXJnZXRJbmRleCwxKTtcbiAgICAgICAgb3B0aW9uLnJlbW92ZSgpO1xuICAgIH1cbiAgICBpbml0RnJvbUpzb24oanNvbil7XG4gICAgICAgICQuZWFjaCh0aGlzLm9wdGlvbnMsZnVuY3Rpb24oaW5kZXgsaXRlbSl7XG4gICAgICAgICAgICBpdGVtLmVsZW1lbnQucmVtb3ZlKCk7XG4gICAgICAgIH0pO1xuICAgICAgICB0aGlzLm9wdGlvbnMuc3BsaWNlKDAsdGhpcy5vcHRpb25zLmxlbmd0aCk7XG4gICAgICAgIHN1cGVyLmZyb21Kc29uKGpzb24pO1xuICAgICAgICBpZihqc29uLnNlYXJjaE9wZXJhdG9yKXtcbiAgICAgICAgICAgIHRoaXMuc2VhcmNoT3BlcmF0b3I9anNvbi5zZWFyY2hPcGVyYXRvcjtcbiAgICAgICAgfVxuICAgICAgICB2YXIgb3B0aW9ucz1qc29uLm9wdGlvbnM7XG4gICAgICAgIGZvcih2YXIgaT0wO2k8b3B0aW9ucy5sZW5ndGg7aSsrKXtcbiAgICAgICAgICAgIHRoaXMuYWRkT3B0aW9uKG9wdGlvbnNbaV0pO1xuICAgICAgICB9XG4gICAgICAgIHRoaXMudXNlRGF0YXNldD1qc29uLnVzZURhdGFzZXQ7XG4gICAgICAgIHRoaXMuZGF0YXNldD1qc29uLmRhdGFzZXQ7XG4gICAgICAgIHRoaXMubGFiZWxGaWVsZD1qc29uLmxhYmVsRmllbGQ7XG4gICAgICAgIHRoaXMudmFsdWVGaWVsZD1qc29uLnZhbHVlRmllbGQ7XG4gICAgfVxuICAgIHRvSnNvbigpe1xuICAgICAgICBjb25zdCBqc29uPXtcbiAgICAgICAgICAgIGxhYmVsOnRoaXMubGFiZWwsXG4gICAgICAgICAgICBvcHRpb25zSW5saW5lOnRoaXMub3B0aW9uc0lubGluZSxcbiAgICAgICAgICAgIGxhYmVsUG9zaXRpb246dGhpcy5sYWJlbFBvc2l0aW9uLFxuICAgICAgICAgICAgYmluZFBhcmFtZXRlcjp0aGlzLmJpbmRQYXJhbWV0ZXIsXG4gICAgICAgICAgICB0eXBlOlNlbGVjdEluc3RhbmNlLlRZUEUsXG4gICAgICAgICAgICB1c2VEYXRhc2V0OnRoaXMudXNlRGF0YXNldCxcbiAgICAgICAgICAgIGRhdGFzZXQ6dGhpcy5kYXRhc2V0LFxuICAgICAgICAgICAgbGFiZWxGaWVsZDp0aGlzLmxhYmVsRmllbGQsXG4gICAgICAgICAgICB2YWx1ZUZpZWxkOnRoaXMudmFsdWVGaWVsZCxcbiAgICAgICAgICAgIG9wdGlvbnM6W11cbiAgICAgICAgfTtcbiAgICAgICAgZm9yKGxldCBvcHRpb24gb2YgdGhpcy5vcHRpb25zKXtcbiAgICAgICAgICAgIGpzb24ub3B0aW9ucy5wdXNoKG9wdGlvbi50b0pzb24oKSk7XG4gICAgICAgIH1cbiAgICAgICAgcmV0dXJuIGpzb247XG4gICAgfVxuICAgIHRvWG1sKCl7XG4gICAgICAgIGxldCB4bWw9YDxpbnB1dC1zZWxlY3QgbGFiZWw9XCIke3RoaXMubGFiZWx9XCIgdHlwZT1cIiR7U2VsZWN0SW5zdGFuY2UuVFlQRX1cIiBsYWJlbC1wb3NpdGlvbj1cIiR7dGhpcy5sYWJlbFBvc2l0aW9uIHx8ICd0b3AnfVwiIGJpbmQtcGFyYW1ldGVyPVwiJHt0aGlzLmJpbmRQYXJhbWV0ZXIgfHwgJyd9XCJgO1xuICAgICAgICBpZih0aGlzLnVzZURhdGFzZXQpe1xuICAgICAgICAgICAgeG1sKz1gIHVzZS1kYXRhc2V0PVwiJHt0aGlzLnVzZURhdGFzZXR9XCIgZGF0YXNldD1cIiR7dGhpcy5kYXRhc2V0fVwiIGxhYmVsLWZpZWxkPVwiJHt0aGlzLmxhYmVsRmllbGR9XCIgdmFsdWUtZmllbGQ9XCIke3RoaXMudmFsdWVGaWVsZH1cImA7XG4gICAgICAgIH1cbiAgICAgICAgeG1sKz0nPic7XG4gICAgICAgIGZvcihsZXQgb3B0aW9uIG9mIHRoaXMub3B0aW9ucyB8fCBbXSl7XG4gICAgICAgICAgICB4bWwrPWA8b3B0aW9uIGxhYmVsPVwiJHtvcHRpb24ubGFiZWx9XCIgdmFsdWU9XCIke29wdGlvbi52YWx1ZX1cIj48L29wdGlvbj5gO1xuICAgICAgICB9XG4gICAgICAgIHhtbCs9YDwvaW5wdXQtc2VsZWN0PmA7XG4gICAgICAgIHJldHVybiB4bWw7XG4gICAgfVxufVxuU2VsZWN0SW5zdGFuY2UuVFlQRT1cIlNlbGVjdFwiOyIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0yMC5cbiAqL1xuaW1wb3J0IEJ1dHRvbkluc3RhbmNlIGZyb20gJy4vQnV0dG9uSW5zdGFuY2UuanMnO1xuXG5leHBvcnQgZGVmYXVsdCBjbGFzcyBTdWJtaXRCdXR0b25JbnN0YW5jZSBleHRlbmRzIEJ1dHRvbkluc3RhbmNle1xuICAgIGNvbnN0cnVjdG9yKGxhYmVsKXtcbiAgICAgICAgc3VwZXIobGFiZWwpO1xuICAgICAgICB0aGlzLmVkaXRvclR5cGU9XCJzdWJtaXQtYnV0dG9uXCI7XG4gICAgfVxuICAgIHRvSnNvbigpe1xuICAgICAgICByZXR1cm4ge1xuICAgICAgICAgICAgbGFiZWw6dGhpcy5sYWJlbCxcbiAgICAgICAgICAgIHN0eWxlOnRoaXMuc3R5bGUsXG4gICAgICAgICAgICBhbGlnbjp0aGlzLmFsaWduLFxuICAgICAgICAgICAgdHlwZTpTdWJtaXRCdXR0b25JbnN0YW5jZS5UWVBFXG4gICAgICAgIH07XG4gICAgfVxuICAgIHRvWG1sKCl7XG4gICAgICAgIHJldHVybiBgPGJ1dHRvbi1zdWJtaXQgbGFiZWw9XCIke3RoaXMubGFiZWx9XCIgYWxpZ249XCIke3RoaXMuYWxpZ259XCIgdHlwZT1cIiR7U3VibWl0QnV0dG9uSW5zdGFuY2UuVFlQRX1cIiBzdHlsZT1cIiR7dGhpcy5zdHlsZX1cIj48L2J1dHRvbi1zdWJtaXQ+YDtcbiAgICB9XG59XG5TdWJtaXRCdXR0b25JbnN0YW5jZS5UWVBFPVwiU3VibWl0LWJ1dHRvblwiO1xuIiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTEyLlxuICovXG5pbXBvcnQgVGFiQ29udGFpbmVyIGZyb20gJy4uL2NvbnRhaW5lci9UYWJDb250YWluZXIuanMnO1xuXG5leHBvcnQgZGVmYXVsdCBjbGFzcyBUYWJ7XG4gICAgY29uc3RydWN0b3Ioc2VxLHRhYm51bSl7XG4gICAgICAgIHRoaXMubGk9JChcIjxsaT5cIik7XG4gICAgICAgIHRoaXMuaWQ9XCJ0YWJDb250ZW50XCIrc2VxK1wiXCIrdGFibnVtO1xuICAgICAgICB0aGlzLnRhYk5hbWU9XCLpobXnrb5cIit0YWJudW07XG4gICAgICAgIHRoaXMubGluaz0kKFwiPGEgaHJlZj0nI1wiK3RoaXMuaWQrXCInIGRhdGEtdG9nZ2xlPSd0YWInPlwiK3RoaXMudGFiTmFtZStcIjwvYT5cIik7XG4gICAgICAgIHRoaXMubGluay5jbGljayhmdW5jdGlvbihlKXtcbiAgICAgICAgICAgICQodGhpcykudGFiKCdzaG93Jyk7XG4gICAgICAgICAgICBlLnN0b3BQcm9wYWdhdGlvbigpO1xuICAgICAgICB9KTtcbiAgICAgICAgdGhpcy5saS5hcHBlbmQodGhpcy5saW5rKTtcbiAgICAgICAgdGhpcy5jb250YWluZXI9bmV3IFRhYkNvbnRhaW5lcih0aGlzLmlkKTtcbiAgICB9XG4gICAgZ2V0VGFiTmFtZSgpe1xuICAgICAgICByZXR1cm4gdGhpcy50YWJOYW1lO1xuICAgIH1cbiAgICBzZXRUYWJOYW1lKHRhYk5hbWUpe1xuICAgICAgICB0aGlzLnRhYk5hbWU9dGFiTmFtZTtcbiAgICAgICAgdGhpcy5saW5rLnRleHQodGFiTmFtZSk7XG4gICAgfVxuICAgIGxpVG9IdG1sKCl7XG4gICAgICAgIHZhciBsaT0kKFwiPGxpPlwiKTtcbiAgICAgICAgbGkuYXBwZW5kKCQoXCI8YSBocmVmPScjXCIrdGhpcy5pZCtcIjEnIGRhdGEtdG9nZ2xlPSd0YWInPlwiK3RoaXMudGFiTmFtZStcIjwvYT5cIikpO1xuICAgICAgICByZXR1cm4gbGk7XG4gICAgfVxuICAgIGdldFRhYkNvbnRlbnQoKXtcbiAgICAgICAgcmV0dXJuIHRoaXMuY29udGFpbmVyLmdldENvbnRhaW5lcigpO1xuICAgIH1cbiAgICByZW1vdmUoKXtcbiAgICAgICAgdGhpcy5saS5yZW1vdmUoKTtcbiAgICAgICAgdGhpcy5jb250YWluZXIuZ2V0Q29udGFpbmVyKCkucmVtb3ZlKCk7XG4gICAgfVxuICAgIGluaXRGcm9tSnNvbihqc29uKXtcbiAgICAgICAgdGhpcy5zZXRUYWJOYW1lKGpzb24udGFiTmFtZSk7XG4gICAgICAgIHRoaXMuY29udGFpbmVyLmluaXRGcm9tSnNvbihqc29uLmNvbnRhaW5lcik7XG4gICAgfVxuICAgIHRvSlNPTigpe1xuICAgICAgICByZXR1cm4ge1xuICAgICAgICAgICAgaWQ6dGhpcy5pZCxcbiAgICAgICAgICAgIHRhYk5hbWU6dGhpcy50YWJOYW1lLFxuICAgICAgICAgICAgdHlwZTp0aGlzLmdldFR5cGUoKSxcbiAgICAgICAgICAgIGNvbnRhaW5lcjp0aGlzLmNvbnRhaW5lci50b0pTT04oKVxuICAgICAgICB9O1xuICAgIH1cbiAgICBnZXRUeXBlKCl7XG4gICAgICAgIHJldHVybiBcIlRhYlwiO1xuICAgIH1cbn0iLCIvKipcbiAqIENyZWF0ZWQgYnkgSmFja3kuR2FvIG9uIDIwMTctMTAtMTIuXG4gKi9cbmltcG9ydCBDb250YWluZXJJbnN0YW5jZSBmcm9tICcuL0NvbnRhaW5lckluc3RhbmNlLmpzJztcbmltcG9ydCBUYWIgZnJvbSAnLi9UYWIuanMnO1xuZXhwb3J0IGRlZmF1bHQgY2xhc3MgVGFiQ29udHJvbEluc3RhbmNlIGV4dGVuZHMgQ29udGFpbmVySW5zdGFuY2V7XG4gICAgY29uc3RydWN0b3Ioc2VxKXtcbiAgICAgICAgc3VwZXIoKTtcbiAgICAgICAgdGhpcy5zZXE9c2VxO1xuICAgICAgICB0aGlzLnRhYnM9W107XG4gICAgICAgIHRoaXMudGFiTnVtPTE7XG4gICAgICAgIHRoaXMuZWxlbWVudD0kKFwiPGRpdiBzdHlsZT0nbWluLWhlaWdodDogMTAwcHg7JyBjbGFzcz0ndGFiY29udGFpbmVyJz5cIik7XG4gICAgICAgIHRoaXMudWw9JChcIjx1bCBjbGFzcz0nbmF2IG5hdi10YWJzJz5cIik7XG4gICAgICAgIHRoaXMuZWxlbWVudC5hcHBlbmQodGhpcy51bCk7XG4gICAgICAgIHRoaXMudGFiQ29udGVudD0kKFwiPGRpdiBjbGFzcz0ndGFiLWNvbnRlbnQnPlwiKTtcbiAgICAgICAgdGhpcy5lbGVtZW50LmFwcGVuZCh0aGlzLnRhYkNvbnRlbnQpO1xuICAgICAgICB0aGlzLmFkZFRhYih0cnVlKTtcbiAgICAgICAgdGhpcy5hZGRUYWIoKTtcbiAgICAgICAgdGhpcy5hZGRUYWIoKTtcbiAgICAgICAgdGhpcy5lbGVtZW50LnVuaXF1ZUlkKCk7XG4gICAgICAgIHRoaXMuaWQ9dGhpcy5lbGVtZW50LnByb3AoXCJpZFwiKTtcbiAgICAgICAgdGhpcy52aXNpYmxlPVwidHJ1ZVwiO1xuICAgIH1cbiAgICBhZGRUYWIoYWN0aXZlLGpzb24pe1xuICAgICAgICBsZXQgdGFibnVtPXRoaXMudGFiTnVtKys7XG4gICAgICAgIGNvbnN0IHRhYj1uZXcgVGFiKHRoaXMuc2VxLHRhYm51bSk7XG4gICAgICAgIGlmKGpzb24pe1xuICAgICAgICAgICAgdGFiLmluaXRGcm9tSnNvbihqc29uKTtcbiAgICAgICAgfVxuICAgICAgICB0aGlzLmNvbnRhaW5lcnMucHVzaCh0YWIuY29udGFpbmVyKTtcbiAgICAgICAgZm9ybUJ1aWxkZXIuY29udGFpbmVycy5wdXNoKHRhYi5jb250YWluZXIpO1xuICAgICAgICB2YXIgbGk9dGFiLmxpO1xuICAgICAgICBpZihhY3RpdmUpe1xuICAgICAgICAgICAgbGkuYWRkQ2xhc3MoXCJhY3RpdmVcIik7XG4gICAgICAgIH1cbiAgICAgICAgdGhpcy51bC5hcHBlbmQobGkpO1xuICAgICAgICB2YXIgdGFiQ29udGVudD10YWIuZ2V0VGFiQ29udGVudCgpO1xuICAgICAgICBpZihhY3RpdmUpe1xuICAgICAgICAgICAgdGFiQ29udGVudC5hZGRDbGFzcyhcImluIGFjdGl2ZVwiKTtcbiAgICAgICAgfVxuICAgICAgICB0aGlzLnRhYkNvbnRlbnQuYXBwZW5kKHRhYkNvbnRlbnQpO1xuICAgICAgICB0aGlzLnRhYnMucHVzaCh0YWIpO1xuICAgICAgICByZXR1cm4gdGFiO1xuICAgIH1cbiAgICBnZXRUYWIoaWQpe1xuICAgICAgICBsZXQgdGFyZ2V0VGFiPW51bGw7XG4gICAgICAgICQuZWFjaCh0aGlzLnRhYnMsZnVuY3Rpb24oaW5kZXgsdGFiKXtcbiAgICAgICAgICAgIGlmKHRhYi5nZXRJZCgpPT09aWQpe1xuICAgICAgICAgICAgICAgIHRhcmdldFRhYj10YWI7XG4gICAgICAgICAgICAgICAgcmV0dXJuIGZhbHNlO1xuICAgICAgICAgICAgfVxuICAgICAgICB9KTtcbiAgICAgICAgcmV0dXJuIHRhcmdldFRhYjtcbiAgICB9XG4gICAgaW5pdEZyb21Kc29uKGpzb24pe1xuICAgICAgICAkLmVhY2godGhpcy50YWJzLGZ1bmN0aW9uKGluZGV4LHRhYil7XG4gICAgICAgICAgICB0YWIucmVtb3ZlKCk7XG4gICAgICAgIH0pO1xuICAgICAgICB0aGlzLnRhYnMuc3BsaWNlKDAsdGhpcy50YWJzLmxlbmd0aCk7XG4gICAgICAgIHRoaXMudmlzaWJsZT1qc29uLnZpc2libGU7XG4gICAgICAgIHZhciB0YWJzPWpzb24udGFicztcbiAgICAgICAgZm9yKHZhciBpPTA7aTx0YWJzLmxlbmd0aDtpKyspe1xuICAgICAgICAgICAgdmFyIHRhYj10YWJzW2ldO1xuICAgICAgICAgICAgaWYoaT09PTApe1xuICAgICAgICAgICAgICAgIHRoaXMuYWRkVGFiKHRydWUsdGFiKTtcbiAgICAgICAgICAgIH1lbHNle1xuICAgICAgICAgICAgICAgIHRoaXMuYWRkVGFiKGZhbHNlLHRhYik7XG4gICAgICAgICAgICB9XG4gICAgICAgIH1cbiAgICB9XG4gICAgdG9KU09OKCl7XG4gICAgICAgIHZhciBqc29uPXtpZDp0aGlzLmlkLHR5cGU6VGFiQ29udHJvbEluc3RhbmNlLlRZUEUsdmlzaWJsZTp0aGlzLnZpc2libGV9O1xuICAgICAgICB2YXIgdGFicz1bXTtcbiAgICAgICAgJC5lYWNoKHRoaXMudGFicyxmdW5jdGlvbihpbmRleCx0YWIpe1xuICAgICAgICAgICAgdGFicy5wdXNoKHRhYi50b0pTT04oKSk7XG4gICAgICAgIH0pO1xuICAgICAgICBqc29uLnRhYnM9dGFicztcbiAgICAgICAgcmV0dXJuIGpzb247XG4gICAgfVxufVxuVGFiQ29udHJvbEluc3RhbmNlLlRZUEU9XCJUYWJDb250cm9sXCI7IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTE2LlxuICovXG5pbXBvcnQgSW5zdGFuY2UgZnJvbSAnLi9JbnN0YW5jZS5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBUZXh0SW5zdGFuY2UgZXh0ZW5kcyBJbnN0YW5jZXtcbiAgICBjb25zdHJ1Y3RvcihsYWJlbCl7XG4gICAgICAgIHN1cGVyKCk7XG4gICAgICAgIHRoaXMuZWxlbWVudD10aGlzLm5ld0VsZW1lbnQobGFiZWwpO1xuICAgICAgICB0aGlzLmlucHV0RWxlbWVudD0kKFwiPGRpdj5cIik7XG4gICAgICAgIHRoaXMuZWxlbWVudC5hcHBlbmQodGhpcy5pbnB1dEVsZW1lbnQpO1xuICAgICAgICB0aGlzLnRleHRJbnB1dD0kKFwiPGlucHV0IHR5cGU9XFxcInRleHRcXFwiIGNsYXNzPVxcXCJmb3JtLWNvbnRyb2xcXFwiPlwiKTtcbiAgICAgICAgdGhpcy5pbnB1dEVsZW1lbnQuYXBwZW5kKHRoaXMudGV4dElucHV0KTtcbiAgICAgICAgdGhpcy5lbGVtZW50LnVuaXF1ZUlkKCk7XG4gICAgICAgIHRoaXMuaWQ9dGhpcy5lbGVtZW50LnByb3AoXCJpZFwiKTtcbiAgICAgICAgdGhpcy5lZGl0b3JUeXBlPVwidGV4dFwiO1xuICAgIH1cbiAgICBpbml0RnJvbUpzb24oanNvbil7XG4gICAgICAgIHN1cGVyLmZyb21Kc29uKGpzb24pO1xuICAgICAgICB0aGlzLmVkaXRvclR5cGU9anNvbi5lZGl0b3JUeXBlO1xuICAgICAgICBpZihqc29uLnNlYXJjaE9wZXJhdG9yKXtcbiAgICAgICAgICAgIHRoaXMuc2VhcmNoT3BlcmF0b3I9anNvbi5zZWFyY2hPcGVyYXRvcjtcbiAgICAgICAgfVxuICAgIH1cbiAgICB0b0pzb24oKXtcbiAgICAgICAgY29uc3QganNvbj17XG4gICAgICAgICAgICBsYWJlbDp0aGlzLmxhYmVsLFxuICAgICAgICAgICAgb3B0aW9uc0lubGluZTp0aGlzLm9wdGlvbnNJbmxpbmUsXG4gICAgICAgICAgICBsYWJlbFBvc2l0aW9uOnRoaXMubGFiZWxQb3NpdGlvbixcbiAgICAgICAgICAgIGJpbmRQYXJhbWV0ZXI6dGhpcy5iaW5kUGFyYW1ldGVyLFxuICAgICAgICAgICAgdHlwZTpUZXh0SW5zdGFuY2UuVFlQRVxuICAgICAgICB9O1xuICAgICAgICByZXR1cm4ganNvbjtcbiAgICB9XG4gICAgdG9YbWwoKXtcbiAgICAgICAgY29uc3QgeG1sPWA8aW5wdXQtdGV4dCBsYWJlbD1cIiR7dGhpcy5sYWJlbH1cIiB0eXBlPVwiJHtUZXh0SW5zdGFuY2UuVFlQRX1cIiBsYWJlbC1wb3NpdGlvbj1cIiR7dGhpcy5sYWJlbFBvc2l0aW9uIHx8ICd0b3AnfVwiIGJpbmQtcGFyYW1ldGVyPVwiJHt0aGlzLmJpbmRQYXJhbWV0ZXIgfHwgJyd9XCI+PC9pbnB1dC10ZXh0PmA7XG4gICAgICAgIHJldHVybiB4bWw7XG4gICAgfVxufVxuVGV4dEluc3RhbmNlLlRZUEU9XCJUZXh0XCI7IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTIwLlxuICovXG5pbXBvcnQgUHJvcGVydHkgZnJvbSAnLi9Qcm9wZXJ0eS5qcyc7XG5cbmV4cG9ydCBkZWZhdWx0IGNsYXNzIEJ1dHRvblByb3BlcnR5IGV4dGVuZHMgUHJvcGVydHl7XG4gICAgY29uc3RydWN0b3IoKXtcbiAgICAgICAgc3VwZXIoKTtcbiAgICAgICAgY29uc3QgX3RoaXM9dGhpcztcbiAgICAgICAgdGhpcy5idXR0b25UeXBlPSQoYDxkaXYgY2xhc3M9XCJmb3JtLWdyb3VwXCI+PC9kaXY+YCk7XG4gICAgICAgIHRoaXMuY29sLmFwcGVuZCh0aGlzLmJ1dHRvblR5cGUpO1xuICAgICAgICBjb25zdCBsYWJlbEdyb3VwPSQoYDxkaXYgY2xhc3M9XCJmb3JtLWdyb3VwXCI+PGxhYmVsPuaMiemSruagh+mimDwvbGFiZWw+PC9kaXY+YCk7XG4gICAgICAgIHRoaXMuY29sLmFwcGVuZChsYWJlbEdyb3VwKTtcbiAgICAgICAgdGhpcy5sYWJlbEVkaXRvcj0kKGA8aW5wdXQgdHlwZT1cInRleHRcIiBjbGFzcz1cImZvcm0tY29udHJvbFwiPmApO1xuICAgICAgICB0aGlzLmxhYmVsRWRpdG9yLmNoYW5nZShmdW5jdGlvbigpe1xuICAgICAgICAgICAgX3RoaXMuY3VycmVudC5zZXRMYWJlbCgkKHRoaXMpLnZhbCgpKTtcbiAgICAgICAgfSk7XG4gICAgICAgIGxhYmVsR3JvdXAuYXBwZW5kKHRoaXMubGFiZWxFZGl0b3IpO1xuXG4gICAgICAgIGNvbnN0IHNlbGVjdEdyb3VwPSQoXCI8ZGl2IGNsYXNzPVxcXCJmb3JtLWdyb3VwXFxcIj48bGFiZWw+5oyJ6ZKu6aOO5qC8PC9sYWJlbD48L2Rpdj5cIik7XG4gICAgICAgIHRoaXMuY29sLmFwcGVuZChzZWxlY3RHcm91cCk7XG4gICAgICAgIHRoaXMudHlwZVNlbGVjdD0kKFwiPHNlbGVjdCBjbGFzcz0nZm9ybS1jb250cm9sJz5cIik7XG4gICAgICAgIHNlbGVjdEdyb3VwLmFwcGVuZCh0aGlzLnR5cGVTZWxlY3QpO1xuICAgICAgICB0aGlzLnR5cGVTZWxlY3QuYXBwZW5kKFwiPG9wdGlvbiB2YWx1ZT0nYnRuLWRlZmF1bHQnPum7mOiupDwvb3B0aW9uPlwiKTtcbiAgICAgICAgdGhpcy50eXBlU2VsZWN0LmFwcGVuZChcIjxvcHRpb24gdmFsdWU9J2J0bi1wcmltYXJ5Jz7ln7rmnKw8L29wdGlvbj5cIik7XG4gICAgICAgIHRoaXMudHlwZVNlbGVjdC5hcHBlbmQoXCI8b3B0aW9uIHZhbHVlPSdidG4tc3VjY2Vzcyc+5oiQ5YqfPC9vcHRpb24+XCIpO1xuICAgICAgICB0aGlzLnR5cGVTZWxlY3QuYXBwZW5kKFwiPG9wdGlvbiB2YWx1ZT0nYnRuLWluZm8nPuS/oeaBrzwvb3B0aW9uPlwiKTtcbiAgICAgICAgdGhpcy50eXBlU2VsZWN0LmFwcGVuZChcIjxvcHRpb24gdmFsdWU9J2J0bi13YXJuaW5nJz7orablkYo8L29wdGlvbj5cIik7XG4gICAgICAgIHRoaXMudHlwZVNlbGVjdC5hcHBlbmQoXCI8b3B0aW9uIHZhbHVlPSdidG4tZGFuZ2VyJz7ljbHpmak8L29wdGlvbj5cIik7XG4gICAgICAgIHRoaXMudHlwZVNlbGVjdC5hcHBlbmQoXCI8b3B0aW9uIHZhbHVlPSdidG4tbGluayc+6ZO+5o6lPC9vcHRpb24+XCIpO1xuICAgICAgICB0aGlzLnR5cGVTZWxlY3QuY2hhbmdlKGZ1bmN0aW9uKCl7XG4gICAgICAgICAgICBjb25zdCBzdHlsZT0kKHRoaXMpLmNoaWxkcmVuKFwib3B0aW9uOnNlbGVjdGVkXCIpLnZhbCgpO1xuICAgICAgICAgICAgX3RoaXMuY3VycmVudC5zZXRTdHlsZShzdHlsZSk7XG4gICAgICAgIH0pO1xuXG4gICAgICAgIGNvbnN0IGFsaWduR3JvdXA9JChgPGRpdiBjbGFzcz1cImZvcm0tZ3JvdXBcIj48bGFiZWw+5a+56b2Q5pa55byPPC9sYWJlbD48L2Rpdj5gKTtcbiAgICAgICAgdGhpcy5jb2wuYXBwZW5kKGFsaWduR3JvdXApO1xuICAgICAgICB0aGlzLmFsaWduU2VsZWN0PSQoYDxzZWxlY3QgY2xhc3M9XCJmb3JtLWNvbnRyb2xcIj5cbiAgICAgICAgICAgIDxvcHRpb24gdmFsdWU9XCJsZWZ0XCI+5bem5a+56b2QPC9vcHRpb24+XG4gICAgICAgICAgICA8b3B0aW9uIHZhbHVlPVwicmlnaHRcIj7lj7Plr7npvZA8L29wdGlvbj5cbiAgICAgICAgPC9zZWxlY3Q+YCk7XG4gICAgICAgIGFsaWduR3JvdXAuYXBwZW5kKHRoaXMuYWxpZ25TZWxlY3QpO1xuICAgICAgICB0aGlzLmFsaWduU2VsZWN0LmNoYW5nZShmdW5jdGlvbigpe1xuICAgICAgICAgICAgX3RoaXMuY3VycmVudC5zZXRBbGlnbigkKHRoaXMpLnZhbCgpKTtcbiAgICAgICAgfSk7XG4gICAgfVxuICAgIHJlZnJlc2hWYWx1ZShjdXJyZW50KXtcbiAgICAgICAgdGhpcy5jdXJyZW50PWN1cnJlbnQ7XG4gICAgICAgIHRoaXMubGFiZWxFZGl0b3IudmFsKGN1cnJlbnQubGFiZWwpO1xuICAgICAgICB0aGlzLnR5cGVTZWxlY3QudmFsKGN1cnJlbnQuc3R5bGUpO1xuICAgICAgICBpZihjdXJyZW50LmVkaXRvclR5cGU9PT0ncmVzZXQtYnV0dG9uJyl7XG4gICAgICAgICAgICB0aGlzLmJ1dHRvblR5cGUuaHRtbChcIumHjee9ruaMiemSrlwiKTtcbiAgICAgICAgfWVsc2V7XG4gICAgICAgICAgICB0aGlzLmJ1dHRvblR5cGUuaHRtbChcIuaPkOS6pOaMiemSrlwiKTtcbiAgICAgICAgfVxuICAgIH1cbn0iLCIvKipcbiAqIENyZWF0ZWQgYnkgSmFja3kuR2FvIG9uIDIwMTctMTAtMTYuXG4gKi9cbmltcG9ydCBQcm9wZXJ0eSBmcm9tICcuL1Byb3BlcnR5LmpzJztcbmV4cG9ydCBkZWZhdWx0IGNsYXNzIENoZWNrYm94UHJvcGVydHkgZXh0ZW5kcyBQcm9wZXJ0eXtcbiAgICBjb25zdHJ1Y3Rvcigpe1xuICAgICAgICBzdXBlcigpO1xuICAgICAgICB0aGlzLmluaXQoKTtcbiAgICB9XG4gICAgaW5pdCgpe1xuICAgICAgICB0aGlzLmNvbC5hcHBlbmQodGhpcy5idWlsZEJpbmRQYXJhbWV0ZXIoKSk7XG4gICAgICAgIHRoaXMucG9zaXRpb25MYWJlbEdyb3VwPXRoaXMuYnVpbGRQb3NpdGlvbkxhYmVsR3JvdXAoKTtcbiAgICAgICAgdGhpcy5jb2wuYXBwZW5kKHRoaXMucG9zaXRpb25MYWJlbEdyb3VwKTtcbiAgICAgICAgdGhpcy5jb2wuYXBwZW5kKHRoaXMuYnVpbGRMYWJlbEdyb3VwKCkpO1xuICAgICAgICB0aGlzLmNvbC5hcHBlbmQodGhpcy5idWlsZE9wdGlvbnNJbmxpbmVHcm91cCgpKTtcbiAgICAgICAgdGhpcy5vcHRpb25Gb3JtR3JvdXA9JChcIjxkaXYgY2xhc3M9J2Zvcm0tZ3JvdXAnPlwiKTtcbiAgICAgICAgdGhpcy5jb2wuYXBwZW5kKHRoaXMub3B0aW9uRm9ybUdyb3VwKTtcbiAgICB9XG4gICAgYWRkQ2hlY2tib3hFZGl0b3IoY2hlY2tib3gpe1xuICAgICAgICB2YXIgc2VsZj10aGlzO1xuICAgICAgICB2YXIgaW5wdXRHcm91cD0kKFwiPGRpdiBjbGFzcz0naW5wdXQtZ3JvdXAnPlwiKTtcbiAgICAgICAgdmFyIHRleHQ9JChcIjxpbnB1dCB0eXBlPSd0ZXh0JyBjbGFzcz0nZm9ybS1jb250cm9sJz5cIik7XG4gICAgICAgIGlucHV0R3JvdXAuYXBwZW5kKHRleHQpO1xuICAgICAgICB0ZXh0LmNoYW5nZShmdW5jdGlvbigpe1xuICAgICAgICAgICAgdmFyIHZhbHVlPSQodGhpcykudmFsKCk7XG4gICAgICAgICAgICB2YXIganNvbj17dmFsdWU6dmFsdWUsbGFiZWw6dmFsdWV9O1xuICAgICAgICAgICAgdmFyIGFycmF5PXZhbHVlLnNwbGl0KFwiLFwiKTtcbiAgICAgICAgICAgIGlmKGFycmF5Lmxlbmd0aD09Mil7XG4gICAgICAgICAgICAgICAganNvbi5sYWJlbD1hcnJheVswXTtcbiAgICAgICAgICAgICAgICBqc29uLnZhbHVlPWFycmF5WzFdO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgY2hlY2tib3guc2V0VmFsdWUoanNvbik7XG4gICAgICAgIH0pO1xuICAgICAgICBpZihjaGVja2JveC5sYWJlbD09PWNoZWNrYm94LnZhbHVlKXtcbiAgICAgICAgICAgIHRleHQudmFsKGNoZWNrYm94LmxhYmVsKTtcbiAgICAgICAgfWVsc2V7XG4gICAgICAgICAgICB0ZXh0LnZhbChjaGVja2JveC5sYWJlbCtcIixcIitjaGVja2JveC52YWx1ZSk7XG4gICAgICAgIH1cbiAgICAgICAgdmFyIGFkZG9uPSQoXCI8c3BhbiBjbGFzcz0naW5wdXQtZ3JvdXAtYWRkb24nPlwiKTtcbiAgICAgICAgaW5wdXRHcm91cC5hcHBlbmQoYWRkb24pO1xuICAgICAgICB2YXIgZGVsPSQoXCI8c3BhbiBjbGFzcz0ncGItaWNvbi1kZWxldGUnPjxsaSBjbGFzcz0nZ2x5cGhpY29uIGdseXBoaWNvbi10cmFzaCc+PC9saT48L3NwYW4+XCIpO1xuICAgICAgICBkZWwuY2xpY2soZnVuY3Rpb24oKXtcbiAgICAgICAgICAgIGlmKHNlbGYuY3VycmVudC5vcHRpb25zLmxlbmd0aD09PTEpe1xuICAgICAgICAgICAgICAgIGJvb3Rib3guYWxlcnQoXCLoh7PlsJHopoHkv53nlZnkuIDkuKrpgInpobkhXCIpO1xuICAgICAgICAgICAgICAgIHJldHVybjtcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIHNlbGYuY3VycmVudC5yZW1vdmVPcHRpb24oY2hlY2tib3gpO1xuICAgICAgICAgICAgaW5wdXRHcm91cC5yZW1vdmUoKTtcbiAgICAgICAgfSk7XG4gICAgICAgIGFkZG9uLmFwcGVuZChkZWwpO1xuICAgICAgICB2YXIgYWRkPSQoXCI8c3BhbiBjbGFzcz0ncGItaWNvbi1hZGQnIHN0eWxlPSdtYXJnaW4tbGVmdDogMTBweCc+PGxpIGNsYXNzPSdnbHlwaGljb24gZ2x5cGhpY29uLXBsdXMnPjwvc3Bhbj5cIik7XG4gICAgICAgIGFkZC5jbGljayhmdW5jdGlvbigpe1xuICAgICAgICAgICAgdmFyIG5ld09wdGlvbj1zZWxmLmN1cnJlbnQuYWRkT3B0aW9uKCk7XG4gICAgICAgICAgICBzZWxmLmFkZENoZWNrYm94RWRpdG9yKG5ld09wdGlvbik7XG4gICAgICAgIH0pO1xuICAgICAgICBhZGRvbi5hcHBlbmQoYWRkKTtcbiAgICAgICAgdGhpcy5vcHRpb25Gb3JtR3JvdXAuYXBwZW5kKGlucHV0R3JvdXApO1xuICAgIH1cbiAgICByZWZyZXNoVmFsdWUoY3VycmVudCl7XG4gICAgICAgIHN1cGVyLnJlZnJlc2hWYWx1ZShjdXJyZW50KTtcbiAgICAgICAgdGhpcy5vcHRpb25Gb3JtR3JvdXAuZW1wdHkoKTtcbiAgICAgICAgdGhpcy5vcHRpb25Gb3JtR3JvdXAuYXBwZW5kKCQoXCI8bGFiZWw+6YCJ6aG5KOiLpeaYvuekuuWAvOS4juWunumZheWAvOS4jeWQjO+8jOWImeeUqOKAnCzigJ3liIbpmpTvvIzlpoLigJzmmK8sdHJ1ZeKAneetiSk8L2xhYmVsPlwiKSk7XG4gICAgICAgIHZhciBzZWxmPXRoaXM7XG4gICAgICAgICQuZWFjaCh0aGlzLmN1cnJlbnQub3B0aW9ucyxmdW5jdGlvbihpbmRleCxjaGVja2JveCl7XG4gICAgICAgICAgICBzZWxmLmFkZENoZWNrYm94RWRpdG9yKGNoZWNrYm94KTtcbiAgICAgICAgfSk7XG4gICAgfVxufSIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0yMy5cbiAqL1xuaW1wb3J0IFByb3BlcnR5IGZyb20gJy4vUHJvcGVydHkuanMnO1xuZXhwb3J0IGRlZmF1bHQgY2xhc3MgRGF0ZXRpbWVQcm9wZXJ0eSBleHRlbmRzIFByb3BlcnR5e1xuICAgIGNvbnN0cnVjdG9yKCl7XG4gICAgICAgIHN1cGVyKCk7XG4gICAgICAgIHRoaXMuaW5pdCgpO1xuICAgIH1cbiAgICBpbml0KCl7XG4gICAgICAgIHRoaXMucG9zaXRpb25MYWJlbEdyb3VwPXRoaXMuYnVpbGRQb3NpdGlvbkxhYmVsR3JvdXAoKTtcbiAgICAgICAgdGhpcy5jb2wuYXBwZW5kKHRoaXMucG9zaXRpb25MYWJlbEdyb3VwKTtcbiAgICAgICAgdGhpcy5jb2wuYXBwZW5kKHRoaXMuYnVpbGRCaW5kUGFyYW1ldGVyKCkpO1xuICAgICAgICB0aGlzLmNvbC5hcHBlbmQodGhpcy5idWlsZExhYmVsR3JvdXAoKSk7XG4gICAgICAgIHZhciBmb3JtYXRHcm91cD0kKFwiPGRpdiBjbGFzcz0nZm9ybS1ncm91cCc+PGxhYmVsIGNsYXNzPSdjb250cm9sLWxhYmVsJz7ml6XmnJ/moLzlvI88L2xhYmVsPjwvZGl2PlwiKTtcbiAgICAgICAgdGhpcy5jb2wuYXBwZW5kKGZvcm1hdEdyb3VwKTtcbiAgICAgICAgdGhpcy5mb3JtYXRTZWxlY3Q9JChcIjxzZWxlY3QgY2xhc3M9J2Zvcm0tY29udHJvbCc+XCIpO1xuICAgICAgICB0aGlzLmZvcm1hdFNlbGVjdC5hcHBlbmQoJChcIjxvcHRpb24+eXl5eS1tbS1kZDwvb3B0aW9uPlwiKSk7XG4gICAgICAgIHRoaXMuZm9ybWF0U2VsZWN0LmFwcGVuZCgkKFwiPG9wdGlvbj55eXl5LW1tLWRkIGhoOmlpOnNzPC9vcHRpb24+XCIpKTtcbiAgICAgICAgdmFyIHNlbGY9dGhpcztcbiAgICAgICAgdGhpcy5mb3JtYXRTZWxlY3QuY2hhbmdlKGZ1bmN0aW9uKCl7XG4gICAgICAgICAgICBzZWxmLmN1cnJlbnQuc2V0RGF0ZUZvcm1hdCgkKHRoaXMpLnZhbCgpKTtcbiAgICAgICAgfSk7XG4gICAgICAgIGZvcm1hdEdyb3VwLmFwcGVuZCh0aGlzLmZvcm1hdFNlbGVjdCk7XG4gICAgfVxuICAgIHJlZnJlc2hWYWx1ZShjdXJyZW50KXtcbiAgICAgICAgc3VwZXIucmVmcmVzaFZhbHVlKGN1cnJlbnQpO1xuICAgICAgICB0aGlzLmZvcm1hdFNlbGVjdC52YWwoY3VycmVudC5kYXRlRm9ybWF0KTtcbiAgICB9XG59IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTE1LlxuICovXG5pbXBvcnQgUHJvcGVydHkgZnJvbSAnLi9Qcm9wZXJ0eS5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBHcmlkUHJvcGVydHkgZXh0ZW5kcyBQcm9wZXJ0eXtcbiAgICBjb25zdHJ1Y3Rvcigpe1xuICAgICAgICBzdXBlcigpO1xuICAgICAgICB0aGlzLmluaXQoKTtcbiAgICB9XG4gICAgaW5pdCgpe1xuICAgICAgICB2YXIgc2hvd0JvcmRlckdyb3VwPSQoXCI8ZGl2IGNsYXNzPSdmb3JtLWdyb3VwJz48bGFiZWw+5pi+56S66L6557q/PC9sYWJlbD48L2Rpdj5cIik7XG4gICAgICAgIHRoaXMuY29sLmFwcGVuZChzaG93Qm9yZGVyR3JvdXApO1xuICAgICAgICB2YXIgc2hvd0xpbmVSYWRpb0dyb3VwPSQoXCI8ZGl2IGNsYXNzPSdjaGVja2JveC1pbmxpbmUnPlwiKTtcbiAgICAgICAgc2hvd0JvcmRlckdyb3VwLmFwcGVuZChzaG93TGluZVJhZGlvR3JvdXApO1xuICAgICAgICB2YXIgcmFkaW9OYW1lPVwic2hvd19ncmlkX2xpbmVfcmFkaW9fXCI7XG4gICAgICAgIHRoaXMuc2hvd0JvcmRlclJhZGlvPSQoXCI8c3BhbiBzdHlsZT0nbWFyZ2luLXJpZ2h0OiAxMHB4Jz7mmK88aW5wdXQgdHlwZT0ncmFkaW8nIG5hbWU9J1wiK3JhZGlvTmFtZStcIic+PC9zcGFuPlwiKTtcbiAgICAgICAgc2hvd0JvcmRlckdyb3VwLmFwcGVuZCh0aGlzLnNob3dCb3JkZXJSYWRpbyk7XG4gICAgICAgIHZhciBzZWxmPXRoaXM7XG4gICAgICAgIHRoaXMuc2hvd0JvcmRlclJhZGlvLmNoYW5nZShmdW5jdGlvbigpe1xuICAgICAgICAgICAgdmFyIHZhbHVlPSQodGhpcykuZmluZChcImlucHV0XCIpLnByb3AoXCJjaGVja2VkXCIpO1xuICAgICAgICAgICAgaWYodmFsdWUpe1xuICAgICAgICAgICAgICAgIHNlbGYuY3VycmVudC5zaG93Qm9yZGVyPXRydWU7XG4gICAgICAgICAgICAgICAgc2VsZi5ib3JkZXJQcm9wR3JvdXAuc2hvdygpO1xuICAgICAgICAgICAgICAgIHNlbGYuYm9yZGVyV2lkdGhUZXh0LnZhbChzZWxmLmN1cnJlbnQuYm9yZGVyV2lkdGgpO1xuICAgICAgICAgICAgICAgIHNlbGYuYm9yZGVyQ29sb3JUZXh0LnZhbChzZWxmLmN1cnJlbnQuYm9yZGVyQ29sb3IpO1xuICAgICAgICAgICAgICAgIHNlbGYuY3VycmVudC5zZXRCb3JkZXJXaWR0aChzZWxmLmN1cnJlbnQuYm9yZGVyV2lkdGgpO1xuICAgICAgICAgICAgfVxuICAgICAgICB9KTtcblxuICAgICAgICB0aGlzLmhpZGVCb3JkZXJSYWRpbz0kKFwiPHNwYW4+5ZCmPGlucHV0IHR5cGU9J3JhZGlvJyBuYW1lPSdcIityYWRpb05hbWUrXCInPjwvc3Bhbj5cIik7XG4gICAgICAgIHNob3dCb3JkZXJHcm91cC5hcHBlbmQodGhpcy5oaWRlQm9yZGVyUmFkaW8pO1xuICAgICAgICB0aGlzLmhpZGVCb3JkZXJSYWRpby5jaGFuZ2UoZnVuY3Rpb24oKXtcbiAgICAgICAgICAgIHZhciB2YWx1ZT0kKHRoaXMpLmZpbmQoXCJpbnB1dFwiKS5wcm9wKFwiY2hlY2tlZFwiKTtcbiAgICAgICAgICAgIGlmKHZhbHVlKXtcbiAgICAgICAgICAgICAgICBzZWxmLmN1cnJlbnQuc2hvd0JvcmRlcj1mYWxzZTtcbiAgICAgICAgICAgICAgICBzZWxmLmJvcmRlclByb3BHcm91cC5oaWRlKCk7XG4gICAgICAgICAgICAgICAgc2VsZi5jdXJyZW50LnNldEJvcmRlcldpZHRoKCk7XG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuXG4gICAgICAgIHRoaXMuYm9yZGVyUHJvcEdyb3VwPSQoXCI8ZGl2PlwiKTtcbiAgICAgICAgdGhpcy5jb2wuYXBwZW5kKHRoaXMuYm9yZGVyUHJvcEdyb3VwKTtcbiAgICAgICAgdmFyIGJvcmRlcldpZHRoR3JvdXA9JChcIjxkaXYgY2xhc3M9J2Zvcm0tZ3JvdXAnPjxsYWJlbD7ovrnnur/lrr3luqYo5Y2V5L2NcHgpPC9sYWJlbD48L2Rpdj5cIik7XG4gICAgICAgIHRoaXMuYm9yZGVyV2lkdGhUZXh0PSQoXCI8aW5wdXQgdHlwZT0nbnVtYmVyJyBjbGFzcz0nZm9ybS1jb250cm9sJz5cIik7XG4gICAgICAgIGJvcmRlcldpZHRoR3JvdXAuYXBwZW5kKHRoaXMuYm9yZGVyV2lkdGhUZXh0KTtcbiAgICAgICAgdGhpcy5ib3JkZXJQcm9wR3JvdXAuYXBwZW5kKGJvcmRlcldpZHRoR3JvdXApO1xuICAgICAgICB0aGlzLmJvcmRlcldpZHRoVGV4dC5jaGFuZ2UoZnVuY3Rpb24oKXtcbiAgICAgICAgICAgIHZhciB3aWR0aD0kKHRoaXMpLnZhbCgpO1xuICAgICAgICAgICAgc2VsZi5jdXJyZW50LnNldEJvcmRlcldpZHRoKHdpZHRoKTtcbiAgICAgICAgfSk7XG5cbiAgICAgICAgdmFyIGJvcmRlckNvbG9yR3JvdXA9JChcIjxkaXYgY2xhc3M9J2Zvcm0tZ3JvdXAnPjxsYWJlbD7ovrnnur/popzoibI8L2xhYmVsPjwvZGl2PlwiKVxuICAgICAgICB0aGlzLmJvcmRlclByb3BHcm91cC5hcHBlbmQoYm9yZGVyQ29sb3JHcm91cCk7XG4gICAgICAgIHRoaXMuYm9yZGVyQ29sb3JUZXh0PSQoXCI8aW5wdXQgdHlwZT0nY29sb3InIGNsYXNzPSdmb3JtLWNvbnRyb2wnPlwiKTtcbiAgICAgICAgYm9yZGVyQ29sb3JHcm91cC5hcHBlbmQodGhpcy5ib3JkZXJDb2xvclRleHQpO1xuICAgICAgICB0aGlzLmJvcmRlckNvbG9yVGV4dC5jaGFuZ2UoZnVuY3Rpb24oKXtcbiAgICAgICAgICAgIHZhciBjb2xvcj0kKHRoaXMpLnZhbCgpO1xuICAgICAgICAgICAgc2VsZi5jdXJyZW50LnNldEJvcmRlckNvbG9yKGNvbG9yKTtcbiAgICAgICAgfSk7XG4gICAgICAgIHRoaXMuYm9yZGVyUHJvcEdyb3VwLmhpZGUoKTtcbiAgICB9XG4gICAgcmVmcmVzaFZhbHVlKGN1cnJlbnQpe1xuICAgICAgICB0aGlzLmN1cnJlbnQ9Y3VycmVudDtcbiAgICAgICAgaWYoY3VycmVudC5zaG93Qm9yZGVyKXtcbiAgICAgICAgICAgIHRoaXMuc2hvd0JvcmRlclJhZGlvLmZpbmQoXCJpbnB1dFwiKS5wcm9wKFwiY2hlY2tlZFwiLHRydWUpO1xuICAgICAgICAgICAgdGhpcy5ib3JkZXJQcm9wR3JvdXAuc2hvdygpO1xuICAgICAgICAgICAgdGhpcy5ib3JkZXJXaWR0aFRleHQudmFsKGN1cnJlbnQuYm9yZGVyV2lkdGgpO1xuICAgICAgICAgICAgdGhpcy5ib3JkZXJDb2xvclRleHQudmFsKGN1cnJlbnQuYm9yZGVyQ29sb3IpO1xuICAgICAgICB9ZWxzZXtcbiAgICAgICAgICAgIHRoaXMuaGlkZUJvcmRlclJhZGlvLmZpbmQoXCJpbnB1dFwiKS5wcm9wKFwiY2hlY2tlZFwiLHRydWUpO1xuICAgICAgICAgICAgdGhpcy5ib3JkZXJQcm9wR3JvdXAuaGlkZSgpO1xuICAgICAgICB9XG4gICAgfVxufVxuIiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTEyLlxuICovXG5pbXBvcnQgUHJvcGVydHkgZnJvbSAnLi9Qcm9wZXJ0eS5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBQYWdlUHJvcGVydHkgZXh0ZW5kcyBQcm9wZXJ0eXtcbiAgICBjb25zdHJ1Y3Rvcigpe1xuICAgICAgICBzdXBlcigpO1xuICAgICAgICB0aGlzLmluaXQoKTtcbiAgICB9XG4gICAgaW5pdCgpe1xuICAgICAgICB2YXIgcG9zaXRpb25Hcm91cD0kKFwiPGRpdiBjbGFzcz0nZm9ybS1ncm91cCc+XCIpO1xuICAgICAgICBwb3NpdGlvbkdyb3VwLmFwcGVuZCgkKFwiPGxhYmVsPuafpeivouihqOWNleS9jee9rjwvbGFiZWw+XCIpKTtcbiAgICAgICAgdGhpcy5wb3NpdGlvblNlbGVjdD0kKGA8c2VsZWN0IGNsYXNzPSdmb3JtLWNvbnRyb2wnPlxuICAgICAgICAgICAgPG9wdGlvbiB2YWx1ZT1cInVwXCI+6aKE6KeI5bel5YW35qCP5LmL5LiKPC9vcHRpb24+XG4gICAgICAgICAgICA8b3B0aW9uIHZhbHVlPVwiZG93blwiPumihOiniOW3peWFt+agj+S5i+S4izwvb3B0aW9uPlxuICAgICAgICA8L3NlbGVjdD5gKTtcbiAgICAgICAgcG9zaXRpb25Hcm91cC5hcHBlbmQodGhpcy5wb3NpdGlvblNlbGVjdCk7XG4gICAgICAgIHZhciBzZWxmPXRoaXM7XG4gICAgICAgIHRoaXMucG9zaXRpb25TZWxlY3QuY2hhbmdlKGZ1bmN0aW9uKCl7XG4gICAgICAgICAgICB3aW5kb3cuZm9ybUJ1aWxkZXIuZm9ybVBvc2l0aW9uPSQodGhpcykudmFsKCk7XG4gICAgICAgIH0pO1xuICAgICAgICB0aGlzLmNvbC5hcHBlbmQocG9zaXRpb25Hcm91cCk7XG4gICAgfVxuICAgIHJlZnJlc2hWYWx1ZShjdXJyZW50KXtcbiAgICAgICAgdGhpcy5wb3NpdGlvblNlbGVjdC52YWwod2luZG93LmZvcm1CdWlsZGVyLmZvcm1Qb3NpdGlvbik7XG4gICAgfVxufVxuIiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTEyLlxuICovXG5leHBvcnQgZGVmYXVsdCBjbGFzcyBQcm9wZXJ0eXtcbiAgICBjb25zdHJ1Y3Rvcigpe1xuICAgICAgICB0aGlzLnByb3BlcnR5Q29udGFpbmVyPSQoXCI8ZGl2IGNsYXNzPSdyb3cnPlwiKTtcbiAgICAgICAgdGhpcy5jb2w9JChcIjxkaXYgY2xhc3M9J2NvbC1tZC0xMic+XCIpO1xuICAgICAgICB0aGlzLnByb3BlcnR5Q29udGFpbmVyLmFwcGVuZCh0aGlzLmNvbCk7XG4gICAgfVxuICAgIGJ1aWxkT3B0aW9uc0lubGluZUdyb3VwKCl7XG4gICAgICAgIGNvbnN0IGlubGluZUdyb3VwPSQoXCI8ZGl2IGNsYXNzPSdmb3JtLWdyb3VwJz48bGFiZWwgY2xhc3M9J2NvbnRyb2wtbGFiZWwnPumAiemhueaNouihjOaYvuekujwvbGFiZWw+PC9kaXY+XCIpO1xuICAgICAgICB0aGlzLm9wdGlvbnNJbmxpbmVTZWxlY3Q9JChcIjxzZWxlY3QgY2xhc3M9J2Zvcm0tY29udHJvbCc+XCIpO1xuICAgICAgICB0aGlzLm9wdGlvbnNJbmxpbmVTZWxlY3QuYXBwZW5kKCQoXCI8b3B0aW9uIHZhbHVlPScwJz7mmK88L29wdGlvbj5cIikpO1xuICAgICAgICB0aGlzLm9wdGlvbnNJbmxpbmVTZWxlY3QuYXBwZW5kKCQoXCI8b3B0aW9uIHZhbHVlPScxJz7lkKY8L29wdGlvbj5cIikpO1xuICAgICAgICBpbmxpbmVHcm91cC5hcHBlbmQodGhpcy5vcHRpb25zSW5saW5lU2VsZWN0KTtcbiAgICAgICAgY29uc3Qgc2VsZj10aGlzO1xuICAgICAgICB0aGlzLm9wdGlvbnNJbmxpbmVTZWxlY3QuY2hhbmdlKGZ1bmN0aW9uKCl7XG4gICAgICAgICAgICBsZXQgdmFsdWU9ZmFsc2U7XG4gICAgICAgICAgICBpZigkKHRoaXMpLnZhbCgpPT09XCIxXCIpe1xuICAgICAgICAgICAgICAgIHZhbHVlPXRydWU7XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBzZWxmLmN1cnJlbnQuc2V0T3B0aW9uc0lubGluZSh2YWx1ZSk7XG4gICAgICAgIH0pO1xuICAgICAgICByZXR1cm4gaW5saW5lR3JvdXA7XG4gICAgfVxuICAgIGJ1aWxkQmluZFBhcmFtZXRlcigpe1xuICAgICAgICBjb25zdCBncm91cD0kKFwiPGRpdiBjbGFzcz0nZm9ybS1ncm91cCc+PGxhYmVsPue7keWumueahOafpeivouWPguaVsDwvbGFiZWw+PC9kaXY+XCIpO1xuICAgICAgICB0aGlzLmJpbmRGaWVsZEVkaXRvcj0kKFwiPGlucHV0IHR5cGU9J3RleHQnIGNsYXNzPSdmb3JtLWNvbnRyb2wnPlwiKTtcbiAgICAgICAgZ3JvdXAuYXBwZW5kKHRoaXMuYmluZEZpZWxkRWRpdG9yKTtcbiAgICAgICAgY29uc3Qgc2VsZj10aGlzO1xuICAgICAgICB0aGlzLmJpbmRGaWVsZEVkaXRvci5jaGFuZ2UoZnVuY3Rpb24oKXtcbiAgICAgICAgICAgIGNvbnN0IHZhbHVlPSQodGhpcykudmFsKCk7XG4gICAgICAgICAgICBzZWxmLmN1cnJlbnQuc2V0QmluZFBhcmFtZXRlcih2YWx1ZSk7XG4gICAgICAgIH0pO1xuICAgICAgICByZXR1cm4gZ3JvdXA7XG4gICAgfVxuICAgIGJ1aWxkTGFiZWxHcm91cCgpe1xuICAgICAgICBjb25zdCBsYWJlbEdyb3VwPSQoXCI8ZGl2IGNsYXNzPSdmb3JtLWdyb3VwJz5cIik7XG4gICAgICAgIGNvbnN0IGxhYmVsTGFiZWw9JChcIjxsYWJlbD7moIfpopg8L2xhYmVsPlwiKTtcbiAgICAgICAgbGFiZWxHcm91cC5hcHBlbmQobGFiZWxMYWJlbCk7XG4gICAgICAgIHRoaXMudGV4dExhYmVsPSQoXCI8aW5wdXQgdHlwZT0ndGV4dCcgY2xhc3M9J2Zvcm0tY29udHJvbCc+XCIpO1xuICAgICAgICBjb25zdCBzZWxmPXRoaXM7XG4gICAgICAgIHRoaXMudGV4dExhYmVsLmNoYW5nZShmdW5jdGlvbigpe1xuICAgICAgICAgICAgc2VsZi5jdXJyZW50LnNldExhYmVsKCQodGhpcykudmFsKCkpO1xuICAgICAgICB9KTtcbiAgICAgICAgbGFiZWxHcm91cC5hcHBlbmQodGhpcy50ZXh0TGFiZWwpO1xuICAgICAgICByZXR1cm4gbGFiZWxHcm91cDtcbiAgICB9XG4gICAgYnVpbGRQb3NpdGlvbkxhYmVsR3JvdXAoKXtcbiAgICAgICAgY29uc3QgcG9zaXRpb25MYWJlbEdyb3VwPSQoXCI8ZGl2IGNsYXNzPSdmb3JtLWdyb3VwJz5cIik7XG4gICAgICAgIGNvbnN0IHBvc2l0aW9uTGFiZWw9JChcIjxsYWJlbCBjbGFzcz0nY29udHJvbC1sYWJlbCc+5qCH6aKY5L2N572uPC9sYWJlbD5cIik7XG4gICAgICAgIHBvc2l0aW9uTGFiZWxHcm91cC5hcHBlbmQocG9zaXRpb25MYWJlbCk7XG4gICAgICAgIHRoaXMucG9zaXRpb25MYWJlbFNlbGVjdD0kKFwiPHNlbGVjdCBjbGFzcz0nZm9ybS1jb250cm9sJz5cIik7XG4gICAgICAgIHBvc2l0aW9uTGFiZWxHcm91cC5hcHBlbmQodGhpcy5wb3NpdGlvbkxhYmVsU2VsZWN0KTtcbiAgICAgICAgdGhpcy5wb3NpdGlvbkxhYmVsU2VsZWN0LmFwcGVuZChcIjxvcHRpb24gdmFsdWU9J3RvcCcgc2VsZWN0ZWQ+5LiK6L65PC9vcHRpb24+XCIpO1xuICAgICAgICB0aGlzLnBvc2l0aW9uTGFiZWxTZWxlY3QuYXBwZW5kKFwiPG9wdGlvbiB2YWx1ZT0nbGVmdCc+5bem6L65PC9vcHRpb24+XCIpO1xuICAgICAgICBjb25zdCBzZWxmPXRoaXM7XG4gICAgICAgIHRoaXMucG9zaXRpb25MYWJlbFNlbGVjdC5jaGFuZ2UoZnVuY3Rpb24oKXtcbiAgICAgICAgICAgIHNlbGYuY3VycmVudC5zZXRMYWJlbFBvc2l0aW9uKCQodGhpcykudmFsKCkpO1xuICAgICAgICB9KTtcbiAgICAgICAgcmV0dXJuIHBvc2l0aW9uTGFiZWxHcm91cDtcbiAgICB9XG5cbiAgICByZWZyZXNoVmFsdWUoaW5zdGFuY2Upe1xuICAgICAgICB0aGlzLmN1cnJlbnQ9aW5zdGFuY2U7XG4gICAgICAgIGlmKHRoaXMub3B0aW9uc0lubGluZVNlbGVjdCl7XG4gICAgICAgICAgICBpZihpbnN0YW5jZS5vcHRpb25zSW5saW5lKXtcbiAgICAgICAgICAgICAgICB0aGlzLm9wdGlvbnNJbmxpbmVTZWxlY3QudmFsKFwiMVwiKTtcbiAgICAgICAgICAgIH1lbHNle1xuICAgICAgICAgICAgICAgIHRoaXMub3B0aW9uc0lubGluZVNlbGVjdC52YWwoXCIwXCIpO1xuICAgICAgICAgICAgfVxuICAgICAgICB9XG4gICAgICAgIHRoaXMucG9zaXRpb25MYWJlbFNlbGVjdC52YWwoaW5zdGFuY2UubGFiZWxQb3NpdGlvbik7XG4gICAgICAgIHRoaXMudGV4dExhYmVsLnZhbChpbnN0YW5jZS5sYWJlbCk7XG4gICAgICAgIHRoaXMuYmluZEZpZWxkRWRpdG9yLnZhbChpbnN0YW5jZS5iaW5kUGFyYW1ldGVyKTtcbiAgICB9XG59IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTEwLTE2LlxuICovXG5pbXBvcnQgUHJvcGVydHkgZnJvbSAnLi9Qcm9wZXJ0eS5qcyc7XG5leHBvcnQgZGVmYXVsdCBjbGFzcyBSYWRpb1Byb3BlcnR5IGV4dGVuZHMgUHJvcGVydHl7XG4gICAgY29uc3RydWN0b3IoKXtcbiAgICAgICAgc3VwZXIoKTtcbiAgICAgICAgdGhpcy5pbml0KCk7XG4gICAgfVxuICAgIGluaXQoKXtcbiAgICAgICAgdGhpcy5jb2wuYXBwZW5kKHRoaXMuYnVpbGRCaW5kUGFyYW1ldGVyKCkpO1xuICAgICAgICB0aGlzLnBvc2l0aW9uTGFiZWxHcm91cD10aGlzLmJ1aWxkUG9zaXRpb25MYWJlbEdyb3VwKCk7XG4gICAgICAgIHRoaXMuY29sLmFwcGVuZCh0aGlzLnBvc2l0aW9uTGFiZWxHcm91cCk7XG4gICAgICAgIHRoaXMuY29sLmFwcGVuZCh0aGlzLmJ1aWxkTGFiZWxHcm91cCgpKTtcbiAgICAgICAgdGhpcy5jb2wuYXBwZW5kKHRoaXMuYnVpbGRPcHRpb25zSW5saW5lR3JvdXAoKSk7XG4gICAgICAgIHRoaXMub3B0aW9uRm9ybUdyb3VwPSQoXCI8ZGl2IGNsYXNzPSdmb3JtLWdyb3VwJz5cIik7XG4gICAgICAgIHRoaXMuY29sLmFwcGVuZCh0aGlzLm9wdGlvbkZvcm1Hcm91cCk7XG4gICAgfVxuICAgIGFkZFJhZGlvRWRpdG9yKHJhZGlvKXtcbiAgICAgICAgdmFyIHNlbGY9dGhpcztcbiAgICAgICAgdmFyIGlucHV0R3JvdXA9JChcIjxkaXYgY2xhc3M9J2lucHV0LWdyb3VwJz5cIik7XG4gICAgICAgIHZhciB0ZXh0PSQoXCI8aW5wdXQgdHlwZT0ndGV4dCcgY2xhc3M9J2Zvcm0tY29udHJvbCc+XCIpO1xuICAgICAgICBpbnB1dEdyb3VwLmFwcGVuZCh0ZXh0KTtcbiAgICAgICAgdGV4dC5jaGFuZ2UoZnVuY3Rpb24oKXtcbiAgICAgICAgICAgIHZhciB2YWx1ZT0kKHRoaXMpLnZhbCgpO1xuICAgICAgICAgICAgdmFyIGpzb249e3ZhbHVlOnZhbHVlLGxhYmVsOnZhbHVlfTtcbiAgICAgICAgICAgIHZhciBhcnJheT12YWx1ZS5zcGxpdChcIixcIik7XG4gICAgICAgICAgICBpZihhcnJheS5sZW5ndGg9PTIpe1xuICAgICAgICAgICAgICAgIGpzb24ubGFiZWw9YXJyYXlbMF07XG4gICAgICAgICAgICAgICAganNvbi52YWx1ZT1hcnJheVsxXTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIHJhZGlvLnNldFZhbHVlKGpzb24pO1xuICAgICAgICB9KTtcbiAgICAgICAgaWYocmFkaW8ubGFiZWw9PT1yYWRpby52YWx1ZSl7XG4gICAgICAgICAgICB0ZXh0LnZhbChyYWRpby5sYWJlbCk7XG4gICAgICAgIH1lbHNle1xuICAgICAgICAgICAgdGV4dC52YWwocmFkaW8ubGFiZWwrXCIsXCIrcmFkaW8udmFsdWUpO1xuICAgICAgICB9XG4gICAgICAgIHZhciBhZGRvbj0kKFwiPHNwYW4gY2xhc3M9J2lucHV0LWdyb3VwLWFkZG9uJz5cIik7XG4gICAgICAgIGlucHV0R3JvdXAuYXBwZW5kKGFkZG9uKTtcbiAgICAgICAgdmFyIGRlbD0kKFwiPHNwYW4gY2xhc3M9J3BiLWljb24tZGVsZXRlJz48bGkgY2xhc3M9J2dseXBoaWNvbiBnbHlwaGljb24tdHJhc2gnPjwvbGk+PC9zcGFuPlwiKTtcbiAgICAgICAgZGVsLmNsaWNrKGZ1bmN0aW9uKCl7XG4gICAgICAgICAgICBpZihzZWxmLmN1cnJlbnQub3B0aW9ucy5sZW5ndGg9PT0xKXtcbiAgICAgICAgICAgICAgICBib290Ym94LmFsZXJ0KFwi6Iez5bCR6KaB5L+d55WZ5LiA5Liq6YCJ6aG5IVwiKTtcbiAgICAgICAgICAgICAgICByZXR1cm47XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBzZWxmLmN1cnJlbnQucmVtb3ZlT3B0aW9uKHJhZGlvKTtcbiAgICAgICAgICAgIGlucHV0R3JvdXAucmVtb3ZlKCk7XG4gICAgICAgIH0pO1xuICAgICAgICBhZGRvbi5hcHBlbmQoZGVsKTtcbiAgICAgICAgdmFyIGFkZD0kKFwiPHNwYW4gY2xhc3M9J3BiLWljb24tYWRkJyBzdHlsZT0nbWFyZ2luLWxlZnQ6IDEwcHgnPjxsaSBjbGFzcz0nZ2x5cGhpY29uIGdseXBoaWNvbi1wbHVzJz48L3NwYW4+XCIpO1xuICAgICAgICBhZGQuY2xpY2soZnVuY3Rpb24oKXtcbiAgICAgICAgICAgIHZhciBuZXdPcHRpb249c2VsZi5jdXJyZW50LmFkZE9wdGlvbigpO1xuICAgICAgICAgICAgc2VsZi5hZGRSYWRpb0VkaXRvcihuZXdPcHRpb24pO1xuICAgICAgICB9KTtcbiAgICAgICAgYWRkb24uYXBwZW5kKGFkZCk7XG4gICAgICAgIHRoaXMub3B0aW9uRm9ybUdyb3VwLmFwcGVuZChpbnB1dEdyb3VwKTtcbiAgICB9XG4gICAgcmVmcmVzaFZhbHVlKGN1cnJlbnQpe1xuICAgICAgICBzdXBlci5yZWZyZXNoVmFsdWUoY3VycmVudCk7XG4gICAgICAgIHRoaXMub3B0aW9uRm9ybUdyb3VwLmVtcHR5KCk7XG4gICAgICAgIHRoaXMub3B0aW9uRm9ybUdyb3VwLmFwcGVuZCgkKFwiPGxhYmVsPumAiemhuSjoi6XmmL7npLrlgLzkuI7lrp7pmYXlgLzkuI3lkIzvvIzliJnnlKjigJws4oCd5YiG6ZqU77yM5aaC4oCc5pivLHRydWXigJ3nrYkpPC9sYWJlbD5cIikpO1xuICAgICAgICB2YXIgc2VsZj10aGlzO1xuICAgICAgICAkLmVhY2godGhpcy5jdXJyZW50Lm9wdGlvbnMsZnVuY3Rpb24oaW5kZXgsY2hlY2tib3gpe1xuICAgICAgICAgICAgc2VsZi5hZGRSYWRpb0VkaXRvcihjaGVja2JveCk7XG4gICAgICAgIH0pO1xuICAgIH1cbn0iLCIvKipcbiAqIENyZWF0ZWQgYnkgSmFja3kuR2FvIG9uIDIwMTctMTAtMjAuXG4gKi9cbmltcG9ydCBQcm9wZXJ0eSBmcm9tICcuL1Byb3BlcnR5LmpzJztcbmV4cG9ydCBkZWZhdWx0IGNsYXNzIFNlbGVjdFByb3BlcnR5IGV4dGVuZHMgUHJvcGVydHl7XG4gICAgY29uc3RydWN0b3IocmVwb3J0KXtcbiAgICAgICAgc3VwZXIoKTtcbiAgICAgICAgdGhpcy5jb2wuYXBwZW5kKHRoaXMuYnVpbGRCaW5kUGFyYW1ldGVyKCkpO1xuICAgICAgICB0aGlzLnBvc2l0aW9uTGFiZWxHcm91cD10aGlzLmJ1aWxkUG9zaXRpb25MYWJlbEdyb3VwKCk7XG4gICAgICAgIHRoaXMuY29sLmFwcGVuZCh0aGlzLnBvc2l0aW9uTGFiZWxHcm91cCk7XG4gICAgICAgIHRoaXMuY29sLmFwcGVuZCh0aGlzLmJ1aWxkTGFiZWxHcm91cCgpKTtcbiAgICAgICAgdGhpcy5vcHRpb25Gb3JtR3JvdXA9JChcIjxkaXYgY2xhc3M9J2Zvcm0tZ3JvdXAnPlwiKTtcbiAgICAgICAgdGhpcy5jb2wuYXBwZW5kKHRoaXMub3B0aW9uRm9ybUdyb3VwKTtcbiAgICB9XG4gICAgcmVmcmVzaFZhbHVlKGVkaXRvcil7XG4gICAgICAgIHN1cGVyLnJlZnJlc2hWYWx1ZShlZGl0b3IpO1xuICAgICAgICB0aGlzLm9wdGlvbkZvcm1Hcm91cC5lbXB0eSgpO1xuICAgICAgICBjb25zdCBncm91cD0kKGA8ZGl2IGNsYXNzPVwiZm9ybS1ncm91cFwiPjxsYWJlbD7mlbDmja7mnaXmupA8L2xhYmVsPjwvZGl2PmApO1xuICAgICAgICBjb25zdCBkYXRhc291cmNlU2VsZWN0PSQoYDxzZWxlY3QgY2xhc3M9XCJmb3JtLWNvbnRyb2xcIj5cbiAgICAgICAgICAgIDxvcHRpb24gdmFsdWU9XCJkYXRhc2V0XCI+5pWw5o2u6ZuGPC9vcHRpb24+XG4gICAgICAgICAgICA8b3B0aW9uIHZhbHVlPVwic2ltcGxlXCI+5Zu65a6a5YC8PC9vcHRpb24+XG4gICAgICAgIDwvc2VsZWN0PmApO1xuICAgICAgICBncm91cC5hcHBlbmQoZGF0YXNvdXJjZVNlbGVjdCk7XG4gICAgICAgIHRoaXMub3B0aW9uRm9ybUdyb3VwLmFwcGVuZChncm91cCk7XG4gICAgICAgIHRoaXMuc2ltcGxlT3B0aW9uR3JvdXA9JChgPGRpdiBjbGFzcz1cImZvcm0tZ3JvdXBcIj48L2Rpdj5gKTtcbiAgICAgICAgdGhpcy5vcHRpb25Gb3JtR3JvdXAuYXBwZW5kKHRoaXMuc2ltcGxlT3B0aW9uR3JvdXApO1xuICAgICAgICB0aGlzLmRhdGFzZXRHcm91cD0kKGA8ZGl2IGNsYXNzPVwiZm9ybS1ncm91cFwiPjwvZGl2PmApO1xuICAgICAgICB0aGlzLm9wdGlvbkZvcm1Hcm91cC5hcHBlbmQodGhpcy5kYXRhc2V0R3JvdXApO1xuICAgICAgICBjb25zdCBfdGhpcz10aGlzO1xuICAgICAgICBkYXRhc291cmNlU2VsZWN0LmNoYW5nZShmdW5jdGlvbigpe1xuICAgICAgICAgICAgaWYoJCh0aGlzKS52YWwoKT09PSdkYXRhc2V0Jyl7XG4gICAgICAgICAgICAgICAgZWRpdG9yLnVzZURhdGFzZXQ9dHJ1ZTtcbiAgICAgICAgICAgICAgICBfdGhpcy5kYXRhc2V0R3JvdXAuc2hvdygpO1xuICAgICAgICAgICAgICAgIF90aGlzLnNpbXBsZU9wdGlvbkdyb3VwLmhpZGUoKTtcbiAgICAgICAgICAgIH1lbHNle1xuICAgICAgICAgICAgICAgIGVkaXRvci51c2VEYXRhc2V0PWZhbHNlO1xuICAgICAgICAgICAgICAgIF90aGlzLmRhdGFzZXRHcm91cC5oaWRlKCk7XG4gICAgICAgICAgICAgICAgX3RoaXMuc2ltcGxlT3B0aW9uR3JvdXAuc2hvdygpO1xuICAgICAgICAgICAgfVxuICAgICAgICB9KTtcbiAgICAgICAgY29uc3QgZGF0YXNldEdyb3VwPSQoYDxkaXYgY2xhc3M9XCJmb3JtLWdyb3VwXCI+PGxhYmVsPuaVsOaNrumbhjwvbGFiZWw+PC9kaXY+YCk7XG4gICAgICAgIHRoaXMuZGF0YXNldEdyb3VwLmFwcGVuZChkYXRhc2V0R3JvdXApO1xuICAgICAgICBjb25zdCBkYXRhc2V0U2VsZWN0PSQoYDxzZWxlY3QgY2xhc3M9XCJmb3JtLWNvbnRyb2xcIj48L3NlbGVjdD5gKTtcbiAgICAgICAgZGF0YXNldEdyb3VwLmFwcGVuZChkYXRhc2V0U2VsZWN0KTtcbiAgICAgICAgbGV0IGRzTmFtZT1udWxsO1xuICAgICAgICBmb3IobGV0IGRhdGFzZXROYW1lIG9mIGZvcm1CdWlsZGVyLmRhdGFzZXRNYXAua2V5cygpKXtcbiAgICAgICAgICAgIGRhdGFzZXRTZWxlY3QuYXBwZW5kKGA8b3B0aW9uPiR7ZGF0YXNldE5hbWV9PC9vcHRpb24+YCk7XG4gICAgICAgICAgICBkc05hbWU9ZGF0YXNldE5hbWU7XG4gICAgICAgIH1cbiAgICAgICAgaWYoZWRpdG9yLmRhdGFzZXQpe1xuICAgICAgICAgICAgZHNOYW1lPWVkaXRvci5kYXRhc2V0O1xuICAgICAgICB9ZWxzZXtcbiAgICAgICAgICAgIGVkaXRvci5kYXRhc2V0PWRzTmFtZTtcbiAgICAgICAgfVxuICAgICAgICBkYXRhc2V0U2VsZWN0LnZhbChkc05hbWUpO1xuICAgICAgICBsZXQgZmllbGRzPWZvcm1CdWlsZGVyLmRhdGFzZXRNYXAuZ2V0KGRzTmFtZSk7XG4gICAgICAgIGlmKCFmaWVsZHMpZmllbGRzPVtdO1xuICAgICAgICBjb25zdCBsYWJlbEdyb3VwPSQoYDxkaXYgY2xhc3M9XCJmb3JtLWdyb3VwXCI+PGxhYmVsPuaYvuekuuWAvOWtl+auteWQjTwvbGFiZWw+PC9kaXY+YCk7XG4gICAgICAgIHRoaXMuZGF0YXNldEdyb3VwLmFwcGVuZChsYWJlbEdyb3VwKTtcbiAgICAgICAgY29uc3QgbGFiZWxTZWxlY3Q9JChgPHNlbGVjdCBjbGFzcz1cImZvcm0tY29udHJvbFwiPjwvc2VsZWN0PmApO1xuICAgICAgICBsYWJlbEdyb3VwLmFwcGVuZChsYWJlbFNlbGVjdCk7XG4gICAgICAgIGNvbnN0IHZhbHVlR3JvdXA9JChgPGRpdiBjbGFzcz1cImZvcm0tZ3JvdXBcIj48bGFiZWw+5a6e6ZmF5YC85a2X5q615ZCNPC9sYWJlbD48L2Rpdj5gKTtcbiAgICAgICAgdGhpcy5kYXRhc2V0R3JvdXAuYXBwZW5kKHZhbHVlR3JvdXApO1xuICAgICAgICBjb25zdCB2YWx1ZVNlbGVjdD0kKGA8c2VsZWN0IGNsYXNzPVwiZm9ybS1jb250cm9sXCI+PC9zZWxlY3Q+YCk7XG4gICAgICAgIGxhYmVsU2VsZWN0LmNoYW5nZShmdW5jdGlvbigpe1xuICAgICAgICAgICAgZWRpdG9yLmxhYmVsRmllbGQ9JCh0aGlzKS52YWwoKTtcbiAgICAgICAgfSk7XG4gICAgICAgIHZhbHVlU2VsZWN0LmNoYW5nZShmdW5jdGlvbigpe1xuICAgICAgICAgICAgZWRpdG9yLnZhbHVlRmllbGQ9JCh0aGlzKS52YWwoKTtcbiAgICAgICAgfSk7XG4gICAgICAgIGxldCB0YXJnZXRGaWVsZD1udWxsO1xuICAgICAgICBmb3IobGV0IGZpZWxkIG9mIGZpZWxkcyl7XG4gICAgICAgICAgICBsYWJlbFNlbGVjdC5hcHBlbmQoYDxvcHRpb24+JHtmaWVsZC5uYW1lfTwvb3B0aW9uPmApO1xuICAgICAgICAgICAgdmFsdWVTZWxlY3QuYXBwZW5kKGA8b3B0aW9uPiR7ZmllbGQubmFtZX08L29wdGlvbj5gKTtcbiAgICAgICAgICAgIHRhcmdldEZpZWxkPWZpZWxkLm5hbWU7XG4gICAgICAgIH1cbiAgICAgICAgZGF0YXNldFNlbGVjdC5jaGFuZ2UoZnVuY3Rpb24gKCkge1xuICAgICAgICAgICAgY29uc3QgZHNOYW1lPSQodGhpcykudmFsKCk7XG4gICAgICAgICAgICBpZighZHNOYW1lKXtcbiAgICAgICAgICAgICAgICByZXR1cm47XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBlZGl0b3IuZGF0YXNldD1kc05hbWU7XG4gICAgICAgICAgICBsYWJlbFNlbGVjdC5lbXB0eSgpO1xuICAgICAgICAgICAgdmFsdWVTZWxlY3QuZW1wdHkoKTtcbiAgICAgICAgICAgIGZpZWxkcz1mb3JtQnVpbGRlci5kYXRhc2V0TWFwLmdldChkc05hbWUpO1xuICAgICAgICAgICAgaWYoIWZpZWxkcylmaWVsZHM9W107XG4gICAgICAgICAgICBmb3IobGV0IGZpZWxkIG9mIGZpZWxkcyl7XG4gICAgICAgICAgICAgICAgbGFiZWxTZWxlY3QuYXBwZW5kKGA8b3B0aW9uPiR7ZmllbGQubmFtZX08L29wdGlvbj5gKTtcbiAgICAgICAgICAgICAgICB2YWx1ZVNlbGVjdC5hcHBlbmQoYDxvcHRpb24+JHtmaWVsZC5uYW1lfTwvb3B0aW9uPmApO1xuICAgICAgICAgICAgICAgIHRhcmdldEZpZWxkPWZpZWxkLm5hbWU7XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBlZGl0b3IubGFiZWxGaWVsZD10YXJnZXRGaWVsZDtcbiAgICAgICAgICAgIGVkaXRvci52YWx1ZUZpZWxkPXRhcmdldEZpZWxkO1xuICAgICAgICAgICAgbGFiZWxTZWxlY3QudmFsKHRhcmdldEZpZWxkKTtcbiAgICAgICAgICAgIHZhbHVlU2VsZWN0LnZhbCh0YXJnZXRGaWVsZCk7XG4gICAgICAgIH0pO1xuICAgICAgICBpZihlZGl0b3IubGFiZWxGaWVsZCl7XG4gICAgICAgICAgICB0YXJnZXRGaWVsZD1lZGl0b3IubGFiZWxGaWVsZDtcbiAgICAgICAgfWVsc2V7XG4gICAgICAgICAgICBlZGl0b3IubGFiZWxGaWVsZD10YXJnZXRGaWVsZDtcbiAgICAgICAgfVxuICAgICAgICBsYWJlbFNlbGVjdC52YWwodGFyZ2V0RmllbGQpO1xuICAgICAgICBpZihlZGl0b3IudmFsdWVGaWVsZCl7XG4gICAgICAgICAgICB0YXJnZXRGaWVsZD1lZGl0b3IudmFsdWVGaWVsZDtcbiAgICAgICAgfWVsc2V7XG4gICAgICAgICAgICBlZGl0b3IudmFsdWVGaWVsZD10YXJnZXRGaWVsZDtcbiAgICAgICAgfVxuICAgICAgICB2YWx1ZVNlbGVjdC52YWwodGFyZ2V0RmllbGQpO1xuICAgICAgICB2YWx1ZUdyb3VwLmFwcGVuZCh2YWx1ZVNlbGVjdCk7XG4gICAgICAgIGlmKGVkaXRvci51c2VEYXRhc2V0KXtcbiAgICAgICAgICAgIGRhdGFzb3VyY2VTZWxlY3QudmFsKCdkYXRhc2V0Jyk7XG4gICAgICAgICAgICB0aGlzLmRhdGFzZXRHcm91cC5zaG93KCk7XG4gICAgICAgICAgICB0aGlzLnNpbXBsZU9wdGlvbkdyb3VwLmhpZGUoKTtcbiAgICAgICAgfWVsc2V7XG4gICAgICAgICAgICB0aGlzLmRhdGFzZXRHcm91cC5oaWRlKCk7XG4gICAgICAgICAgICB0aGlzLnNpbXBsZU9wdGlvbkdyb3VwLnNob3coKTtcbiAgICAgICAgICAgIGRhdGFzb3VyY2VTZWxlY3QudmFsKCdzaW1wbGUnKTtcbiAgICAgICAgfVxuICAgICAgICB0aGlzLnNpbXBsZU9wdGlvbkdyb3VwLmFwcGVuZCgkKFwiPGxhYmVsPuWbuuWumuWAvOmAiemhuSjoi6XmmL7npLrlgLzkuI7lrp7pmYXlgLzkuI3lkIzvvIzliJnnlKjigJws4oCd5YiG6ZqU77yM5aaC4oCc5pivLHRydWXigJ3nrYkpPC9sYWJlbD5cIikpO1xuICAgICAgICB2YXIgc2VsZj10aGlzO1xuICAgICAgICAkLmVhY2goZWRpdG9yLm9wdGlvbnMsZnVuY3Rpb24oaW5kZXgsb3B0aW9uKXtcbiAgICAgICAgICAgIHNlbGYuYWRkT3B0aW9uRWRpdG9yKG9wdGlvbik7XG4gICAgICAgIH0pO1xuICAgIH1cbiAgICBhZGRPcHRpb25FZGl0b3Iob3B0aW9uKXtcbiAgICAgICAgdmFyIGlucHV0R3JvdXA9JChcIjxkaXYgY2xhc3M9J2lucHV0LWdyb3VwJz5cIik7XG4gICAgICAgIHZhciBpbnB1dD0kKFwiPGlucHV0IGNsYXNzPSdmb3JtLWNvbnRyb2wnIHR5cGU9J3RleHQnPlwiKTtcblxuICAgICAgICBpZihvcHRpb24ubGFiZWw9PT1vcHRpb24udmFsdWUpe1xuICAgICAgICAgICAgaW5wdXQudmFsKG9wdGlvbi5sYWJlbCk7XG4gICAgICAgIH1lbHNle1xuICAgICAgICAgICAgaW5wdXQudmFsKG9wdGlvbi5sYWJlbCtcIixcIitvcHRpb24udmFsdWUpO1xuICAgICAgICB9XG5cbiAgICAgICAgaW5wdXQuY2hhbmdlKGZ1bmN0aW9uKCl7XG4gICAgICAgICAgICB2YXIgdmFsdWU9JCh0aGlzKS52YWwoKTtcbiAgICAgICAgICAgIHZhciBqc29uPXt2YWx1ZTp2YWx1ZSxsYWJlbDp2YWx1ZX07XG4gICAgICAgICAgICB2YXIgYXJyYXk9dmFsdWUuc3BsaXQoXCIsXCIpO1xuICAgICAgICAgICAgaWYoYXJyYXkubGVuZ3RoPT0yKXtcbiAgICAgICAgICAgICAgICBqc29uLmxhYmVsPWFycmF5WzBdO1xuICAgICAgICAgICAgICAgIGpzb24udmFsdWU9YXJyYXlbMV07XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBvcHRpb24uc2V0VmFsdWUoanNvbik7XG4gICAgICAgIH0pO1xuICAgICAgICBpbnB1dEdyb3VwLmFwcGVuZChpbnB1dCk7XG4gICAgICAgIHZhciBhZGRvbj0kKFwiPHNwYW4gY2xhc3M9J2lucHV0LWdyb3VwLWFkZG9uJz5cIik7XG4gICAgICAgIGlucHV0R3JvdXAuYXBwZW5kKGFkZG9uKTtcbiAgICAgICAgdmFyIHNlbGY9dGhpcztcbiAgICAgICAgdmFyIGRlbD0kKFwiPHNwYW4gY2xhc3M9J3BiLWljb24tZGVsZXRlJz48bGkgY2xhc3M9J2dseXBoaWNvbiBnbHlwaGljb24tdHJhc2gnPjwvbGk+PC9zcGFuPlwiKTtcbiAgICAgICAgZGVsLmNsaWNrKGZ1bmN0aW9uKCl7XG4gICAgICAgICAgICBpZihzZWxmLmN1cnJlbnQub3B0aW9ucy5sZW5ndGg9PT0xKXtcbiAgICAgICAgICAgICAgICBib290Ym94LmFsZXJ0KFwi6Iez5bCR6KaB5L+d55WZ5LiA5Liq5YiX6KGo6YCJ6aG5IVwiKTtcbiAgICAgICAgICAgICAgICByZXR1cm47XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBzZWxmLmN1cnJlbnQucmVtb3ZlT3B0aW9uKG9wdGlvbik7XG4gICAgICAgICAgICBpbnB1dEdyb3VwLnJlbW92ZSgpO1xuICAgICAgICB9KTtcbiAgICAgICAgYWRkb24uYXBwZW5kKGRlbCk7XG4gICAgICAgIHZhciBhZGQ9JChcIjxzcGFuIGNsYXNzPSdwYi1pY29uLWFkZCcgc3R5bGU9J21hcmdpbi1sZWZ0OiAxMHB4Jz48bGkgY2xhc3M9J2dseXBoaWNvbiBnbHlwaGljb24tcGx1cyc+PC9zcGFuPlwiKTtcbiAgICAgICAgYWRkLmNsaWNrKGZ1bmN0aW9uKCl7XG4gICAgICAgICAgICB2YXIgbmV3T3B0aW9uPXNlbGYuY3VycmVudC5hZGRPcHRpb24oKTtcbiAgICAgICAgICAgIHNlbGYuYWRkT3B0aW9uRWRpdG9yKG5ld09wdGlvbik7XG4gICAgICAgIH0pO1xuICAgICAgICBhZGRvbi5hcHBlbmQoYWRkKTtcbiAgICAgICAgdGhpcy5zaW1wbGVPcHRpb25Hcm91cC5hcHBlbmQoaW5wdXRHcm91cCk7XG4gICAgfVxufSIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0xMC0xNi5cbiAqL1xuaW1wb3J0IFByb3BlcnR5IGZyb20gJy4vUHJvcGVydHkuanMnO1xuZXhwb3J0IGRlZmF1bHQgY2xhc3MgVGV4dFByb3BlcnR5IGV4dGVuZHMgUHJvcGVydHl7XG4gICAgY29uc3RydWN0b3IocmVwb3J0KXtcbiAgICAgICAgc3VwZXIoKTtcbiAgICAgICAgdGhpcy5pbml0KHJlcG9ydCk7XG4gICAgfVxuICAgIGluaXQocmVwb3J0KXtcbiAgICAgICAgdGhpcy5jb2wuYXBwZW5kKHRoaXMuYnVpbGRCaW5kUGFyYW1ldGVyKCkpO1xuICAgICAgICB0aGlzLnBvc2l0aW9uTGFiZWxHcm91cD10aGlzLmJ1aWxkUG9zaXRpb25MYWJlbEdyb3VwKCk7XG4gICAgICAgIHRoaXMuY29sLmFwcGVuZCh0aGlzLnBvc2l0aW9uTGFiZWxHcm91cCk7XG4gICAgICAgIHRoaXMuY29sLmFwcGVuZCh0aGlzLmJ1aWxkTGFiZWxHcm91cCgpKTtcbiAgICB9XG4gICAgcmVmcmVzaFZhbHVlKGN1cnJlbnQpe1xuICAgICAgICBzdXBlci5yZWZyZXNoVmFsdWUoY3VycmVudCk7XG4gICAgICAgIGlmKHRoaXMudHlwZVNlbGVjdCl7XG4gICAgICAgICAgICB0aGlzLnR5cGVTZWxlY3QudmFsKGN1cnJlbnQuZWRpdG9yVHlwZSk7XG4gICAgICAgIH1cbiAgICB9XG59XG4iXSwic291cmNlUm9vdCI6IiJ9