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
/******/ 	return __webpack_require__(__webpack_require__.s = "./src/preview.js");
/******/ })
/************************************************************************/
/******/ ({

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

/***/ "./node_modules/undo-manager/lib/undomanager.js":
/*!******************************************************!*\
  !*** ./node_modules/undo-manager/lib/undomanager.js ***!
  \******************************************************/
/*! no static exports found */
/***/ (function(module, exports, __webpack_require__) {

var __WEBPACK_AMD_DEFINE_RESULT__;/*
Simple Javascript undo and redo.
https://github.com/ArthurClemens/Javascript-Undo-Manager
*/

;(function() {

	'use strict';

    function removeFromTo(array, from, to) {
        array.splice(from,
            !to ||
            1 + to - from + (!(to < 0 ^ from >= 0) && (to < 0 || -1) * array.length));
        return array.length;
    }

    var UndoManager = function() {

        var commands = [],
            index = -1,
            limit = 0,
            isExecuting = false,
            callback,
            
            // functions
            execute;

        execute = function(command, action) {
            if (!command || typeof command[action] !== "function") {
                return this;
            }
            isExecuting = true;

            command[action]();

            isExecuting = false;
            return this;
        };

        return {

            /*
            Add a command to the queue.
            */
            add: function (command) {
                if (isExecuting) {
                    return this;
                }
                // if we are here after having called undo,
                // invalidate items higher on the stack
                commands.splice(index + 1, commands.length - index);

                commands.push(command);
                
                // if limit is set, remove items from the start
                if (limit && commands.length > limit) {
                    removeFromTo(commands, 0, -(limit+1));
                }
                
                // set the current index to the end
                index = commands.length - 1;
                if (callback) {
                    callback();
                }
                return this;
            },

            /*
            Pass a function to be called on undo and redo actions.
            */
            setCallback: function (callbackFunc) {
                callback = callbackFunc;
            },

            /*
            Perform undo: call the undo function at the current index and decrease the index by 1.
            */
            undo: function () {
                var command = commands[index];
                if (!command) {
                    return this;
                }
                execute(command, "undo");
                index -= 1;
                if (callback) {
                    callback();
                }
                return this;
            },

            /*
            Perform redo: call the redo function at the next index and increase the index by 1.
            */
            redo: function () {
                var command = commands[index + 1];
                if (!command) {
                    return this;
                }
                execute(command, "redo");
                index += 1;
                if (callback) {
                    callback();
                }
                return this;
            },

            /*
            Clears the memory, losing all stored states. Reset the index.
            */
            clear: function () {
                var prev_size = commands.length;

                commands = [];
                index = -1;

                if (callback && (prev_size > 0)) {
                    callback();
                }
            },

            hasUndo: function () {
                return index !== -1;
            },

            hasRedo: function () {
                return index < (commands.length - 1);
            },

            getCommands: function () {
                return commands;
            },

            getIndex: function() {
                return index;
            },
            
            setLimit: function (l) {
                limit = l;
            }
        };
    };

	if (true) {
		// AMD. Register as an anonymous module.
		!(__WEBPACK_AMD_DEFINE_RESULT__ = (function() {
			return UndoManager;
		}).call(exports, __webpack_require__, exports, module),
				__WEBPACK_AMD_DEFINE_RESULT__ !== undefined && (module.exports = __WEBPACK_AMD_DEFINE_RESULT__));
	} else {}

}());


/***/ }),

/***/ "./src/MsgBox.js":
/*!***********************!*\
  !*** ./src/MsgBox.js ***!
  \***********************/
/*! exports provided: alert, successAlert, confirm, dialog, showDialog */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "alert", function() { return alert; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "successAlert", function() { return successAlert; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "confirm", function() { return confirm; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "dialog", function() { return dialog; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "showDialog", function() { return showDialog; });
/**
 * Created by jacky on 2016/7/9.
 */
function alert(msg) {
    toastr.options = {
        closeButton: false,
        debug: false,
        progressBar: false,
        positionClass: "toast-top-center",
        onclick: null,
        showDuration: "300",
        hideDuration: "1000",
        timeOut: "5000",
        extendedTimeOut: "1000",
        showEasing: "swing",
        hideEasing: "linear",
        showMethod: "fadeIn",
        hideMethod: "fadeOut"
    };
    toastr.error(msg);
    // const dialog = buildDialog('消息提示',msg);
    // dialog.modal('show');
};

function successAlert(msg) {
    toastr.options = {
        closeButton: false,
        debug: false,
        progressBar: false,
        positionClass: "toast-top-center",
        onclick: null,
        showDuration: "300",
        hideDuration: "1000",
        timeOut: "5000",
        extendedTimeOut: "1000",
        showEasing: "swing",
        hideEasing: "linear",
        showMethod: "fadeIn",
        hideMethod: "fadeOut"
    };
    toastr.success(msg);
};

function confirm(msg, callback) {
    const dialog = buildDialog('确认提示', msg, [{
        name: '确认',
        click: function () {
            callback.call(this);
        }
    }]);
    dialog.modal('show');
};

function dialog(title, content, callback) {
    const dialog = buildDialog(title, content, [{
        name: '确认',
        click: function () {
            callback.call(this);
        }
    }]);
    dialog.modal('show');
};

function showDialog(title, dialogContent, buttons, events, large) {
    const dialog = buildDialog(title, dialogContent, buttons, large);
    dialog.modal('show');
    if (events) {
        for (let event of events) {
            dialog.on(event.name, event.callback);
        }
    }
};

function buildDialog(title, dialogContent, buttons, large) {
    const className = 'modal-dialog' + (large ? ' modal-lg' : '');
    let modal = $(`<div class="modal fade data-report-modal" tabindex="-1" role="dialog" aria-hidden="true"></div>`);
    let dialog = $(`<div class="${className}" style="width: 390px;"></div>`);
    modal.append(dialog);
    let content = $(`<div class="modal-content">
         <div class="modal-header">
            <button type="button" class="close" data-dismiss="modal" aria-hidden="true">
                <i class="report-icon report-icon-huaban16fuben4"></i>
            </button>
            <h4 class="modal-title">
               ${title}
            </h4>
         </div>
         <div class="modal-body">
            ${typeof dialogContent === 'string' ? dialogContent : ''}
         </div>`);
    if (typeof dialogContent === 'object') {
        content.find('.modal-body').append(dialogContent);
    }
    dialog.append(content);
    let footer = $(`<div class="modal-footer"></div>`);
    content.append(footer);
    if (buttons) {
        buttons.forEach((btn, index) => {
            let button = $(`<button type="button" class="el-button el-button--primary el-button--small">${btn.name}</button>`);
            button.click(function (e) {
                btn.click.call(this);
                if (!btn.holdDialog) {
                    modal.modal('hide');
                }
            }.bind(this));
            footer.append(button);
        });
    } else {
        let okBtn = $(`<button type="button" class="el-button el-button--primary el-button--small" data-dismiss="modal">确定</button>`);
        footer.append(okBtn);
    }

    modal.on("show.bs.modal", function () {
        var index = 1050;
        $(document).find('.modal').each(function (i, d) {
            var zIndex = $(d).css('z-index');
            if (zIndex && zIndex !== '' && !isNaN(zIndex)) {
                zIndex = parseInt(zIndex);
                if (zIndex > index) {
                    index = zIndex;
                }
            }
        });
        index++;
        modal.css({
            'z-index': index
        });
    });
    return modal;
};

/***/ }),

/***/ "./src/Utils.js":
/*!**********************!*\
  !*** ./src/Utils.js ***!
  \**********************/
/*! exports provided: showLoading, hideLoading, resetTableData, buildNewCellDef, tableToXml, encode, getParameter, mmToPoint, pointToMM, pointToPixel, pixelToPoint, setDirty, resetDirty, formatDate, buildPageSizeList, undoManager, getUrlParam */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "showLoading", function() { return showLoading; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "hideLoading", function() { return hideLoading; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "resetTableData", function() { return resetTableData; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "buildNewCellDef", function() { return buildNewCellDef; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "tableToXml", function() { return tableToXml; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "encode", function() { return encode; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "getParameter", function() { return getParameter; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "mmToPoint", function() { return mmToPoint; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "pointToMM", function() { return pointToMM; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "pointToPixel", function() { return pointToPixel; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "pixelToPoint", function() { return pixelToPoint; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "setDirty", function() { return setDirty; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "resetDirty", function() { return resetDirty; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "formatDate", function() { return formatDate; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "buildPageSizeList", function() { return buildPageSizeList; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "undoManager", function() { return undoManager; });
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "getUrlParam", function() { return getUrlParam; });
/* harmony import */ var undo_manager__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! undo-manager */ "./node_modules/undo-manager/lib/undomanager.js");
/* harmony import */ var undo_manager__WEBPACK_IMPORTED_MODULE_0___default = /*#__PURE__*/__webpack_require__.n(undo_manager__WEBPACK_IMPORTED_MODULE_0__);
/* harmony import */ var _MsgBox_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./MsgBox.js */ "./src/MsgBox.js");
/**
 * Created by Jacky.gao on 2016/7/27.
 */



const showLoading = () => {
    const url = './icons/loading.gif';
    const h = $(window).height() / 2,
          w = $(window).width() / 2;
    const cover = $(`<div class="ureport-loading-cover" style="position: absolute;left: 0px;top: 0px;width:${w * 2}px;height:${h * 2}px;z-index: 1199;background:rgba(222,222,222,.5)"></div>`);
    $(document.body).append(cover);
    const loading = $(`<div class="ureport-loading" style="text-align: center;position: absolute;z-index: 1120;left: ${w - 35}px;top: ${h - 35}px;"><img src="${url}">
    <div style="margin-top: 5px">数据加载中...</div></div>`);
    $(document.body).append(loading);
};

function hideLoading() {
    $('.ureport-loading-cover').remove();
    $('.ureport-loading').remove();
};

function resetTableData(hot) {
    const countCols = hot.countCols(),
          countRows = hot.countRows(),
          context = hot.context,
          data = [];
    for (let i = 0; i < countRows; i++) {
        let rowData = [];
        for (let j = 0; j < countCols; j++) {
            let td = hot.getCell(i, j);
            if (!td) {
                rowData.push("");
                continue;
            }
            let cellDef = context.getCell(i, j);
            if (cellDef) {
                let valueType = cellDef.value.type,
                    value = cellDef.value;
                if (valueType === 'dataset') {
                    let text = value.datasetName + "." + value.aggregate + "(";
                    let prop = value.property;
                    if (prop.length > 13) {
                        text += prop.substring(0, 10) + '..)';
                    } else {
                        text += prop + ")";
                    }
                    rowData.push(text);
                } else if (valueType === 'expression') {
                    let v = value.value || '';
                    if (v.length > 16) {
                        v = v.substring(0, 13) + '...';
                    }
                    rowData.push(v);
                } else {
                    rowData.push(value.value || "");
                }
            } else {
                rowData.push("");
            }
        }
        data.push(rowData);
    }
    hot.loadData(data);
};

function buildNewCellDef(rowNumber, columnNumber) {
    let cellDef = {
        rowNumber,
        columnNumber,
        expand: 'None',
        cellStyle: {
            fontSize: 9,
            forecolor: '0,0,0',
            fontFamily: '宋体',
            align: 'center',
            valign: 'middle'
        },
        value: {
            type: 'simple',
            value: ''
        }
    };
    return cellDef;
};

function tableToXml(context) {
    const hot = context.hot;
    const countRows = hot.countRows(),
          countCols = hot.countCols();
    let xml = `<?xml version="1.0" encoding="UTF-8"?><ureport>`;
    let rowsXml = '',
        columnXml = '';
    const rowHeaders = context.rowHeaders;
    for (let i = 0; i < countRows; i++) {
        let height = hot.getRowHeight(i) || 16;
        height = pixelToPoint(height);
        let band = null;
        for (let header of rowHeaders) {
            if (header.rowNumber === i) {
                band = header.band;
                break;
            }
        }
        if (band) {
            rowsXml += `<row row-number="${i + 1}" height="${height}" band="${band}"/>`;
        } else {
            rowsXml += `<row row-number="${i + 1}" height="${height}"/>`;
        }
    }
    for (let i = 0; i < countCols; i++) {
        let width = hot.getColWidth(i) || 30;
        width = pixelToPoint(width);
        columnXml += `<column col-number="${i + 1}" width="${width}"/>`;
    }
    let cellXml = '',
        spanData = [];
    for (let i = 0; i < countRows; i++) {
        for (let j = 0; j < countCols; j++) {
            if (spanData.indexOf(i + "," + j) > -1) {
                continue;
            }
            let cellDef = context.getCell(i, j);
            if (!cellDef) {
                continue;
            }
            let cellName = context.getCellName(i, j);
            cellXml += `<cell expand="${cellDef.expand}" name="${cellName}" row="${i + 1}" col="${j + 1}"`;
            if (cellDef.leftParentCellName && cellDef.leftParentCellName !== '') {
                cellXml += ` left-cell="${cellDef.leftParentCellName}"`;
            }
            if (cellDef.topParentCellName && cellDef.topParentCellName !== '') {
                cellXml += ` top-cell="${cellDef.topParentCellName}"`;
            }
            if (cellDef.fillBlankRows) {
                cellXml += ` fill-blank-rows="${cellDef.fillBlankRows}"`;
                if (cellDef.multiple) {
                    cellXml += ` multiple="${cellDef.multiple}"`;
                }
            }

            const span = getSpan(hot, i, j);
            let rowSpan = span.rowspan,
                colSpan = span.colspan;
            let startRow = i,
                endRow = i + rowSpan - 1,
                startCol = j,
                endCol = j + colSpan - 1;
            for (let r = startRow; r <= endRow; r++) {
                for (let c = startCol; c <= endCol; c++) {
                    spanData.push(r + "," + c);
                }
            }
            if (rowSpan > 1) {
                cellXml += ` row-span="${rowSpan}"`;
            }
            if (colSpan > 1) {
                cellXml += ` col-span="${colSpan}"`;
            }

            if (cellDef.linkUrl && cellDef.linkUrl !== '') {
                cellXml += ` link-url="${cellDef.linkUrl}"`;
            }
            if (cellDef.linkTargetWindow && cellDef.linkTargetWindow !== '') {
                cellXml += ` link-target-window="${cellDef.linkTargetWindow}"`;
            }

            cellXml += '>';
            let cellStyle = cellDef.cellStyle;
            cellXml += buildCellStyle(cellStyle);
            if (cellDef.linkParameters && cellDef.linkParameters.length > 0) {
                for (let param of cellDef.linkParameters) {
                    cellXml += `<link-parameter name="${param.name}">`;
                    cellXml += `<value><![CDATA[${param.value}]]></value>`;
                    cellXml += `</link-parameter>`;
                }
            }
            const value = cellDef.value;
            if (value.type === 'dataset') {
                let msg = null;
                if (!value.datasetName) {
                    msg = `${cellName}单元格数据集属性不能为空！`;
                }
                if (!msg && !value.property) {
                    msg = `${cellName}单元格属性不能为空！`;
                }
                if (!msg && !value.aggregate) {
                    msg = `${cellName}单元格聚合方式属性不能为空！`;
                }
                if (msg) {
                    Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_1__["alert"])(msg);
                    throw msg;
                }
                const mappingType = value.mappingType || 'simple';
                cellXml += `<dataset-value dataset-name="${encode(value.datasetName)}" aggregate="${value.aggregate}" property="${value.property}" order="${value.order}" mapping-type="${mappingType}"`;
                if (mappingType === 'dataset') {
                    cellXml += ` mapping-dataset="${value.mappingDataset}" mapping-key-property="${value.mappingKeyProperty}" mapping-value-property="${value.mappingValueProperty}"`;
                }
                cellXml += '>';
                cellXml += buildConditions(value.conditions);
                if (value.aggregate === 'customgroup') {
                    const groupItems = value.groupItems;
                    for (let groupItem of groupItems) {
                        cellXml += `<group-item name="${groupItem.name}">`;
                        for (let condition of groupItem.conditions) {
                            cellXml += `<condition property="${condition.left}" op="${encode(condition.operation || condition.op)}" id="${condition.id}"`;
                            if (condition.join) {
                                cellXml += ` join="${condition.join}">`;
                            } else {
                                cellXml += `>`;
                            }
                            cellXml += `<value><![CDATA[${condition.right}]]></value>`;
                            cellXml += `</condition>`;
                        }
                        cellXml += '</group-item>';
                    }
                }
                if (mappingType === 'simple') {
                    const mappingItems = value.mappingItems;
                    if (mappingItems && mappingItems.length > 0) {
                        for (let mappingItem of mappingItems) {
                            cellXml += `<mapping-item value="${encode(mappingItem.value)}" label="${encode(mappingItem.label)}"/>`;
                        }
                    }
                }
                cellXml += `</dataset-value>`;
            } else if (value.type === 'expression') {
                if (!value.value || value.value === '') {
                    const msg = `${cellName}单元格表达式不能为空`;
                    Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_1__["alert"])(msg);
                    throw msg;
                }
                cellXml += `<expression-value>`;
                cellXml += `<![CDATA[${value.value}]]>`;
                cellXml += `</expression-value>`;
            } else if (value.type === 'simple') {
                cellXml += `<simple-value>`;
                cellXml += `<![CDATA[${value.value || ''}]]>`;
                cellXml += `</simple-value>`;
            } else if (value.type === 'image') {
                cellXml += `<image-value source="${value.source}"`;
                if (value.width) {
                    cellXml += ` width="${value.width}"`;
                }
                if (value.height) {
                    cellXml += ` height="${value.height}"`;
                }
                cellXml += `>`;
                cellXml += `<text>`;
                cellXml += `<![CDATA[${value.value}]]>`;
                cellXml += `</text>`;
                cellXml += `</image-value>`;
            } else if (value.type === 'zxing') {
                cellXml += `<zxing-value source="${value.source}" category="${value.category}" width="${value.width}" height="${value.height}"`;
                if (value.format) {
                    cellXml += ` format="${value.format}"`;
                }
                cellXml += `>`;
                cellXml += `<text>`;
                cellXml += `<![CDATA[${value.value}]]>`;
                cellXml += `</text>`;
                cellXml += `</zxing-value>`;
            } else if (value.type === 'slash') {
                cellXml += `<slash-value>`;
                const slashes = value.slashes;
                for (let slash of slashes) {
                    cellXml += `<slash text="${slash.text}" x="${slash.x}" y="${slash.y}" degree="${slash.degree}"/>`;
                }
                cellXml += `<base64-data>`;
                cellXml += `<![CDATA[${value.base64Data}]]>`;
                cellXml += `</base64-data>`;
                cellXml += `</slash-value>`;
            } else if (value.type === 'chart') {
                cellXml += `<chart-value>`;
                const chart = value.chart;
                const dataset = chart.dataset;
                cellXml += `<dataset dataset-name="${dataset.datasetName}" type="${dataset.type}"`;
                if (dataset.categoryProperty) {
                    cellXml += ` category-property="${dataset.categoryProperty}"`;
                }
                if (dataset.seriesProperty) {
                    cellXml += ` series-property="${dataset.seriesProperty}"`;
                }
                if (dataset.seriesType) {
                    cellXml += ` series-type="${dataset.seriesType}"`;
                }
                if (dataset.seriesText) {
                    cellXml += ` series-text="${dataset.seriesText}"`;
                }
                if (dataset.valueProperty) {
                    cellXml += ` value-property="${dataset.valueProperty}"`;
                }
                if (dataset.rProperty) {
                    cellXml += ` r-property="${dataset.rProperty}"`;
                }
                if (dataset.xProperty) {
                    cellXml += ` x-property="${dataset.xProperty}"`;
                }
                if (dataset.yProperty) {
                    cellXml += ` y-property="${dataset.yProperty}"`;
                }
                if (dataset.collectType) {
                    cellXml += ` collect-type="${dataset.collectType}"`;
                }
                cellXml += `/>`;
                const xaxes = chart.xaxes;
                if (xaxes) {
                    cellXml += `<xaxes`;
                    if (xaxes.rotation) {
                        cellXml += ` rotation="${xaxes.rotation}"`;
                    }
                    cellXml += `>`;
                    const scaleLabel = xaxes.scaleLabel;
                    if (scaleLabel) {
                        cellXml += `<scale-label display="${scaleLabel.display}"`;
                        if (scaleLabel.labelString) {
                            cellXml += ` label-string="${scaleLabel.labelString}"`;
                        }
                        cellXml += `/>`;
                    }
                    cellXml += `</xaxes>`;
                }
                const yaxes = chart.yaxes;
                if (yaxes) {
                    cellXml += `<yaxes`;
                    if (yaxes.rotation) {
                        cellXml += ` rotation="${yaxes.rotation}"`;
                    }
                    cellXml += `>`;
                    const scaleLabel = yaxes.scaleLabel;
                    if (scaleLabel) {
                        cellXml += `<scale-label display="${scaleLabel.display}"`;
                        if (scaleLabel.labelString) {
                            cellXml += ` label-string="${scaleLabel.labelString}"`;
                        }
                        cellXml += `/>`;
                    }
                    cellXml += `</yaxes>`;
                }
                const options = chart.options;
                if (options) {
                    for (let option of options) {
                        cellXml += `<option type="${option.type}"`;
                        if (option.position) {
                            cellXml += ` position="${option.position}"`;
                        }
                        if (option.display !== undefined && option.display !== null) {
                            cellXml += ` display="${option.display}"`;
                        }
                        if (option.duration) {
                            cellXml += ` duration="${option.duration}"`;
                        }
                        if (option.easing) {
                            cellXml += ` easing="${option.easing}"`;
                        }
                        if (option.text) {
                            cellXml += ` text="${option.text}"`;
                        }
                        cellXml += `/>`;
                    }
                }
                const plugins = chart.plugins || [];
                for (let plugin of plugins) {
                    cellXml += `<plugin name="${plugin.name}" display="${plugin.display}"/>`;
                }
                if (plugins) {}
                cellXml += `</chart-value>`;
            }
            const propertyConditions = cellDef.conditionPropertyItems || [];
            for (let pc of propertyConditions) {
                cellXml += `<condition-property-item name="${pc.name}"`;
                const rowHeight = pc.rowHeight;
                if (rowHeight !== null && rowHeight !== undefined && rowHeight !== -1) {
                    cellXml += ` row-height="${rowHeight}"`;
                }
                const colWidth = pc.colWidth;
                if (colWidth !== null && colWidth !== undefined && colWidth !== -1) {
                    cellXml += ` col-width="${colWidth}"`;
                }
                if (pc.newValue && pc.newValue !== '') {
                    cellXml += ` new-value="${pc.newValue}"`;
                }
                if (pc.linkUrl && pc.linkUrl !== '') {
                    cellXml += ` link-url="${pc.linkUrl}"`;
                    let targetWindow = pc.linkTargetWindow;
                    if (!targetWindow || targetWindow === '') {
                        targetWindow = "_self";
                    }
                    cellXml += ` link-target-window="${pc.linkTargetWindow}"`;
                }
                cellXml += `>`;
                const paging = pc.paging;
                if (paging) {
                    cellXml += `<paging position="${paging.position}" line="${paging.line}"/>`;
                }
                if (pc.linkParameters && pc.linkParameters.length > 0) {
                    for (let param of pc.linkParameters) {
                        cellXml += `<link-parameter name="${param.name}">`;
                        cellXml += `<value><![CDATA[${param.value}]]></value>`;
                        cellXml += `</link-parameter>`;
                    }
                }
                const style = pc.cellStyle;
                if (style) {
                    cellXml += buildCellStyle(style, true);
                }
                cellXml += buildConditions(pc.conditions);
                cellXml += `</condition-property-item>`;
            }
            cellXml += '</cell>';
        }
    }
    xml += cellXml;
    xml += rowsXml;
    xml += columnXml;
    const header = context.reportDef.header;
    if (header && (header.left || header.center || header.right)) {
        xml += '<header ';
        if (header.fontFamily) {
            xml += ` font-family="${header.fontFamily}"`;
        }
        if (header.fontSize) {
            xml += ` font-size="${header.fontSize}"`;
        }
        if (header.forecolor) {
            xml += ` forecolor="${header.forecolor}"`;
        }
        if (header.bold) {
            xml += ` bold="${header.bold}"`;
        }
        if (header.italic) {
            xml += ` italic="${header.italic}"`;
        }
        if (header.underline) {
            xml += ` underline="${header.underline}"`;
        }
        if (header.margin) {
            xml += ` margin="${header.margin}"`;
        }
        xml += '>';
        if (header.left) {
            xml += `<left><![CDATA[${header.left}]]></left>`;
        }
        if (header.center) {
            xml += `<center><![CDATA[${header.center}]]></center>`;
        }
        if (header.right) {
            xml += `<right><![CDATA[${header.right}]]></right>`;
        }
        xml += '</header>';
    }
    const footer = context.reportDef.footer;
    if (footer && (footer.left || footer.center || footer.right)) {
        xml += '<footer ';
        if (footer.fontFamily) {
            xml += ` font-family="${footer.fontFamily}"`;
        }
        if (footer.fontSize) {
            xml += ` font-size="${footer.fontSize}"`;
        }
        if (footer.forecolor) {
            xml += ` forecolor="${footer.forecolor}"`;
        }
        if (footer.bold) {
            xml += ` bold="${footer.bold}"`;
        }
        if (footer.italic) {
            xml += ` italic="${footer.italic}"`;
        }
        if (footer.underline) {
            xml += ` underline="${footer.underline}"`;
        }
        if (footer.margin) {
            xml += ` margin="${footer.margin}"`;
        }
        xml += '>';
        if (footer.left) {
            xml += `<left><![CDATA[${footer.left}]]></left>`;
        }
        if (footer.center) {
            xml += `<center><![CDATA[${footer.center}]]></center>`;
        }
        if (footer.right) {
            xml += `<right><![CDATA[${footer.right}]]></right>`;
        }
        xml += '</footer>';
    }
    let datasourceXml = "";
    const datasources = context.reportDef.datasources;
    for (let datasource of datasources) {
        let ds = `<datasource name="${encode(datasource.name)}" type="${datasource.type}"`;
        let type = datasource.type;
        if (type === 'jdbc') {
            ds += ` username="${encode(datasource.username)}"`;
            ds += ` password="${encode(datasource.password)}"`;
            ds += ` url="${encode(datasource.url)}"`;
            ds += ` driver="${datasource.driver}"`;
            ds += '>';
            for (let dataset of datasource.datasets) {
                ds += `<dataset name="${encode(dataset.name)}" type="sql">`;
                ds += `<sql><![CDATA[${dataset.sql}]]></sql>`;
                for (let field of dataset.fields) {
                    ds += `<field name="${field.name}"/>`;
                }
                for (let parameter of dataset.parameters) {
                    ds += `<parameter name="${encode(parameter.name)}" type="${parameter.type}" default-value="${encode(parameter.defaultValue)}"/>`;
                }
                ds += `</dataset>`;
            }
        } else if (type === 'spring') {
            ds += ` bean="${datasource.beanId}">`;
            for (let dataset of datasource.datasets) {
                ds += `<dataset name="${encode(dataset.name)}" type="bean" method="${dataset.method}" clazz="${dataset.clazz}">`;
                for (let field of dataset.fields) {
                    ds += `<field name="${field.name}"/>`;
                }
                ds += `</dataset>`;
            }
        } else if (type === 'buildin') {
            ds += '>';
            for (let dataset of datasource.datasets) {
                ds += `<dataset name="${encode(dataset.name)}" type="sql">`;
                ds += `<sql><![CDATA[${dataset.sql}]]></sql>`;
                for (let field of dataset.fields) {
                    ds += `<field name="${field.name}"/>`;
                }
                for (let parameter of dataset.parameters) {
                    ds += `<parameter name="${parameter.name}" type="${parameter.type}" default-value="${parameter.defaultValue}"/>`;
                }
                ds += `</dataset>`;
            }
        }
        ds += "</datasource>";
        datasourceXml += ds;
    }
    xml += datasourceXml;
    const paper = context.reportDef.paper;
    let htmlIntervalRefreshValue = 0;
    if (paper.htmlIntervalRefreshValue !== null && paper.htmlIntervalRefreshValue !== undefined) {
        htmlIntervalRefreshValue = paper.htmlIntervalRefreshValue;
    }
    xml += `<paper type="${paper.paperType}" left-margin="${paper.leftMargin}" right-margin="${paper.rightMargin}"
    top-margin="${paper.topMargin}" bottom-margin="${paper.bottomMargin}" paging-mode="${paper.pagingMode}" fixrows="${paper.fixRows}"
    width="${paper.width}" height="${paper.height}" orientation="${paper.orientation}" html-report-align="${paper.htmlReportAlign}" bg-image="${paper.bgImage || ''}" html-interval-refresh-value="${htmlIntervalRefreshValue}" column-enabled="${paper.columnEnabled}"`;
    if (paper.columnEnabled) {
        xml += ` column-count="${paper.columnCount}" column-margin="${paper.columnMargin}"`;
    }
    xml += `></paper>`;
    if (context.reportDef.searchFormXml) {
        xml += context.reportDef.searchFormXml;
    }
    xml += `</ureport>`;
    xml = encodeURIComponent(xml);
    return xml;
};

function getSpan(hot, row, col) {
    const mergeCells = hot.getSettings().mergeCells || [];
    for (let item of mergeCells) {
        if (item.row === row && item.col === col) {
            return item;
        }
    }
    return {
        rowspan: 0,
        colspan: 0
    };
}

function buildConditions(conditions) {
    let cellXml = '';
    if (conditions) {
        const size = conditions.length;
        for (let condition of conditions) {
            if (!condition.type || condition.type === 'property') {
                if (condition.left) {
                    cellXml += `<condition property="${condition.left}" op="${encode(condition.operation)}" id="${condition.id}"`;
                } else {
                    cellXml += `<condition op="${encode(condition.operation)}" id="${condition.id}"`;
                }
                cellXml += ` type="${condition.type}"`;
                if (condition.join && size > 1) {
                    cellXml += ` join="${condition.join}">`;
                } else {
                    cellXml += `>`;
                }
                cellXml += `<value><![CDATA[${condition.right}]]></value>`;
            } else {
                cellXml += `<condition type="${condition.type}" op="${encode(condition.operation)}" id="${condition.id}"`;
                if (condition.join && size > 1) {
                    cellXml += ` join="${condition.join}">`;
                } else {
                    cellXml += `>`;
                }
                cellXml += `<left><![CDATA[${condition.left}]]></left>`;
                cellXml += `<right><![CDATA[${condition.right}]]></right>`;
            }
            cellXml += `</condition>`;
        }
    }
    return cellXml;
};

function buildCellStyle(cellStyle, condition) {
    let cellXml = "<cell-style";
    if (condition) {
        cellXml += ` for-condition="true"`;
    }
    if (cellStyle.fontSize && cellStyle.fontSize !== '') {
        cellXml += ` font-size="${cellStyle.fontSize}"`;
    }
    if (cellStyle.fontSizeScope) {
        cellXml += ` font-size-scope="${cellStyle.fontSizeScope}"`;
    }
    if (cellStyle.forecolor && cellStyle.forecolor !== '') {
        cellXml += ` forecolor="${cellStyle.forecolor}"`;
    }
    if (cellStyle.forecolorScope) {
        cellXml += ` forecolor-scope="${cellStyle.forecolorScope}"`;
    }
    if (cellStyle.fontFamily) {
        if (cellStyle.fontFamily === '0') {
            cellXml += ` font-family=""`;
        } else {
            cellXml += ` font-family="${cellStyle.fontFamily}"`;
        }
    }
    if (cellStyle.fontFamilyScope) {
        cellXml += ` font-family-scope="${cellStyle.fontFamilyScope}"`;
    }
    if (cellStyle.bgcolor && cellStyle.bgcolor !== '') {
        cellXml += ` bgcolor="${cellStyle.bgcolor}"`;
    }
    if (cellStyle.bgcolorScope) {
        cellXml += ` bgcolor-scope="${cellStyle.bgcolorScope}"`;
    }
    if (cellStyle.format && cellStyle.format !== '') {
        cellXml += ` format="${cellStyle.format}"`;
    }
    if (cellStyle.bold !== undefined && cellStyle.bold !== null) {
        cellXml += ` bold="${cellStyle.bold}"`;
    }
    if (cellStyle.boldScope) {
        cellXml += ` bold-scope="${cellStyle.boldScope}"`;
    }
    if (cellStyle.italic !== undefined && cellStyle.italic !== null) {
        cellXml += ` italic="${cellStyle.italic}"`;
    }
    if (cellStyle.italicScope) {
        cellXml += ` italic-scope="${cellStyle.italicScope}"`;
    }
    if (cellStyle.underline !== undefined && cellStyle.underline !== null) {
        cellXml += ` underline="${cellStyle.underline}"`;
    }
    if (cellStyle.underlineScope) {
        cellXml += ` underline-scope="${cellStyle.underlineScope}"`;
    }
    if (cellStyle.wrapCompute !== undefined && cellStyle.wrapCompute !== null) {
        cellXml += ` wrap-compute="${cellStyle.wrapCompute}"`;
    }
    if (cellStyle.align && cellStyle.align !== '') {
        cellXml += ` align="${cellStyle.align}"`;
    }
    if (cellStyle.alignScope) {
        cellXml += ` align-scope="${cellStyle.alignScope}"`;
    }
    if (cellStyle.valign && cellStyle.valign !== '') {
        cellXml += ` valign="${cellStyle.valign}"`;
    }
    if (cellStyle.valignScope) {
        cellXml += ` valign-scope="${cellStyle.valignScope}"`;
    }
    if (cellStyle.lineHeight) {
        cellXml += ` line-height="${cellStyle.lineHeight}"`;
    }
    cellXml += '>';
    let leftBorder = cellStyle.leftBorder;
    if (leftBorder && leftBorder.style !== "none") {
        cellXml += `<left-border width="${leftBorder.width}" style="${leftBorder.style}" color="${leftBorder.color}"/>`;
    }
    let rightBorder = cellStyle.rightBorder;
    if (rightBorder && rightBorder.style !== "none") {
        cellXml += `<right-border width="${rightBorder.width}" style="${rightBorder.style}" color="${rightBorder.color}"/>`;
    }
    let topBorder = cellStyle.topBorder;
    if (topBorder && topBorder.style !== "none") {
        cellXml += `<top-border width="${topBorder.width}" style="${topBorder.style}" color="${topBorder.color}"/>`;
    }
    let bottomBorder = cellStyle.bottomBorder;
    if (bottomBorder && bottomBorder.style !== "none") {
        cellXml += `<bottom-border width="${bottomBorder.width}" style="${bottomBorder.style}" color="${bottomBorder.color}"/>`;
    }
    cellXml += '</cell-style>';
    return cellXml;
};

function encode(text) {
    let result = text.replace(/[<>&"]/g, function (c) {
        return {
            '<': '&lt;',
            '>': '&gt;',
            '&': '&amp;',
            '"': '&quot;'
        }[c];
    });
    return result;
};

function getParameter(name) {
    var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)");
    var r = window.location.search.substr(1).match(reg);
    if (r != null) return r[2];
    return null;
};

function mmToPoint(mm) {
    let value = mm * 2.834646;
    return Math.round(value);
};
function pointToMM(point) {
    let value = point * 0.352778;
    return Math.round(value);
};

function pointToPixel(point) {
    const value = point * 1.33;
    return Math.round(value);
};

function pixelToPoint(pixel) {
    const value = pixel * 0.75;
    return Math.round(value);
};

function setDirty() {
    $('#__save_btn').removeClass('disabled');
};

function resetDirty() {
    $('#__save_btn').addClass('disabled');
};

function formatDate(date, format) {
    if (typeof date === 'number') {
        date = new Date(date);
    }
    if (typeof date === 'string') {
        return date;
    }
    var o = {
        "M+": date.getMonth() + 1,
        "d+": date.getDate(),
        "H+": date.getHours(),
        "m+": date.getMinutes(),
        "s+": date.getSeconds()
    };
    if (/(y+)/.test(format)) format = format.replace(RegExp.$1, (date.getFullYear() + "").substr(4 - RegExp.$1.length));
    for (var k in o) if (new RegExp("(" + k + ")").test(format)) format = format.replace(RegExp.$1, RegExp.$1.length == 1 ? o[k] : ("00" + o[k]).substr(("" + o[k]).length));
    return format;
};

function buildPageSizeList() {
    return {
        A0: {
            width: 841,
            height: 1189
        },
        A1: {
            width: 594,
            height: 841
        },
        A2: {
            width: 420,
            height: 594
        },
        A3: {
            width: 297,
            height: 420
        },
        A4: {
            width: 210,
            height: 297
        },
        A5: {
            width: 148,
            height: 210
        },
        A6: {
            width: 105,
            height: 148
        },
        A7: {
            width: 74,
            height: 105
        },
        A8: {
            width: 52,
            height: 74
        },
        A9: {
            width: 37,
            height: 52
        },
        A10: {
            width: 26,
            height: 37
        },
        B0: {
            width: 1000,
            height: 1414
        },
        B1: {
            width: 707,
            height: 1000
        },
        B2: {
            width: 500,
            height: 707
        },
        B3: {
            width: 353,
            height: 500
        },
        B4: {
            width: 250,
            height: 353
        },
        B5: {
            width: 176,
            height: 250
        },
        B6: {
            width: 125,
            height: 176
        },
        B7: {
            width: 88,
            height: 125
        },
        B8: {
            width: 62,
            height: 88
        },
        B9: {
            width: 44,
            height: 62
        },
        B10: {
            width: 31,
            height: 44
        }
    };
}

const undoManager = new undo_manager__WEBPACK_IMPORTED_MODULE_0___default.a();

const getUrlParam = name => {
    let reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)");
    let r = window.location.search.substr(1).match(reg);
    if (r != null) {
        return unescape(r[2]);
    } else {
        return null;
    }
};

/***/ }),

/***/ "./src/dialog/PDFPrintDialog.js":
/*!**************************************!*\
  !*** ./src/dialog/PDFPrintDialog.js ***!
  \**************************************/
/*! exports provided: default */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony export (binding) */ __webpack_require__.d(__webpack_exports__, "default", function() { return PDFPrintDialog; });
/* harmony import */ var _Utils_js__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../Utils.js */ "./src/Utils.js");
/* harmony import */ var _MsgBox_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../MsgBox.js */ "./src/MsgBox.js");
/**
 * Created by Jacky.Gao on 2017-02-07.
 */



class PDFPrintDialog {
    constructor() {
        const w = $(window).width(),
              h = $(window).height() - 280;
        this.paperSizeList = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["buildPageSizeList"])();
        this.dialog = $(`<div class="modal fade data-report-modal" role="dialog" aria-hidden="true" style="z-index: 10000">
            <div class="modal-dialog modal-lg" style="width: 1030px;">
                <div class="modal-content">
                    <div class="modal-header">
                        <button type="button" class="close pdfclose" data-dismiss="modal" aria-hidden="true">
                            X
                        </button>
                        <h4 class="modal-title">
                            ${window.i18n.pdfPrint.title}
                        </h4>
                    </div>
                    <div class="modal-body" style="padding-top:5px;"></div>
                </div>
            </div>
        </div>`);
        this.body = this.dialog.find('.modal-body');
        this.initBody();
    }
    initBody() {
        const token = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["getUrlParam"])('token');
        const toolbar = $(`<fieldset style="width: 100%;height: 60px;font-size: 12px;border: solid 1px #ddd;border-radius: 5px;padding: 1px 8px;">
        <legend style="font-size: 12px;width: 60px;border-bottom: none;margin-bottom: 0;">${window.i18n.pdfPrint.setup}</legend>
        </fieldset>`);
        this.body.append(toolbar);
        const pageTypeGroup = $(`<div class="form-group" style="display: inline-block"><label>${window.i18n.pdfPrint.paper}</label></div>`);
        toolbar.append(pageTypeGroup);
        this.pageSelect = $(`<select class="form-control" style="display: inline-block;width: 68px;font-size: 12px;padding: 1px;height: 28px;">
            <option>A0</option>
            <option>A1</option>
            <option>A2</option>
            <option>A3</option>
            <option>A4</option>
            <option>A5</option>
            <option>A6</option>
            <option>A7</option>
            <option>A8</option>
            <option>A9</option>
            <option>A10</option>
            <option>B0</option>
            <option>B1</option>
            <option>B2</option>
            <option>B3</option>
            <option>B4</option>
            <option>B5</option>
            <option>B6</option>
            <option>B7</option>
            <option>B8</option>
            <option>B9</option>
            <option>B10</option>
            <option value="CUSTOM">${window.i18n.pdfPrint.custom}</option>
        </select>`);
        pageTypeGroup.append(this.pageSelect);
        const _this = this;
        this.pageSelect.change(function () {
            let value = $(this).val();
            if (value === 'CUSTOM') {
                _this.pageWidthEditor.prop('readonly', false);
                _this.pageHeightEditor.prop('readonly', false);
            } else {
                _this.pageWidthEditor.prop('readonly', true);
                _this.pageHeightEditor.prop('readonly', true);
                let pageSize = _this.paperSizeList[value];
                console.log(pageSize);
                _this.pageWidthEditor.val(pageSize.width);
                _this.pageHeightEditor.val(pageSize.height);
                _this.paper.width = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["mmToPoint"])(pageSize.width);
                _this.paper.height = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["mmToPoint"])(pageSize.height);
            }
            _this.paper.paperType = value;
        });

        const pageWidthGroup = $(`<div class="form-group" style="display: inline-block;margin-left: 6px"><span>${window.i18n.pdfPrint.width}</span></div>`);
        toolbar.append(pageWidthGroup);
        this.pageWidthEditor = $(`<input type="number" class="form-control" readonly style="display: inline-block;width: 40px;font-size: 12px;padding: 1px;height: 28px">`);
        pageWidthGroup.append(this.pageWidthEditor);
        this.pageWidthEditor.change(function () {
            let value = $(this).val();
            if (!value || isNaN(value)) {
                Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_1__["alert"])(`${window.i18n.pdfPrint.numberTip}`);
                return;
            }
            _this.paper.width = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["mmToPoint"])(value);
            _this.context.printLine.refresh();
        });

        const pageHeightGroup = $(`<div class="form-group" style="display: inline-block;margin-left: 6px"><span>${window.i18n.pdfPrint.height}</span></div>`);
        toolbar.append(pageHeightGroup);
        this.pageHeightEditor = $(`<input type="number" class="form-control" readonly style="display: inline-block;width: 40px;font-size: 12px;padding: 1px;height: 28px">`);
        pageHeightGroup.append(this.pageHeightEditor);
        this.pageHeightEditor.change(function () {
            let value = $(this).val();
            if (!value || isNaN(value)) {
                Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_1__["alert"])(`${window.i18n.pdfPrint.numberTip}`);
                return;
            }
            _this.paper.height = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["mmToPoint"])(value);
        });

        const orientationGroup = $(`<div class="form-group" style="display: inline-block;margin-left: 6px"><label>${window.i18n.pdfPrint.orientation}</label></div>`);
        toolbar.append(orientationGroup);
        this.orientationSelect = $(`<select class="form-control" style="display:inline-block;width: 60px;font-size: 12px;padding: 1px;height: 28px">
            <option value="portrait">${window.i18n.pdfPrint.portrait}</option>
            <option value="landscape">${window.i18n.pdfPrint.landscape}</option>
        </select>`);
        orientationGroup.append(this.orientationSelect);
        this.orientationSelect.change(function () {
            let value = $(this).val();
            _this.paper.orientation = value;
        });

        const marginGroup = $(`<div style="display: inline-block"></div>`);
        toolbar.append(marginGroup);

        const leftMarginGroup = $(`<div class="form-group" style="display: inline-block;margin-left:6px"><label>${window.i18n.pdfPrint.leftMargin}</label></div>`);
        marginGroup.append(leftMarginGroup);
        this.leftMarginEditor = $(`<input type="number" class="form-control" style="display: inline-block;width: 40px;font-size: 12px;padding: 1px;height: 28px">`);
        leftMarginGroup.append(this.leftMarginEditor);
        this.leftMarginEditor.change(function () {
            let value = $(this).val();
            if (!value || isNaN(value)) {
                Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_1__["alert"])(`${window.i18n.pdfPrint.numberTip}`);
                return;
            }
            _this.paper.leftMargin = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["mmToPoint"])(value);
            _this.context.printLine.refresh();
        });

        const rightMarginGroup = $(`<div class="form-group" style="display: inline-block;margin-top: 5px;margin-left: 6px""><label>${window.i18n.pdfPrint.rightMargin}</label></div>`);
        marginGroup.append(rightMarginGroup);
        this.rightMarginEditor = $(`<input type="number" class="form-control" style="display: inline-block;width: 40px;font-size: 12px;padding: 1px;height: 28px">`);
        rightMarginGroup.append(this.rightMarginEditor);
        this.rightMarginEditor.change(function () {
            let value = $(this).val();
            if (!value || isNaN(value)) {
                Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_1__["alert"])(`${window.i18n.pdfPrint.numberTip}`);
                return;
            }
            _this.paper.rightMargin = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["mmToPoint"])(value);
            _this.context.printLine.refresh();
        });

        const topMarginGroup = $(`<div class="form-group" style="display: inline-block;margin-left: 6px;"><label>${window.i18n.pdfPrint.topMargin}</label></div>`);
        marginGroup.append(topMarginGroup);
        this.topMarginEditor = $(`<input type="number" class="form-control" style="display: inline-block;width: 40px;font-size: 12px;padding: 1px;height: 28px">`);
        topMarginGroup.append(this.topMarginEditor);
        this.topMarginEditor.change(function () {
            let value = $(this).val();
            if (!value || isNaN(value)) {
                Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_1__["alert"])(`${window.i18n.pdfPrint.numberTip}`);
                return;
            }
            _this.paper.topMargin = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["mmToPoint"])(value);
        });

        const bottomMarginGroup = $(`<div class="form-group" style="display: inline-block;margin-left: 6px""><label>${window.i18n.pdfPrint.bottomMargin}</label></div>`);
        marginGroup.append(bottomMarginGroup);
        this.bottomMarginEditor = $(`<input type="number" class="form-control" style="display: inline-block;width: 40px;font-size: 12px;padding: 1px;height: 28px">`);
        bottomMarginGroup.append(this.bottomMarginEditor);
        this.bottomMarginEditor.change(function () {
            let value = $(this).val();
            if (!value || isNaN(value)) {
                Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_1__["alert"])(`${window.i18n.pdfPrint.numberTip}`);
                return;
            }
            _this.paper.bottomMargin = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["mmToPoint"])(value);
        });
        const file = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["getParameter"])('_u');
        const urlParameters = window.location.search;
        const button = $(`<button class="btn btn-primary" style="padding-top:5px;height: 30px;margin-left: 10px;">${window.i18n.pdfPrint.apply}</button>`);
        toolbar.append(button);
        let index = 0;
        button.on('click', () => {
            Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["showLoading"])();
            const paperData = JSON.stringify(_this.paper);
            $.ajax({
                url: window._server + '/pdf/newPaging' + urlParameters,
                type: 'POST',
                data: {
                    _paper: paperData
                },
                headers: {
                    'Authorization': token
                },
                success: function () {
                    const newUrl = window._server + '/pdf/show' + urlParameters + '&_r=' + index++;
                    _this.iFrame.prop('src', newUrl);
                },
                error: function () {
                    Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["hideLoading"])();
                    Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_1__["alert"])(`${window.i18n.pdfPrint.fail}`);
                }
            });
        });

        const printButton = $(`<button class="btn btn-danger" style="padding-top:5px;height: 30px;margin-left: 10px;">${window.i18n.pdfPrint.print}</button>`);
        toolbar.append(printButton);
        printButton.on('click', () => {
            window.frames['_iframe_for_pdf_print'].window.print();
        });
    }

    initIFrame() {
        if (this.iFrame) {
            return;
        }
        const urlParameters = buildLocationSearchParameters();
        const h = $(window).height();
        const url = window._server + "/pdf/show" + urlParameters + "&_p=1";
        this.iFrame = $(`<iframe name="_iframe_for_pdf_print" style="width: 100%;height:600px;margin-top: 5px;border:solid 1px #c2c2c2" frameborder="0" src="${url}"></iframe>`);
        this.body.append(this.iFrame);
        const iframe = this.iFrame.get(0);
        const msie = window.navigator.appName.indexOf("Internet Explorer");
        const ie11 = !!window.MSInputMethodContext && !!document.documentMode;
        if (msie === -1 && !ie11) {
            Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["showLoading"])();
        }
        this.iFrame.on('load', function () {
            Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["hideLoading"])();
        });
    }

    show(paper) {
        console.log(paper);
        this.paper = paper;
        this.pageSelect.val(this.paper.paperType);
        this.pageWidthEditor.val(Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["pointToMM"])(this.paper.width));
        this.pageHeightEditor.val(Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["pointToMM"])(this.paper.height));
        this.pageSelect.trigger('change');
        this.leftMarginEditor.val(Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["pointToMM"])(this.paper.leftMargin));
        this.rightMarginEditor.val(Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["pointToMM"])(this.paper.rightMargin));
        this.topMarginEditor.val(Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["pointToMM"])(this.paper.topMargin));
        this.bottomMarginEditor.val(Object(_Utils_js__WEBPACK_IMPORTED_MODULE_0__["pointToMM"])(this.paper.bottomMargin));
        this.orientationSelect.val(this.paper.orientation);
        this.dialog.modal('show');
        this.initIFrame();
    }
};

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

/***/ "./src/i18n/preview.json":
/*!*******************************!*\
  !*** ./src/i18n/preview.json ***!
  \*******************************/
/*! exports provided: pdfPrint, default */
/***/ (function(module) {

module.exports = JSON.parse("{\"pdfPrint\":{\"title\":\"PDF在线打印\",\"setup\":\"打印配置\",\"paper\":\"纸张:\",\"custom\":\"自定义\",\"width\":\"宽(毫米):\",\"numberTip\":\"请输入数字！\",\"height\":\"高(毫米):\",\"orientation\":\"方向:\",\"portrait\":\"纵向\",\"landscape\":\"横向\",\"leftMargin\":\"左边距(毫米):\",\"rightMargin\":\"右边距(毫米):\",\"topMargin\":\"上边距(毫米):\",\"bottomMargin\":\"下边距(毫米):\",\"apply\":\"应用\",\"fail\":\"操作失败！\",\"print\":\"打印\"}}");

/***/ }),

/***/ "./src/i18n/preview_en.json":
/*!**********************************!*\
  !*** ./src/i18n/preview_en.json ***!
  \**********************************/
/*! exports provided: pdfPrint, default */
/***/ (function(module) {

module.exports = JSON.parse("{\"pdfPrint\":{\"title\":\"pdf online print\",\"setup\":\"Print Setup\",\"paper\":\"Paper:\",\"custom\":\"Custom\",\"width\":\"Width(mm):\",\"numberTip\":\"Please input a number\",\"height\":\"Height(mm):\",\"orientation\":\"Orientation:\",\"portrait\":\"Portrait\",\"landscape\":\"Landscape\",\"leftMargin\":\"Left Margin(mm):\",\"rightMargin\":\"Right Margin(mm):\",\"topMargin\":\"Top Margin(mm):\",\"bottomMargin\":\"Bottom Margin(mm):\",\"apply\":\"Apply\",\"fail\":\"Apply fail!\",\"print\":\"Print\"}}");

/***/ }),

/***/ "./src/preview.js":
/*!************************!*\
  !*** ./src/preview.js ***!
  \************************/
/*! no exports provided */
/***/ (function(module, __webpack_exports__, __webpack_require__) {

"use strict";
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _form_external_bootstrap_datetimepicker_css__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./form/external/bootstrap-datetimepicker.css */ "./src/form/external/bootstrap-datetimepicker.css");
/* harmony import */ var _form_external_bootstrap_datetimepicker_css__WEBPACK_IMPORTED_MODULE_0___default = /*#__PURE__*/__webpack_require__.n(_form_external_bootstrap_datetimepicker_css__WEBPACK_IMPORTED_MODULE_0__);
/* harmony import */ var _Utils_js__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./Utils.js */ "./src/Utils.js");
/* harmony import */ var _MsgBox_js__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! ./MsgBox.js */ "./src/MsgBox.js");
/* harmony import */ var _dialog_PDFPrintDialog_js__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ./dialog/PDFPrintDialog.js */ "./src/dialog/PDFPrintDialog.js");
/* harmony import */ var _i18n_preview_json__WEBPACK_IMPORTED_MODULE_4__ = __webpack_require__(/*! ./i18n/preview.json */ "./src/i18n/preview.json");
var _i18n_preview_json__WEBPACK_IMPORTED_MODULE_4___namespace = /*#__PURE__*/__webpack_require__.t(/*! ./i18n/preview.json */ "./src/i18n/preview.json", 1);
/* harmony import */ var _i18n_preview_en_json__WEBPACK_IMPORTED_MODULE_5__ = __webpack_require__(/*! ./i18n/preview_en.json */ "./src/i18n/preview_en.json");
var _i18n_preview_en_json__WEBPACK_IMPORTED_MODULE_5___namespace = /*#__PURE__*/__webpack_require__.t(/*! ./i18n/preview_en.json */ "./src/i18n/preview_en.json", 1);
/**
 * Created by Jacky.Gao on 2017-03-17.
 */






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

const previewId = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["getUrlParam"])('id');
const pageNo = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["getUrlParam"])('page') || 0;
const token = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["getUrlParam"])('token');

$(document).ready(function () {

    let jsPageIndex = 0;
    let jsTotalPage = 1;

    // 处理预览关闭按钮
    if (previewId === 'preview') {
        $('.preview-btn').hide();
    } else {
        $('.preview-btn').show();
    }

    // 页面初始化
    pageInit(pageNo);

    function pageInit(pageNo) {
        Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["showLoading"])();
        console.log(token);
        // 初始化预览报表数据
        $.ajax({
            url: `${window._server}/DataReport/preview?id=${previewId}&page=${pageNo}`,
            type: 'GET',
            headers: {
                'Authorization': token
            },
            success: res => {
                const data = res.data;
                if (res.code !== 200) {
                    Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_2__["alert"])(res.msg);
                } else {
                    // 页面数据渲染
                    let styles = `<style type="text/css">`;
                    styles += data.style;
                    styles += `</style>`;
                    $('#_ureport_table').html(styles + data.content);
                    $('#_ureport_table_style').html(data.style);

                    jsPageIndex = data.pageIndex;
                    jsTotalPage = data.totalPage;

                    if (data.pageIndex === 0) {
                        // 不分页
                        $('#pageLinkContainer').show();
                        $('.pageNum').html('1/1');
                    } else {
                        $('#pageLinkContainer').show();
                        $('.pageNum').html(`${jsPageIndex}/${jsTotalPage}`);
                    }

                    // 自动刷新处理
                    if (data.htmlIntervalRefreshValue > 0) _intervalRefresh(data.htmlIntervalRefreshValue, data.totalPageWithCol);

                    // 图表数据
                    _buildChartDatas(data.chartDatas);

                    Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["hideLoading"])();
                    $('#_ureport_table').show();
                }
            },
            error: response => {
                Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["hideLoading"])();
                if (response && response.responseText) {
                    Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_2__["alert"])("服务端错误：" + response.responseText + "");
                } else {
                    Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_2__["alert"])("服务端出错！");
                }
            }
        });
    }

    // 第一页
    $('.pageIndex').on('click', () => {
        if (jsPageIndex !== 0) {
            if (jsPageIndex === 1) return;
            pageInit(1);
        }
    });

    // 上一页
    $('.pagePre').on('click', () => {
        if (jsPageIndex !== 0) {
            if (jsPageIndex === 1) return;
            pageInit(jsPageIndex - 1);
        }
    });

    // 下一页
    $('.pageNext').on('click', () => {
        if (jsPageIndex !== 0) {
            if (jsPageIndex === jsTotalPage) return;
            pageInit(jsPageIndex + 1);
        }
    });

    // 最后一页
    $('.pageLast').on('click', () => {
        if (jsPageIndex !== 0) {
            if (jsPageIndex === jsTotalPage) return;
            pageInit(jsTotalPage);
        }
    });

    let language = window.navigator.language || window.navigator.browserLanguage;
    if (!language) {
        language = 'zh-cn';
    }
    language = language.toLowerCase();
    window.i18n = _i18n_preview_json__WEBPACK_IMPORTED_MODULE_4__;
    if (language !== 'zh-cn') {
        window.i18n = _i18n_preview_en_json__WEBPACK_IMPORTED_MODULE_5__;
    }

    let directPrintPdf = false,
        index = 0;
    const pdfPrintDialog = new _dialog_PDFPrintDialog_js__WEBPACK_IMPORTED_MODULE_3__["default"]();

    // 关闭
    $(".preview-btn").on('click', () => {
        window.parent.postMessage('closeDialog', '*');
    });

    // 在线打印
    $('.ureport-print').on('click', () => {
        const urlParameters = buildLocationSearchParameters();
        const url = window._server + '/preview/loadPrintPages' + urlParameters;
        Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["showLoading"])();
        $.ajax({
            url,
            type: 'POST',
            headers: {
                'Authorization': token
            },
            success: result => {
                $.get(window._server + '/preview/loadPagePaper' + urlParameters, function (paper) {
                    Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["hideLoading"])();
                    const html = result.data;
                    const iFrame = window.frames['_print_frame'];
                    let styles = `<style type="text/css">`;
                    styles += buildPrintStyle(paper);
                    styles += $('#_ureport_table_style').html();
                    styles += `</style>`;
                    $(iFrame.document.body).html(styles + html);
                    iFrame.window.focus();
                    iFrame.window.print();
                });
            },
            error: response => {
                Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["hideLoading"])();
                if (response && response.responseText) {
                    Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_2__["alert"])("服务端错误：" + response.responseText + "");
                } else {
                    Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_2__["alert"])("服务端出错！");
                }
            }
        });
    });

    // PDF在线预览打印
    $('.ureport-pdf-print').on('click', () => {
        const urlParameters = buildLocationSearchParameters();
        $.get(window._server + '/preview/loadPagePaper' + urlParameters, function (paper) {
            pdfPrintDialog.show(paper);
        });
    });

    // 刷新
    $('.ureport-refresh').on('click', () => {
        location.reload();
    });

    // PDF在线打印
    $('.ureport-pdf-direct-print').on('click', () => {

        Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["showLoading"])();
        const urlParameters = buildLocationSearchParameters();
        const url = window._server + '/pdf/show' + urlParameters + `&_i=${index++}`;
        const iframe = window.frames['_print_pdf_frame'];
        if (!directPrintPdf) {
            directPrintPdf = true;
            $("iframe[name='_print_pdf_frame']").on("load", function () {
                Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["hideLoading"])();
                iframe.window.focus();
                iframe.window.print();
            });
        }
        iframe.window.focus();
        iframe.location.href = url;

        // showLoading();
        // const urlParameters = buildLocationSearchParameters();
        // const url = window._server + '/pdf/show' + urlParameters + `&_i=${index++}`;
        // const iFrame = window.frames['_print_pdf_frame'];
        // if (!directPrintPdf) {
        //     directPrintPdf = true;
        //     $("iframe[name='_print_pdf_frame']").on("load", function () {
        //         hideLoading();
        //         iFrame.window.focus();
        //         iFrame.window.print();
        //     });
        // }
        // iFrame.window.focus();
        // iFrame.location.href = url;
    });

    // 导出PDF
    $('.ureport-export-pdf').on('click', () => {
        const urlParameters = buildLocationSearchParameters();
        const url = window._server + '/pdf' + urlParameters;
        window.open(url, '_blank');
    });

    // 导出WORD
    $(`.ureport-export-word`).on('click', () => {
        const urlParameters = buildLocationSearchParameters();
        const url = window._server + '/word' + urlParameters;
        window.open(url, '_blank');
    });

    // 导出Excel
    $(`.ureport-export-excel`).on('click', () => {
        const urlParameters = buildLocationSearchParameters();
        const url = window._server + '/excel' + urlParameters;
        window.open(url, '_blank');
    });

    // 分页导出EXCEL
    $(`.ureport-export-excel-paging`).on('click', () => {
        const urlParameters = buildLocationSearchParameters();
        const url = window._server + '/excel/paging' + urlParameters;
        window.open(url, '_blank');
    });

    // 分页分Sheet导出EXCEL
    $(`.ureport-export-excel-paging-sheet`).on('click', () => {
        const urlParameters = buildLocationSearchParameters();
        const url = window._server + '/excel/sheet' + urlParameters;
        window.open(url, '_blank');
    });
});

window._currentPageIndex = null;
window._totalPage = null;

window.buildLocationSearchParameters = function (exclude) {

    let urlParameters = `${window.location.search}&token=${token}`;
    if (urlParameters.length > 0) {
        urlParameters = urlParameters.substring(1, urlParameters.length);
    }
    let parameters = {};
    const pairs = urlParameters.split('&');
    for (let i = 0; i < pairs.length; i++) {
        const item = pairs[i];
        if (item === '') {
            continue;
        }
        const param = item.split('=');
        let key = param[0];
        if (exclude && key === exclude) {
            continue;
        }
        let value = param[1];
        parameters[key] = value;
    }
    if (window.searchFormParameters) {
        for (let key in window.searchFormParameters) {
            if (key === exclude) {
                continue;
            }
            const value = window.searchFormParameters[key];
            if (value) {
                parameters[key] = value;
            }
        }
    }
    let p = '?';
    for (let key in parameters) {
        if (p === '?') {
            p += key + '=' + parameters[key];
        } else {
            p += '&' + key + '=' + parameters[key];
        }
    }
    return p;
};

function buildPrintStyle(paper) {
    const marginLeft = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["pointToMM"])(paper.leftMargin);
    const marginTop = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["pointToMM"])(paper.topMargin);
    const marginRight = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["pointToMM"])(paper.rightMargin);
    const marginBottom = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["pointToMM"])(paper.bottomMargin);
    const paperType = paper.paperType;
    let page = paperType;
    if (paperType === 'CUSTOM') {
        page = Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["pointToMM"])(paper.width) + 'mm ' + Object(_Utils_js__WEBPACK_IMPORTED_MODULE_1__["pointToMM"])(paper.height) + 'mm';
    }
    const style = `
        @media print {
            .page-break{
                display: block;
                page-break-before: always;
            }
        }
        @page {
          size: ${page} ${paper.orientation};
          margin-left: ${marginLeft}mm;
          margin-top: ${marginTop}mm;
          margin-right:${marginRight}mm;
          margin-bottom:${marginBottom}mm;
        }
    `;
    return style;
};

window._intervalRefresh = function (value, totalPage) {
    if (!value) {
        return;
    }
    window._totalPage = totalPage;
    const second = value * 1000;
    setTimeout(function () {
        _refreshData(second);
    }, second);
};

function _refreshData(second) {
    const params = buildLocationSearchParameters('_i');
    let url = window._server + `/preview/loadData${params}`;
    const totalPage = window._totalPage;
    if (totalPage > 0) {
        if (window._currentPageIndex) {
            if (window._currentPageIndex > totalPage) {
                window._currentPageIndex = 1;
            }
            url += "&_i=" + window._currentPageIndex + "";
        }
        $("#pageSelector").val(window._currentPageIndex);
    }
    $.ajax({
        url,
        type: 'GET',
        headers: {
            'Authorization': token
        },
        success: report => {
            const tableContainer = $(`#_ureport_table`);
            tableContainer.empty();
            window._totalPage = report.totalPageWithCol;
            tableContainer.append(report.content);
            _buildChartDatas(report.chartDatas);
            buildPaging(window._currentPageIndex, window._totalPage);
            if (window._currentPageIndex) {
                window._currentPageIndex++;
            }
            setTimeout(function () {
                _refreshData(second);
            }, second);
        },
        error: function (response) {
            const tableContainer = $(`#_ureport_table`);
            tableContainer.empty();
            if (response && response.responseText) {
                tableContainer.append("<h3 style='color: #d30e00;'>服务端错误：" + response.responseText + "</h3>");
            } else {
                tableContainer.append("<h3 style='color: #d30e00;'>加载数据失败</h3>");
            }
            setTimeout(function () {
                _refreshData(second);
            }, second);
        }
    });
};

window._buildChartDatas = function (chartData) {
    if (!chartData) {
        return;
    }
    for (let d of chartData) {
        let json = d.json;
        json = JSON.parse(json, function (k, v) {
            if (v.indexOf && v.indexOf('function') > -1) {
                return eval("(function(){return " + v + " })()");
            }
            return v;
        });
        _buildChart(d.id, json);
    }
};
window._buildChart = function (canvasId, chartJson) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) {
        return;
    }
    let options = chartJson.options;
    if (!options) {
        options = {};
        chartJson.options = options;
    }
    let animation = options.animation;
    if (!animation) {
        animation = {};
        options.animation = animation;
    }
    animation.onComplete = function (event) {
        const chart = event.chart;
        const base64Image = chart.toBase64Image();
        const urlParameters = window.location.search;
        const url = window._server + '/chart/storeData' + urlParameters;
        const canvas = $("#" + canvasId);
        const width = parseInt(canvas.css('width'));
        const height = parseInt(canvas.css('height'));
        $.ajax({
            type: 'POST',
            headers: {
                'Authorization': token
            },
            data: {
                _base64Data: base64Image,
                _chartId: canvasId,
                _width: width,
                _height: height
            },
            url
        });
    };
    const chart = new Chart(ctx, chartJson);
};

window.submitSearchForm = function (file, customParameters) {
    window.searchFormParameters = {};
    for (let fun of window.formElements) {
        const json = fun.call(this);
        for (let key in json) {
            let value = json[key];
            value = encodeURI(value);
            value = encodeURI(value);
            window.searchFormParameters[key] = value;
        }
    }
    const parameters = window.buildLocationSearchParameters('_i');
    let url = window._server + "/preview/loadData" + parameters;
    const pageSelector = $(`#pageSelector`);
    if (pageSelector.length > 0) {
        url += '&_i=1';
    }
    $.ajax({
        url,
        type: 'POST',
        headers: {
            'Authorization': token
        },
        success: report => {
            window._currentPageIndex = 1;
            const tableContainer = $(`#_ureport_table`);
            tableContainer.empty();
            tableContainer.append(report.content);
            _buildChartDatas(report.chartDatas);
            const totalPage = report.totalPage;
            window._totalPage = totalPage;
            if (pageSelector.length > 0) {
                pageSelector.empty();
                for (let i = 1; i <= totalPage; i++) {
                    pageSelector.append(`<option>${i}</option>`);
                }
                const pageIndex = report.pageIndex || 1;
                pageSelector.val(pageIndex);
                $('#totalPageLabel').html(totalPage);
                buildPaging(pageIndex, totalPage);
            }
        },
        error: response => {
            if (response && response.responseText) {
                Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_2__["alert"])("服务端错误：" + response.responseText + "");
            } else {
                Object(_MsgBox_js__WEBPACK_IMPORTED_MODULE_2__["alert"])('查询操作失败！');
            }
        }
    });
};

/***/ })

/******/ });
//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly8vd2VicGFjay9ib290c3RyYXAiLCJ3ZWJwYWNrOi8vLy4vc3JjL2Zvcm0vZXh0ZXJuYWwvYm9vdHN0cmFwLWRhdGV0aW1lcGlja2VyLmNzcyIsIndlYnBhY2s6Ly8vLi9ub2RlX21vZHVsZXMvY3NzLWxvYWRlci9saWIvY3NzLWJhc2UuanMiLCJ3ZWJwYWNrOi8vLy4vbm9kZV9tb2R1bGVzL3N0eWxlLWxvYWRlci9hZGRTdHlsZXMuanMiLCJ3ZWJwYWNrOi8vLy4vbm9kZV9tb2R1bGVzL3VuZG8tbWFuYWdlci9saWIvdW5kb21hbmFnZXIuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL01zZ0JveC5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvVXRpbHMuanMiLCJ3ZWJwYWNrOi8vLy4vc3JjL2RpYWxvZy9QREZQcmludERpYWxvZy5qcyIsIndlYnBhY2s6Ly8vLi9zcmMvZm9ybS9leHRlcm5hbC9ib290c3RyYXAtZGF0ZXRpbWVwaWNrZXIuY3NzPzUzODMiLCJ3ZWJwYWNrOi8vLy4vc3JjL3ByZXZpZXcuanMiXSwibmFtZXMiOlsiYWxlcnQiLCJtc2ciLCJ0b2FzdHIiLCJvcHRpb25zIiwiY2xvc2VCdXR0b24iLCJkZWJ1ZyIsInByb2dyZXNzQmFyIiwicG9zaXRpb25DbGFzcyIsIm9uY2xpY2siLCJzaG93RHVyYXRpb24iLCJoaWRlRHVyYXRpb24iLCJ0aW1lT3V0IiwiZXh0ZW5kZWRUaW1lT3V0Iiwic2hvd0Vhc2luZyIsImhpZGVFYXNpbmciLCJzaG93TWV0aG9kIiwiaGlkZU1ldGhvZCIsImVycm9yIiwic3VjY2Vzc0FsZXJ0Iiwic3VjY2VzcyIsImNvbmZpcm0iLCJjYWxsYmFjayIsImRpYWxvZyIsImJ1aWxkRGlhbG9nIiwibmFtZSIsImNsaWNrIiwiY2FsbCIsIm1vZGFsIiwidGl0bGUiLCJjb250ZW50Iiwic2hvd0RpYWxvZyIsImRpYWxvZ0NvbnRlbnQiLCJidXR0b25zIiwiZXZlbnRzIiwibGFyZ2UiLCJldmVudCIsIm9uIiwiY2xhc3NOYW1lIiwiJCIsImFwcGVuZCIsImZpbmQiLCJmb290ZXIiLCJmb3JFYWNoIiwiYnRuIiwiaW5kZXgiLCJidXR0b24iLCJlIiwiaG9sZERpYWxvZyIsImJpbmQiLCJva0J0biIsImRvY3VtZW50IiwiZWFjaCIsImkiLCJkIiwiekluZGV4IiwiY3NzIiwiaXNOYU4iLCJwYXJzZUludCIsInNob3dMb2FkaW5nIiwidXJsIiwiaCIsIndpbmRvdyIsImhlaWdodCIsInciLCJ3aWR0aCIsImNvdmVyIiwiYm9keSIsImxvYWRpbmciLCJoaWRlTG9hZGluZyIsInJlbW92ZSIsInJlc2V0VGFibGVEYXRhIiwiaG90IiwiY291bnRDb2xzIiwiY291bnRSb3dzIiwiY29udGV4dCIsImRhdGEiLCJyb3dEYXRhIiwiaiIsInRkIiwiZ2V0Q2VsbCIsInB1c2giLCJjZWxsRGVmIiwidmFsdWVUeXBlIiwidmFsdWUiLCJ0eXBlIiwidGV4dCIsImRhdGFzZXROYW1lIiwiYWdncmVnYXRlIiwicHJvcCIsInByb3BlcnR5IiwibGVuZ3RoIiwic3Vic3RyaW5nIiwidiIsImxvYWREYXRhIiwiYnVpbGROZXdDZWxsRGVmIiwicm93TnVtYmVyIiwiY29sdW1uTnVtYmVyIiwiZXhwYW5kIiwiY2VsbFN0eWxlIiwiZm9udFNpemUiLCJmb3JlY29sb3IiLCJmb250RmFtaWx5IiwiYWxpZ24iLCJ2YWxpZ24iLCJ0YWJsZVRvWG1sIiwieG1sIiwicm93c1htbCIsImNvbHVtblhtbCIsInJvd0hlYWRlcnMiLCJnZXRSb3dIZWlnaHQiLCJwaXhlbFRvUG9pbnQiLCJiYW5kIiwiaGVhZGVyIiwiZ2V0Q29sV2lkdGgiLCJjZWxsWG1sIiwic3BhbkRhdGEiLCJpbmRleE9mIiwiY2VsbE5hbWUiLCJnZXRDZWxsTmFtZSIsImxlZnRQYXJlbnRDZWxsTmFtZSIsInRvcFBhcmVudENlbGxOYW1lIiwiZmlsbEJsYW5rUm93cyIsIm11bHRpcGxlIiwic3BhbiIsImdldFNwYW4iLCJyb3dTcGFuIiwicm93c3BhbiIsImNvbFNwYW4iLCJjb2xzcGFuIiwic3RhcnRSb3ciLCJlbmRSb3ciLCJzdGFydENvbCIsImVuZENvbCIsInIiLCJjIiwibGlua1VybCIsImxpbmtUYXJnZXRXaW5kb3ciLCJidWlsZENlbGxTdHlsZSIsImxpbmtQYXJhbWV0ZXJzIiwicGFyYW0iLCJtYXBwaW5nVHlwZSIsImVuY29kZSIsIm9yZGVyIiwibWFwcGluZ0RhdGFzZXQiLCJtYXBwaW5nS2V5UHJvcGVydHkiLCJtYXBwaW5nVmFsdWVQcm9wZXJ0eSIsImJ1aWxkQ29uZGl0aW9ucyIsImNvbmRpdGlvbnMiLCJncm91cEl0ZW1zIiwiZ3JvdXBJdGVtIiwiY29uZGl0aW9uIiwibGVmdCIsIm9wZXJhdGlvbiIsIm9wIiwiaWQiLCJqb2luIiwicmlnaHQiLCJtYXBwaW5nSXRlbXMiLCJtYXBwaW5nSXRlbSIsImxhYmVsIiwic291cmNlIiwiY2F0ZWdvcnkiLCJmb3JtYXQiLCJzbGFzaGVzIiwic2xhc2giLCJ4IiwieSIsImRlZ3JlZSIsImJhc2U2NERhdGEiLCJjaGFydCIsImRhdGFzZXQiLCJjYXRlZ29yeVByb3BlcnR5Iiwic2VyaWVzUHJvcGVydHkiLCJzZXJpZXNUeXBlIiwic2VyaWVzVGV4dCIsInZhbHVlUHJvcGVydHkiLCJyUHJvcGVydHkiLCJ4UHJvcGVydHkiLCJ5UHJvcGVydHkiLCJjb2xsZWN0VHlwZSIsInhheGVzIiwicm90YXRpb24iLCJzY2FsZUxhYmVsIiwiZGlzcGxheSIsImxhYmVsU3RyaW5nIiwieWF4ZXMiLCJvcHRpb24iLCJwb3NpdGlvbiIsInVuZGVmaW5lZCIsImR1cmF0aW9uIiwiZWFzaW5nIiwicGx1Z2lucyIsInBsdWdpbiIsInByb3BlcnR5Q29uZGl0aW9ucyIsImNvbmRpdGlvblByb3BlcnR5SXRlbXMiLCJwYyIsInJvd0hlaWdodCIsImNvbFdpZHRoIiwibmV3VmFsdWUiLCJ0YXJnZXRXaW5kb3ciLCJwYWdpbmciLCJsaW5lIiwic3R5bGUiLCJyZXBvcnREZWYiLCJjZW50ZXIiLCJib2xkIiwiaXRhbGljIiwidW5kZXJsaW5lIiwibWFyZ2luIiwiZGF0YXNvdXJjZVhtbCIsImRhdGFzb3VyY2VzIiwiZGF0YXNvdXJjZSIsImRzIiwidXNlcm5hbWUiLCJwYXNzd29yZCIsImRyaXZlciIsImRhdGFzZXRzIiwic3FsIiwiZmllbGQiLCJmaWVsZHMiLCJwYXJhbWV0ZXIiLCJwYXJhbWV0ZXJzIiwiZGVmYXVsdFZhbHVlIiwiYmVhbklkIiwibWV0aG9kIiwiY2xhenoiLCJwYXBlciIsImh0bWxJbnRlcnZhbFJlZnJlc2hWYWx1ZSIsInBhcGVyVHlwZSIsImxlZnRNYXJnaW4iLCJyaWdodE1hcmdpbiIsInRvcE1hcmdpbiIsImJvdHRvbU1hcmdpbiIsInBhZ2luZ01vZGUiLCJmaXhSb3dzIiwib3JpZW50YXRpb24iLCJodG1sUmVwb3J0QWxpZ24iLCJiZ0ltYWdlIiwiY29sdW1uRW5hYmxlZCIsImNvbHVtbkNvdW50IiwiY29sdW1uTWFyZ2luIiwic2VhcmNoRm9ybVhtbCIsImVuY29kZVVSSUNvbXBvbmVudCIsInJvdyIsImNvbCIsIm1lcmdlQ2VsbHMiLCJnZXRTZXR0aW5ncyIsIml0ZW0iLCJzaXplIiwiZm9udFNpemVTY29wZSIsImZvcmVjb2xvclNjb3BlIiwiZm9udEZhbWlseVNjb3BlIiwiYmdjb2xvciIsImJnY29sb3JTY29wZSIsImJvbGRTY29wZSIsIml0YWxpY1Njb3BlIiwidW5kZXJsaW5lU2NvcGUiLCJ3cmFwQ29tcHV0ZSIsImFsaWduU2NvcGUiLCJ2YWxpZ25TY29wZSIsImxpbmVIZWlnaHQiLCJsZWZ0Qm9yZGVyIiwiY29sb3IiLCJyaWdodEJvcmRlciIsInRvcEJvcmRlciIsImJvdHRvbUJvcmRlciIsInJlc3VsdCIsInJlcGxhY2UiLCJnZXRQYXJhbWV0ZXIiLCJyZWciLCJSZWdFeHAiLCJsb2NhdGlvbiIsInNlYXJjaCIsInN1YnN0ciIsIm1hdGNoIiwibW1Ub1BvaW50IiwibW0iLCJNYXRoIiwicm91bmQiLCJwb2ludFRvTU0iLCJwb2ludCIsInBvaW50VG9QaXhlbCIsInBpeGVsIiwic2V0RGlydHkiLCJyZW1vdmVDbGFzcyIsInJlc2V0RGlydHkiLCJhZGRDbGFzcyIsImZvcm1hdERhdGUiLCJkYXRlIiwiRGF0ZSIsIm8iLCJnZXRNb250aCIsImdldERhdGUiLCJnZXRIb3VycyIsImdldE1pbnV0ZXMiLCJnZXRTZWNvbmRzIiwidGVzdCIsIiQxIiwiZ2V0RnVsbFllYXIiLCJrIiwiYnVpbGRQYWdlU2l6ZUxpc3QiLCJBMCIsIkExIiwiQTIiLCJBMyIsIkE0IiwiQTUiLCJBNiIsIkE3IiwiQTgiLCJBOSIsIkExMCIsIkIwIiwiQjEiLCJCMiIsIkIzIiwiQjQiLCJCNSIsIkI2IiwiQjciLCJCOCIsIkI5IiwiQjEwIiwidW5kb01hbmFnZXIiLCJVbmRvTWFuYWdlciIsImdldFVybFBhcmFtIiwidW5lc2NhcGUiLCJQREZQcmludERpYWxvZyIsImNvbnN0cnVjdG9yIiwicGFwZXJTaXplTGlzdCIsImkxOG4iLCJwZGZQcmludCIsImluaXRCb2R5IiwidG9rZW4iLCJ0b29sYmFyIiwic2V0dXAiLCJwYWdlVHlwZUdyb3VwIiwicGFnZVNlbGVjdCIsImN1c3RvbSIsIl90aGlzIiwiY2hhbmdlIiwidmFsIiwicGFnZVdpZHRoRWRpdG9yIiwicGFnZUhlaWdodEVkaXRvciIsInBhZ2VTaXplIiwiY29uc29sZSIsImxvZyIsInBhZ2VXaWR0aEdyb3VwIiwibnVtYmVyVGlwIiwicHJpbnRMaW5lIiwicmVmcmVzaCIsInBhZ2VIZWlnaHRHcm91cCIsIm9yaWVudGF0aW9uR3JvdXAiLCJvcmllbnRhdGlvblNlbGVjdCIsInBvcnRyYWl0IiwibGFuZHNjYXBlIiwibWFyZ2luR3JvdXAiLCJsZWZ0TWFyZ2luR3JvdXAiLCJsZWZ0TWFyZ2luRWRpdG9yIiwicmlnaHRNYXJnaW5Hcm91cCIsInJpZ2h0TWFyZ2luRWRpdG9yIiwidG9wTWFyZ2luR3JvdXAiLCJ0b3BNYXJnaW5FZGl0b3IiLCJib3R0b21NYXJnaW5Hcm91cCIsImJvdHRvbU1hcmdpbkVkaXRvciIsImZpbGUiLCJ1cmxQYXJhbWV0ZXJzIiwiYXBwbHkiLCJwYXBlckRhdGEiLCJKU09OIiwic3RyaW5naWZ5IiwiYWpheCIsIl9zZXJ2ZXIiLCJfcGFwZXIiLCJoZWFkZXJzIiwibmV3VXJsIiwiaUZyYW1lIiwiZmFpbCIsInByaW50QnV0dG9uIiwicHJpbnQiLCJmcmFtZXMiLCJpbml0SUZyYW1lIiwiYnVpbGRMb2NhdGlvblNlYXJjaFBhcmFtZXRlcnMiLCJpZnJhbWUiLCJnZXQiLCJtc2llIiwibmF2aWdhdG9yIiwiYXBwTmFtZSIsImllMTEiLCJNU0lucHV0TWV0aG9kQ29udGV4dCIsImRvY3VtZW50TW9kZSIsInNob3ciLCJ0cmlnZ2VyIiwiZm4iLCJkYXRldGltZXBpY2tlciIsImRhdGVzIiwiZGF5cyIsImRheXNTaG9ydCIsImRheXNNaW4iLCJtb250aHMiLCJtb250aHNTaG9ydCIsInRvZGF5Iiwic3VmZml4IiwibWVyaWRpZW0iLCJqUXVlcnkiLCJwcmV2aWV3SWQiLCJwYWdlTm8iLCJyZWFkeSIsImpzUGFnZUluZGV4IiwianNUb3RhbFBhZ2UiLCJoaWRlIiwicGFnZUluaXQiLCJyZXMiLCJjb2RlIiwic3R5bGVzIiwiaHRtbCIsInBhZ2VJbmRleCIsInRvdGFsUGFnZSIsIl9pbnRlcnZhbFJlZnJlc2giLCJ0b3RhbFBhZ2VXaXRoQ29sIiwiX2J1aWxkQ2hhcnREYXRhcyIsImNoYXJ0RGF0YXMiLCJyZXNwb25zZSIsInJlc3BvbnNlVGV4dCIsImxhbmd1YWdlIiwiYnJvd3Nlckxhbmd1YWdlIiwidG9Mb3dlckNhc2UiLCJkZWZhdWx0STE4bkpzb25EYXRhIiwiZW4xOG5Kc29uRGF0YSIsImRpcmVjdFByaW50UGRmIiwicGRmUHJpbnREaWFsb2ciLCJwYXJlbnQiLCJwb3N0TWVzc2FnZSIsImJ1aWxkUHJpbnRTdHlsZSIsImZvY3VzIiwicmVsb2FkIiwiaHJlZiIsIm9wZW4iLCJfY3VycmVudFBhZ2VJbmRleCIsIl90b3RhbFBhZ2UiLCJleGNsdWRlIiwicGFpcnMiLCJzcGxpdCIsImtleSIsInNlYXJjaEZvcm1QYXJhbWV0ZXJzIiwicCIsIm1hcmdpbkxlZnQiLCJtYXJnaW5Ub3AiLCJtYXJnaW5SaWdodCIsIm1hcmdpbkJvdHRvbSIsInBhZ2UiLCJzZWNvbmQiLCJzZXRUaW1lb3V0IiwiX3JlZnJlc2hEYXRhIiwicGFyYW1zIiwicmVwb3J0IiwidGFibGVDb250YWluZXIiLCJlbXB0eSIsImJ1aWxkUGFnaW5nIiwiY2hhcnREYXRhIiwianNvbiIsInBhcnNlIiwiZXZhbCIsIl9idWlsZENoYXJ0IiwiY2FudmFzSWQiLCJjaGFydEpzb24iLCJjdHgiLCJnZXRFbGVtZW50QnlJZCIsImFuaW1hdGlvbiIsIm9uQ29tcGxldGUiLCJiYXNlNjRJbWFnZSIsInRvQmFzZTY0SW1hZ2UiLCJjYW52YXMiLCJfYmFzZTY0RGF0YSIsIl9jaGFydElkIiwiX3dpZHRoIiwiX2hlaWdodCIsIkNoYXJ0Iiwic3VibWl0U2VhcmNoRm9ybSIsImN1c3RvbVBhcmFtZXRlcnMiLCJmdW4iLCJmb3JtRWxlbWVudHMiLCJlbmNvZGVVUkkiLCJwYWdlU2VsZWN0b3IiXSwibWFwcGluZ3MiOiI7UUFBQTtRQUNBOztRQUVBO1FBQ0E7O1FBRUE7UUFDQTtRQUNBO1FBQ0E7UUFDQTtRQUNBO1FBQ0E7UUFDQTtRQUNBO1FBQ0E7O1FBRUE7UUFDQTs7UUFFQTtRQUNBOztRQUVBO1FBQ0E7UUFDQTs7O1FBR0E7UUFDQTs7UUFFQTtRQUNBOztRQUVBO1FBQ0E7UUFDQTtRQUNBLDBDQUEwQyxnQ0FBZ0M7UUFDMUU7UUFDQTs7UUFFQTtRQUNBO1FBQ0E7UUFDQSx3REFBd0Qsa0JBQWtCO1FBQzFFO1FBQ0EsaURBQWlELGNBQWM7UUFDL0Q7O1FBRUE7UUFDQTtRQUNBO1FBQ0E7UUFDQTtRQUNBO1FBQ0E7UUFDQTtRQUNBO1FBQ0E7UUFDQTtRQUNBLHlDQUF5QyxpQ0FBaUM7UUFDMUUsZ0hBQWdILG1CQUFtQixFQUFFO1FBQ3JJO1FBQ0E7O1FBRUE7UUFDQTtRQUNBO1FBQ0EsMkJBQTJCLDBCQUEwQixFQUFFO1FBQ3ZELGlDQUFpQyxlQUFlO1FBQ2hEO1FBQ0E7UUFDQTs7UUFFQTtRQUNBLHNEQUFzRCwrREFBK0Q7O1FB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eVgsSUFBckIsQ0FBMEJFLFNBQTFCO0FBQ0F5Qyw0QkFBWTFDLFNBQVosRUFBdUJDLFNBQXZCO0FBQ0g7QUFDSixTQXhCRTtBQXlCSGhaLGVBQU9xWixZQUFZO0FBQ2YsZ0JBQUlBLFlBQVlBLFNBQVNDLFlBQXpCLEVBQXVDO0FBQ25DdmEsd0VBQUtBLENBQUMsV0FBV3NhLFNBQVNDLFlBQXBCLEdBQW1DLEVBQXpDO0FBQ0gsYUFGRCxNQUVPO0FBQ0h2YSx3RUFBS0EsQ0FBQyxTQUFOO0FBQ0g7QUFDSjtBQS9CRSxLQUFQO0FBaUNILENBbERELEMiLCJmaWxlIjoicHJldmlldy5idW5kbGUuanMiLCJzb3VyY2VzQ29udGVudCI6WyIgXHQvLyBUaGUgbW9kdWxlIGNhY2hlXG4gXHR2YXIgaW5zdGFsbGVkTW9kdWxlcyA9IHt9O1xuXG4gXHQvLyBUaGUgcmVxdWlyZSBmdW5jdGlvblxuIFx0ZnVuY3Rpb24gX193ZWJwYWNrX3JlcXVpcmVfXyhtb2R1bGVJZCkge1xuXG4gXHRcdC8vIENoZWNrIGlmIG1vZHVsZSBpcyBpbiBjYWNoZVxuIFx0XHRpZihpbnN0YWxsZWRNb2R1bGVzW21vZHVsZUlkXSkge1xuIFx0XHRcdHJldHVybiBpbnN0YWxsZWRNb2R1bGVzW21vZHVsZUlkXS5leHBvcnRzO1xuIFx0XHR9XG4gXHRcdC8vIENyZWF0ZSBhIG5ldyBtb2R1bGUgKGFuZCBwdXQgaXQgaW50byB0aGUgY2FjaGUpXG4gXHRcdHZhciBtb2R1bGUgPSBpbnN0YWxsZWRNb2R1bGVzW21vZHVsZUlkXSA9IHtcbiBcdFx0XHRpOiBtb2R1bGVJZCxcbiBcdFx0XHRsOiBmYWxzZSxcbiBcdFx0XHRleHBvcnRzOiB7fVxuIFx0XHR9O1xuXG4gXHRcdC8vIEV4ZWN1dGUgdGhlIG1vZHVsZSBmdW5jdGlvblxuIFx0XHRtb2R1bGVzW21vZHVsZUlkXS5jYWxsKG1vZHVsZS5leHBvcnRzLCBtb2R1bGUsIG1vZHVsZS5leHBvcnRzLCBfX3dlYnBhY2tfcmVxdWlyZV9fKTtcblxuIFx0XHQvLyBGbGFnIHRoZSBtb2R1bGUgYXMgbG9hZGVkXG4gXHRcdG1vZHVsZS5sID0gdHJ1ZTtcblxuIFx0XHQvLyBSZXR1cm4gdGhlIGV4cG9ydHMgb2YgdGhlIG1vZHVsZVxuIFx0XHRyZXR1cm4gbW9kdWxlLmV4cG9ydHM7XG4gXHR9XG5cblxuIFx0Ly8gZXhwb3NlIHRoZSBtb2R1bGVzIG9iamVjdCAoX193ZWJwYWNrX21vZHVsZXNfXylcbiBcdF9fd2VicGFja19yZXF1aXJlX18ubSA9IG1vZHVsZXM7XG5cbiBcdC8vIGV4cG9zZSB0aGUgbW9kdWxlIGNhY2hlXG4gXHRfX3dlYnBhY2tfcmVxdWlyZV9fLmMgPSBpbnN0YWxsZWRNb2R1bGVzO1xuXG4gXHQvLyBkZWZpbmUgZ2V0dGVyIGZ1bmN0aW9uIGZvciBoYXJtb255IGV4cG9ydHNcbiBcdF9fd2VicGFja19yZXF1aXJlX18uZCA9IGZ1bmN0aW9uKGV4cG9ydHMsIG5hbWUsIGdldHRlcikge1xuIFx0XHRpZighX193ZWJwYWNrX3JlcXVpcmVfXy5vKGV4cG9ydHMsIG5hbWUpKSB7XG4gXHRcdFx0T2JqZWN0LmRlZmluZVByb3BlcnR5KGV4cG9ydHMsIG5hbWUsIHsgZW51bWVyYWJsZTogdHJ1ZSwgZ2V0OiBnZXR0ZXIgfSk7XG4gXHRcdH1cbiBcdH07XG5cbiBcdC8vIGRlZmluZSBfX2VzTW9kdWxlIG9uIGV4cG9ydHNcbiBcdF9fd2VicGFja19yZXF1aXJlX18uciA9IGZ1bmN0aW9uKGV4cG9ydHMpIHtcbiBcdFx0aWYodHlwZW9mIFN5bWJvbCAhPT0gJ3VuZGVmaW5lZCcgJiYgU3ltYm9sLnRvU3RyaW5nVGFnKSB7XG4gXHRcdFx0T2JqZWN0LmRlZmluZVByb3BlcnR5KGV4cG9ydHMsIFN5bWJvbC50b1N0cmluZ1RhZywgeyB2YWx1ZTogJ01vZHVsZScgfSk7XG4gXHRcdH1cbiBcdFx0T2JqZWN0LmRlZmluZVByb3BlcnR5KGV4cG9ydHMsICdfX2VzTW9kdWxlJywgeyB2YWx1ZTogdHJ1ZSB9KTtcbiBcdH07XG5cbiBcdC8vIGNyZWF0ZSBhIGZha2UgbmFtZXNwYWNlIG9iamVjdFxuIFx0Ly8gbW9kZSAmIDE6IHZhbHVlIGlzIGEgbW9kdWxlIGlkLCByZXF1aXJlIGl0XG4gXHQvLyBtb2RlICYgMjogbWVyZ2UgYWxsIHByb3BlcnRpZXMgb2YgdmFsdWUgaW50byB0aGUgbnNcbiBcdC8vIG1vZGUgJiA0OiByZXR1cm4gdmFsdWUgd2hlbiBhbHJlYWR5IG5zIG9iamVjdFxuIFx0Ly8gbW9kZSAmIDh8MTogYmVoYXZlIGxpa2UgcmVxdWlyZVxuIFx0X193ZWJwYWNrX3JlcXVpcmVfXy50ID0gZnVuY3Rpb24odmFsdWUsIG1vZGUpIHtcbiBcdFx0aWYobW9kZSAmIDEpIHZhbHVlID0gX193ZWJwYWNrX3JlcXVpcmVfXyh2YWx1ZSk7XG4gXHRcdGlmKG1vZGUgJiA4KSByZXR1cm4gdmFsdWU7XG4gXHRcdGlmKChtb2RlICYgNCkgJiYgdHlwZW9mIHZhbHVlID09PSAnb2JqZWN0JyAmJiB2YWx1ZSAmJiB2YWx1ZS5fX2VzTW9kdWxlKSByZXR1cm4gdmFsdWU7XG4gXHRcdHZhciBucyA9IE9iamVjdC5jcmVhdGUobnVsbCk7XG4gXHRcdF9fd2VicGFja19yZXF1aXJlX18ucihucyk7XG4gXHRcdE9iamVjdC5kZWZpbmVQcm9wZXJ0eShucywgJ2RlZmF1bHQnLCB7IGVudW1lcmFibGU6IHRydWUsIHZhbHVlOiB2YWx1ZSB9KTtcbiBcdFx0aWYobW9kZSAmIDIgJiYgdHlwZW9mIHZhbHVlICE9ICdzdHJpbmcnKSBmb3IodmFyIGtleSBpbiB2YWx1ZSkgX193ZWJwYWNrX3JlcXVpcmVfXy5kKG5zLCBrZXksIGZ1bmN0aW9uKGtleSkgeyByZXR1cm4gdmFsdWVba2V5XTsgfS5iaW5kKG51bGwsIGtleSkpO1xuIFx0XHRyZXR1cm4gbnM7XG4gXHR9O1xuXG4gXHQvLyBnZXREZWZhdWx0RXhwb3J0IGZ1bmN0aW9uIGZvciBjb21wYXRpYmlsaXR5IHdpdGggbm9uLWhhcm1vbnkgbW9kdWxlc1xuIFx0X193ZWJwYWNrX3JlcXVpcmVfXy5uID0gZnVuY3Rpb24obW9kdWxlKSB7XG4gXHRcdHZhciBnZXR0ZXIgPSBtb2R1bGUgJiYgbW9kdWxlLl9fZXNNb2R1bGUgP1xuIFx0XHRcdGZ1bmN0aW9uIGdldERlZmF1bHQoKSB7IHJldHVybiBtb2R1bGVbJ2RlZmF1bHQnXTsgfSA6XG4gXHRcdFx0ZnVuY3Rpb24gZ2V0TW9kdWxlRXhwb3J0cygpIHsgcmV0dXJuIG1vZHVsZTsgfTtcbiBcdFx0X193ZWJwYWNrX3JlcXVpcmVfXy5kKGdldHRlciwgJ2EnLCBnZXR0ZXIpO1xuIFx0XHRyZXR1cm4gZ2V0dGVyO1xuIFx0fTtcblxuIFx0Ly8gT2JqZWN0LnByb3RvdHlwZS5oYXNPd25Qcm9wZXJ0eS5jYWxsXG4gXHRfX3dlYnBhY2tfcmVxdWlyZV9fLm8gPSBmdW5jdGlvbihvYmplY3QsIHByb3BlcnR5KSB7IHJldHVybiBPYmplY3QucHJvdG90eXBlLmhhc093blByb3BlcnR5LmNhbGwob2JqZWN0LCBwcm9wZXJ0eSk7IH07XG5cbiBcdC8vIF9fd2VicGFja19wdWJsaWNfcGF0aF9fXG4gXHRfX3dlYnBhY2tfcmVxdWlyZV9fLnAgPSBcIlwiO1xuXG5cbiBcdC8vIExvYWQgZW50cnkgbW9kdWxlIGFuZCByZXR1cm4gZXhwb3J0c1xuIFx0cmV0dXJuIF9fd2VicGFja19yZXF1aXJlX18oX193ZWJwYWNrX3JlcXVpcmVfXy5zID0gXCIuL3NyYy9wcmV2aWV3LmpzXCIpO1xuIiwiZXhwb3J0cyA9IG1vZHVsZS5leHBvcnRzID0gcmVxdWlyZShcIi4uLy4uLy4uL25vZGVfbW9kdWxlcy9jc3MtbG9hZGVyL2xpYi9jc3MtYmFzZS5qc1wiKShmYWxzZSk7XG4vLyBpbXBvcnRzXG5cblxuLy8gbW9kdWxlXG5leHBvcnRzLnB1c2goW21vZHVsZS5pZCwgXCIvKiFcXG4gKiBEYXRldGltZXBpY2tlciBmb3IgQm9vdHN0cmFwXFxuICpcXG4gKiBDb3B5cmlnaHQgMjAxMiBTdGVmYW4gUGV0cmVcXG4gKiBJbXByb3ZlbWVudHMgYnkgQW5kcmV3IFJvd2xzXFxuICogTGljZW5zZWQgdW5kZXIgdGhlIEFwYWNoZSBMaWNlbnNlIHYyLjBcXG4gKiBodHRwOi8vd3d3LmFwYWNoZS5vcmcvbGljZW5zZXMvTElDRU5TRS0yLjBcXG4gKlxcbiAqL1xcbi5kYXRldGltZXBpY2tlciB7XFxuXFx0cGFkZGluZzogNHB4O1xcblxcdG1hcmdpbi10b3A6IDFweDtcXG5cXHQtd2Via2l0LWJvcmRlci1yYWRpdXM6IDRweDtcXG5cXHQtbW96LWJvcmRlci1yYWRpdXM6IDRweDtcXG5cXHRib3JkZXItcmFkaXVzOiA0cHg7XFxuXFx0ZGlyZWN0aW9uOiBsdHI7XFxufVxcblxcbi5kYXRldGltZXBpY2tlci1pbmxpbmUge1xcblxcdHdpZHRoOiAyMjBweDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyLmRhdGV0aW1lcGlja2VyLXJ0bCB7XFxuXFx0ZGlyZWN0aW9uOiBydGw7XFxufVxcblxcbi5kYXRldGltZXBpY2tlci5kYXRldGltZXBpY2tlci1ydGwgdGFibGUgdHIgdGQgc3BhbiB7XFxuXFx0ZmxvYXQ6IHJpZ2h0O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXItZHJvcGRvd24sIC5kYXRldGltZXBpY2tlci1kcm9wZG93bi1sZWZ0IHtcXG5cXHR0b3A6IDA7XFxuXFx0bGVmdDogMDtcXG59XFxuXFxuW2NsYXNzKj1cXFwiIGRhdGV0aW1lcGlja2VyLWRyb3Bkb3duXFxcIl06YmVmb3JlIHtcXG5cXHRjb250ZW50OiAnJztcXG5cXHRkaXNwbGF5OiBpbmxpbmUtYmxvY2s7XFxuXFx0Ym9yZGVyLWxlZnQ6IDdweCBzb2xpZCB0cmFuc3BhcmVudDtcXG5cXHRib3JkZXItcmlnaHQ6IDdweCBzb2xpZCB0cmFuc3BhcmVudDtcXG5cXHRib3JkZXItYm90dG9tOiA3cHggc29saWQgI2NjY2NjYztcXG5cXHRib3JkZXItYm90dG9tLWNvbG9yOiByZ2JhKDAsIDAsIDAsIDAuMik7XFxuXFx0cG9zaXRpb246IGFic29sdXRlO1xcbn1cXG5cXG5bY2xhc3MqPVxcXCIgZGF0ZXRpbWVwaWNrZXItZHJvcGRvd25cXFwiXTphZnRlciB7XFxuXFx0Y29udGVudDogJyc7XFxuXFx0ZGlzcGxheTogaW5saW5lLWJsb2NrO1xcblxcdGJvcmRlci1sZWZ0OiA2cHggc29saWQgdHJhbnNwYXJlbnQ7XFxuXFx0Ym9yZGVyLXJpZ2h0OiA2cHggc29saWQgdHJhbnNwYXJlbnQ7XFxuXFx0Ym9yZGVyLWJvdHRvbTogNnB4IHNvbGlkICNmZmZmZmY7XFxuXFx0cG9zaXRpb246IGFic29sdXRlO1xcbn1cXG5cXG5bY2xhc3MqPVxcXCIgZGF0ZXRpbWVwaWNrZXItZHJvcGRvd24tdG9wXFxcIl06YmVmb3JlIHtcXG5cXHRjb250ZW50OiAnJztcXG5cXHRkaXNwbGF5OiBpbmxpbmUtYmxvY2s7XFxuXFx0Ym9yZGVyLWxlZnQ6IDdweCBzb2xpZCB0cmFuc3BhcmVudDtcXG5cXHRib3JkZXItcmlnaHQ6IDdweCBzb2xpZCB0cmFuc3BhcmVudDtcXG5cXHRib3JkZXItdG9wOiA3cHggc29saWQgI2NjY2NjYztcXG5cXHRib3JkZXItdG9wLWNvbG9yOiByZ2JhKDAsIDAsIDAsIDAuMik7XFxuXFx0Ym9yZGVyLWJvdHRvbTogMDtcXG59XFxuXFxuW2NsYXNzKj1cXFwiIGRhdGV0aW1lcGlja2VyLWRyb3Bkb3duLXRvcFxcXCJdOmFmdGVyIHtcXG5cXHRjb250ZW50OiAnJztcXG5cXHRkaXNwbGF5OiBpbmxpbmUtYmxvY2s7XFxuXFx0Ym9yZGVyLWxlZnQ6IDZweCBzb2xpZCB0cmFuc3BhcmVudDtcXG5cXHRib3JkZXItcmlnaHQ6IDZweCBzb2xpZCB0cmFuc3BhcmVudDtcXG5cXHRib3JkZXItdG9wOiA2cHggc29saWQgI2ZmZmZmZjtcXG5cXHRib3JkZXItYm90dG9tOiAwO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXItZHJvcGRvd24tYm90dG9tLWxlZnQ6YmVmb3JlIHtcXG5cXHR0b3A6IC03cHg7XFxuXFx0cmlnaHQ6IDZweDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyLWRyb3Bkb3duLWJvdHRvbS1sZWZ0OmFmdGVyIHtcXG5cXHR0b3A6IC02cHg7XFxuXFx0cmlnaHQ6IDdweDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyLWRyb3Bkb3duLWJvdHRvbS1yaWdodDpiZWZvcmUge1xcblxcdHRvcDogLTdweDtcXG5cXHRsZWZ0OiA2cHg7XFxufVxcblxcbi5kYXRldGltZXBpY2tlci1kcm9wZG93bi1ib3R0b20tcmlnaHQ6YWZ0ZXIge1xcblxcdHRvcDogLTZweDtcXG5cXHRsZWZ0OiA3cHg7XFxufVxcblxcbi5kYXRldGltZXBpY2tlci1kcm9wZG93bi10b3AtbGVmdDpiZWZvcmUge1xcblxcdGJvdHRvbTogLTdweDtcXG5cXHRyaWdodDogNnB4O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXItZHJvcGRvd24tdG9wLWxlZnQ6YWZ0ZXIge1xcblxcdGJvdHRvbTogLTZweDtcXG5cXHRyaWdodDogN3B4O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXItZHJvcGRvd24tdG9wLXJpZ2h0OmJlZm9yZSB7XFxuXFx0Ym90dG9tOiAtN3B4O1xcblxcdGxlZnQ6IDZweDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyLWRyb3Bkb3duLXRvcC1yaWdodDphZnRlciB7XFxuXFx0Ym90dG9tOiAtNnB4O1xcblxcdGxlZnQ6IDdweDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyID4gZGl2IHtcXG5cXHRkaXNwbGF5OiBub25lO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIubWludXRlcyBkaXYuZGF0ZXRpbWVwaWNrZXItbWludXRlcyB7XFxuXFx0ZGlzcGxheTogYmxvY2s7XFxufVxcblxcbi5kYXRldGltZXBpY2tlci5ob3VycyBkaXYuZGF0ZXRpbWVwaWNrZXItaG91cnMge1xcblxcdGRpc3BsYXk6IGJsb2NrO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIuZGF5cyBkaXYuZGF0ZXRpbWVwaWNrZXItZGF5cyB7XFxuXFx0ZGlzcGxheTogYmxvY2s7XFxufVxcblxcbi5kYXRldGltZXBpY2tlci5tb250aHMgZGl2LmRhdGV0aW1lcGlja2VyLW1vbnRocyB7XFxuXFx0ZGlzcGxheTogYmxvY2s7XFxufVxcblxcbi5kYXRldGltZXBpY2tlci55ZWFycyBkaXYuZGF0ZXRpbWVwaWNrZXIteWVhcnMge1xcblxcdGRpc3BsYXk6IGJsb2NrO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUge1xcblxcdG1hcmdpbjogMDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyICB0ZCxcXG4uZGF0ZXRpbWVwaWNrZXIgdGgge1xcblxcdHRleHQtYWxpZ246IGNlbnRlcjtcXG5cXHR3aWR0aDogMjBweDtcXG5cXHRoZWlnaHQ6IDIwcHg7XFxuXFx0LXdlYmtpdC1ib3JkZXItcmFkaXVzOiA0cHg7XFxuXFx0LW1vei1ib3JkZXItcmFkaXVzOiA0cHg7XFxuXFx0Ym9yZGVyLXJhZGl1czogNHB4O1xcblxcdGJvcmRlcjogbm9uZTtcXG59XFxuXFxuLnRhYmxlLXN0cmlwZWQgLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLFxcbi50YWJsZS1zdHJpcGVkIC5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0aCB7XFxuXFx0YmFja2dyb3VuZC1jb2xvcjogdHJhbnNwYXJlbnQ7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5taW51dGU6aG92ZXIge1xcblxcdGJhY2tncm91bmQ6ICNlZWVlZWU7XFxuXFx0Y3Vyc29yOiBwb2ludGVyO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuaG91cjpob3ZlciB7XFxuXFx0YmFja2dyb3VuZDogI2VlZWVlZTtcXG5cXHRjdXJzb3I6IHBvaW50ZXI7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5kYXk6aG92ZXIge1xcblxcdGJhY2tncm91bmQ6ICNlZWVlZWU7XFxuXFx0Y3Vyc29yOiBwb2ludGVyO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQub2xkLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5uZXcge1xcblxcdGNvbG9yOiAjOTk5OTk5O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuZGlzYWJsZWQsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmRpc2FibGVkOmhvdmVyIHtcXG5cXHRiYWNrZ3JvdW5kOiBub25lO1xcblxcdGNvbG9yOiAjOTk5OTk5O1xcblxcdGN1cnNvcjogZGVmYXVsdDtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5LFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheTpob3ZlcixcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXkuZGlzYWJsZWQsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5LmRpc2FibGVkOmhvdmVyIHtcXG5cXHRiYWNrZ3JvdW5kLWNvbG9yOiAjZmRlMTlhO1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IC1tb3otbGluZWFyLWdyYWRpZW50KHRvcCwgI2ZkZDQ5YSwgI2ZkZjU5YSk7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogLW1zLWxpbmVhci1ncmFkaWVudCh0b3AsICNmZGQ0OWEsICNmZGY1OWEpO1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IC13ZWJraXQtZ3JhZGllbnQobGluZWFyLCAwIDAsIDAgMTAwJSwgZnJvbSgjZmRkNDlhKSwgdG8oI2ZkZjU5YSkpO1xcblxcdGJhY2tncm91bmQtaW1hZ2U6IC13ZWJraXQtbGluZWFyLWdyYWRpZW50KHRvcCwgI2ZkZDQ5YSwgI2ZkZjU5YSk7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogLW8tbGluZWFyLWdyYWRpZW50KHRvcCwgI2ZkZDQ5YSwgI2ZkZjU5YSk7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogbGluZWFyLWdyYWRpZW50KHRvIGJvdHRvbSwgI2ZkZDQ5YSwgI2ZkZjU5YSk7XFxuXFx0YmFja2dyb3VuZC1yZXBlYXQ6IHJlcGVhdC14O1xcblxcdGZpbHRlcjogcHJvZ2lkOkRYSW1hZ2VUcmFuc2Zvcm0uTWljcm9zb2Z0LmdyYWRpZW50KHN0YXJ0Q29sb3JzdHI9JyNmZGQ0OWEnLCBlbmRDb2xvcnN0cj0nI2ZkZjU5YScsIEdyYWRpZW50VHlwZT0wKTtcXG5cXHRib3JkZXItY29sb3I6ICNmZGY1OWEgI2ZkZjU5YSAjZmJlZDUwO1xcblxcdGJvcmRlci1jb2xvcjogcmdiYSgwLCAwLCAwLCAwLjEpIHJnYmEoMCwgMCwgMCwgMC4xKSByZ2JhKDAsIDAsIDAsIDAuMjUpO1xcblxcdGZpbHRlcjogcHJvZ2lkOkRYSW1hZ2VUcmFuc2Zvcm0uTWljcm9zb2Z0LmdyYWRpZW50KGVuYWJsZWQ9ZmFsc2UpO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXk6aG92ZXIsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5OmhvdmVyOmhvdmVyLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheS5kaXNhYmxlZDpob3ZlcixcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXkuZGlzYWJsZWQ6aG92ZXI6aG92ZXIsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5OmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXk6aG92ZXI6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheS5kaXNhYmxlZDphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5LmRpc2FibGVkOmhvdmVyOmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXkuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheTpob3Zlci5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5LmRpc2FibGVkLmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXkuZGlzYWJsZWQ6aG92ZXIuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheS5kaXNhYmxlZCxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXk6aG92ZXIuZGlzYWJsZWQsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5LmRpc2FibGVkLmRpc2FibGVkLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheS5kaXNhYmxlZDpob3Zlci5kaXNhYmxlZCxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXlbZGlzYWJsZWRdLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheTpob3ZlcltkaXNhYmxlZF0sXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5LmRpc2FibGVkW2Rpc2FibGVkXSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXkuZGlzYWJsZWQ6aG92ZXJbZGlzYWJsZWRdIHtcXG5cXHRiYWNrZ3JvdW5kLWNvbG9yOiAjZmRmNTlhO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXk6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheTpob3ZlcjphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5LmRpc2FibGVkOmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXkuZGlzYWJsZWQ6aG92ZXI6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheS5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLnRvZGF5OmhvdmVyLmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQudG9kYXkuZGlzYWJsZWQuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC50b2RheS5kaXNhYmxlZDpob3Zlci5hY3RpdmUge1xcblxcdGJhY2tncm91bmQtY29sb3I6ICNmYmYwNjk7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZTpob3ZlcixcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuYWN0aXZlLmRpc2FibGVkLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuZGlzYWJsZWQ6aG92ZXIge1xcblxcdGJhY2tncm91bmQtY29sb3I6ICMwMDZkY2M7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogLW1vei1saW5lYXItZ3JhZGllbnQodG9wLCAjMDA4OGNjLCAjMDA0NGNjKTtcXG5cXHRiYWNrZ3JvdW5kLWltYWdlOiAtbXMtbGluZWFyLWdyYWRpZW50KHRvcCwgIzAwODhjYywgIzAwNDRjYyk7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogLXdlYmtpdC1ncmFkaWVudChsaW5lYXIsIDAgMCwgMCAxMDAlLCBmcm9tKCMwMDg4Y2MpLCB0bygjMDA0NGNjKSk7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogLXdlYmtpdC1saW5lYXItZ3JhZGllbnQodG9wLCAjMDA4OGNjLCAjMDA0NGNjKTtcXG5cXHRiYWNrZ3JvdW5kLWltYWdlOiAtby1saW5lYXItZ3JhZGllbnQodG9wLCAjMDA4OGNjLCAjMDA0NGNjKTtcXG5cXHRiYWNrZ3JvdW5kLWltYWdlOiBsaW5lYXItZ3JhZGllbnQodG8gYm90dG9tLCAjMDA4OGNjLCAjMDA0NGNjKTtcXG5cXHRiYWNrZ3JvdW5kLXJlcGVhdDogcmVwZWF0LXg7XFxuXFx0ZmlsdGVyOiBwcm9naWQ6RFhJbWFnZVRyYW5zZm9ybS5NaWNyb3NvZnQuZ3JhZGllbnQoc3RhcnRDb2xvcnN0cj0nIzAwODhjYycsIGVuZENvbG9yc3RyPScjMDA0NGNjJywgR3JhZGllbnRUeXBlPTApO1xcblxcdGJvcmRlci1jb2xvcjogIzAwNDRjYyAjMDA0NGNjICMwMDJhODA7XFxuXFx0Ym9yZGVyLWNvbG9yOiByZ2JhKDAsIDAsIDAsIDAuMSkgcmdiYSgwLCAwLCAwLCAwLjEpIHJnYmEoMCwgMCwgMCwgMC4yNSk7XFxuXFx0ZmlsdGVyOiBwcm9naWQ6RFhJbWFnZVRyYW5zZm9ybS5NaWNyb3NvZnQuZ3JhZGllbnQoZW5hYmxlZD1mYWxzZSk7XFxuXFx0Y29sb3I6ICNmZmZmZmY7XFxuXFx0dGV4dC1zaGFkb3c6IDAgLTFweCAwIHJnYmEoMCwgMCwgMCwgMC4yNSk7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmU6aG92ZXIsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZTpob3Zlcjpob3ZlcixcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuYWN0aXZlLmRpc2FibGVkOmhvdmVyLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuZGlzYWJsZWQ6aG92ZXI6aG92ZXIsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZTphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZTpob3ZlcjphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5kaXNhYmxlZDphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5kaXNhYmxlZDpob3ZlcjphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZTpob3Zlci5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5kaXNhYmxlZC5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5kaXNhYmxlZDpob3Zlci5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5kaXNhYmxlZCxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuYWN0aXZlOmhvdmVyLmRpc2FibGVkLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZC5hY3RpdmUuZGlzYWJsZWQuZGlzYWJsZWQsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5kaXNhYmxlZDpob3Zlci5kaXNhYmxlZCxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuYWN0aXZlW2Rpc2FibGVkXSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuYWN0aXZlOmhvdmVyW2Rpc2FibGVkXSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuYWN0aXZlLmRpc2FibGVkW2Rpc2FibGVkXSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQuYWN0aXZlLmRpc2FibGVkOmhvdmVyW2Rpc2FibGVkXSB7XFxuXFx0YmFja2dyb3VuZC1jb2xvcjogIzAwNDRjYztcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZTphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZTpob3ZlcjphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5kaXNhYmxlZDphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5kaXNhYmxlZDpob3ZlcjphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZTpob3Zlci5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5kaXNhYmxlZC5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkLmFjdGl2ZS5kaXNhYmxlZDpob3Zlci5hY3RpdmUge1xcblxcdGJhY2tncm91bmQtY29sb3I6ICMwMDMzOTk7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuIHtcXG5cXHRkaXNwbGF5OiBibG9jaztcXG5cXHR3aWR0aDogMjMlO1xcblxcdGhlaWdodDogNTRweDtcXG5cXHRsaW5lLWhlaWdodDogNTRweDtcXG5cXHRmbG9hdDogbGVmdDtcXG5cXHRtYXJnaW46IDElO1xcblxcdGN1cnNvcjogcG9pbnRlcjtcXG5cXHQtd2Via2l0LWJvcmRlci1yYWRpdXM6IDRweDtcXG5cXHQtbW96LWJvcmRlci1yYWRpdXM6IDRweDtcXG5cXHRib3JkZXItcmFkaXVzOiA0cHg7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciAuZGF0ZXRpbWVwaWNrZXItaG91cnMgc3BhbiB7XFxuXFx0aGVpZ2h0OiAyNnB4O1xcblxcdGxpbmUtaGVpZ2h0OiAyNnB4O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgLmRhdGV0aW1lcGlja2VyLWhvdXJzIHRhYmxlIHRyIHRkIHNwYW4uaG91cl9hbSxcXG4uZGF0ZXRpbWVwaWNrZXIgLmRhdGV0aW1lcGlja2VyLWhvdXJzIHRhYmxlIHRyIHRkIHNwYW4uaG91cl9wbSB7XFxuXFx0d2lkdGg6IDE0LjYlO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgLmRhdGV0aW1lcGlja2VyLWhvdXJzIGZpZWxkc2V0IGxlZ2VuZCxcXG4uZGF0ZXRpbWVwaWNrZXIgLmRhdGV0aW1lcGlja2VyLW1pbnV0ZXMgZmllbGRzZXQgbGVnZW5kIHtcXG5cXHRtYXJnaW4tYm90dG9tOiBpbmhlcml0O1xcblxcdGxpbmUtaGVpZ2h0OiAzMHB4O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgLmRhdGV0aW1lcGlja2VyLW1pbnV0ZXMgc3BhbiB7XFxuXFx0aGVpZ2h0OiAyNnB4O1xcblxcdGxpbmUtaGVpZ2h0OiAyNnB4O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbjpob3ZlciB7XFxuXFx0YmFja2dyb3VuZDogI2VlZWVlZTtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uZGlzYWJsZWQsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uZGlzYWJsZWQ6aG92ZXIge1xcblxcdGJhY2tncm91bmQ6IG5vbmU7XFxuXFx0Y29sb3I6ICM5OTk5OTk7XFxuXFx0Y3Vyc29yOiBkZWZhdWx0O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlOmhvdmVyLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZS5kaXNhYmxlZCxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmUuZGlzYWJsZWQ6aG92ZXIge1xcblxcdGJhY2tncm91bmQtY29sb3I6ICMwMDZkY2M7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogLW1vei1saW5lYXItZ3JhZGllbnQodG9wLCAjMDA4OGNjLCAjMDA0NGNjKTtcXG5cXHRiYWNrZ3JvdW5kLWltYWdlOiAtbXMtbGluZWFyLWdyYWRpZW50KHRvcCwgIzAwODhjYywgIzAwNDRjYyk7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogLXdlYmtpdC1ncmFkaWVudChsaW5lYXIsIDAgMCwgMCAxMDAlLCBmcm9tKCMwMDg4Y2MpLCB0bygjMDA0NGNjKSk7XFxuXFx0YmFja2dyb3VuZC1pbWFnZTogLXdlYmtpdC1saW5lYXItZ3JhZGllbnQodG9wLCAjMDA4OGNjLCAjMDA0NGNjKTtcXG5cXHRiYWNrZ3JvdW5kLWltYWdlOiAtby1saW5lYXItZ3JhZGllbnQodG9wLCAjMDA4OGNjLCAjMDA0NGNjKTtcXG5cXHRiYWNrZ3JvdW5kLWltYWdlOiBsaW5lYXItZ3JhZGllbnQodG8gYm90dG9tLCAjMDA4OGNjLCAjMDA0NGNjKTtcXG5cXHRiYWNrZ3JvdW5kLXJlcGVhdDogcmVwZWF0LXg7XFxuXFx0ZmlsdGVyOiBwcm9naWQ6RFhJbWFnZVRyYW5zZm9ybS5NaWNyb3NvZnQuZ3JhZGllbnQoc3RhcnRDb2xvcnN0cj0nIzAwODhjYycsIGVuZENvbG9yc3RyPScjMDA0NGNjJywgR3JhZGllbnRUeXBlPTApO1xcblxcdGJvcmRlci1jb2xvcjogIzAwNDRjYyAjMDA0NGNjICMwMDJhODA7XFxuXFx0Ym9yZGVyLWNvbG9yOiByZ2JhKDAsIDAsIDAsIDAuMSkgcmdiYSgwLCAwLCAwLCAwLjEpIHJnYmEoMCwgMCwgMCwgMC4yNSk7XFxuXFx0ZmlsdGVyOiBwcm9naWQ6RFhJbWFnZVRyYW5zZm9ybS5NaWNyb3NvZnQuZ3JhZGllbnQoZW5hYmxlZD1mYWxzZSk7XFxuXFx0Y29sb3I6ICNmZmZmZmY7XFxuXFx0dGV4dC1zaGFkb3c6IDAgLTFweCAwIHJnYmEoMCwgMCwgMCwgMC4yNSk7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZTpob3ZlcixcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmU6aG92ZXI6aG92ZXIsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlLmRpc2FibGVkOmhvdmVyLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZS5kaXNhYmxlZDpob3Zlcjpob3ZlcixcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmU6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZTpob3ZlcjphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlLmRpc2FibGVkOmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmUuZGlzYWJsZWQ6aG92ZXI6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZS5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlOmhvdmVyLmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmUuZGlzYWJsZWQuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZS5kaXNhYmxlZDpob3Zlci5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlLmRpc2FibGVkLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZTpob3Zlci5kaXNhYmxlZCxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmUuZGlzYWJsZWQuZGlzYWJsZWQsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlLmRpc2FibGVkOmhvdmVyLmRpc2FibGVkLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZVtkaXNhYmxlZF0sXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlOmhvdmVyW2Rpc2FibGVkXSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmUuZGlzYWJsZWRbZGlzYWJsZWRdLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZS5kaXNhYmxlZDpob3ZlcltkaXNhYmxlZF0ge1xcblxcdGJhY2tncm91bmQtY29sb3I6ICMwMDQ0Y2M7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZTphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlOmhvdmVyOmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmUuZGlzYWJsZWQ6YWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZS5kaXNhYmxlZDpob3ZlcjphY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlLmFjdGl2ZSxcXG4uZGF0ZXRpbWVwaWNrZXIgdGFibGUgdHIgdGQgc3Bhbi5hY3RpdmU6aG92ZXIuYWN0aXZlLFxcbi5kYXRldGltZXBpY2tlciB0YWJsZSB0ciB0ZCBzcGFuLmFjdGl2ZS5kaXNhYmxlZC5hY3RpdmUsXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4uYWN0aXZlLmRpc2FibGVkOmhvdmVyLmFjdGl2ZSB7XFxuXFx0YmFja2dyb3VuZC1jb2xvcjogIzAwMzM5OTtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRhYmxlIHRyIHRkIHNwYW4ub2xkIHtcXG5cXHRjb2xvcjogIzk5OTk5OTtcXG59XFxuXFxuLmRhdGV0aW1lcGlja2VyIHRoLnN3aXRjaCB7XFxuXFx0d2lkdGg6IDE0NXB4O1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGggc3Bhbi5nbHlwaGljb24ge1xcblxcdHBvaW50ZXItZXZlbnRzOiBub25lO1xcbn1cXG5cXG4uZGF0ZXRpbWVwaWNrZXIgdGhlYWQgdHI6Zmlyc3QtY2hpbGQgdGgsXFxuLmRhdGV0aW1lcGlja2VyIHRmb290IHRoIHtcXG5cXHRjdXJzb3I6IHBvaW50ZXI7XFxufVxcblxcbi5kYXRldGltZXBpY2tlciB0aGVhZCB0cjpmaXJzdC1jaGlsZCB0aDpob3ZlcixcXG4uZGF0ZXRpbWVwaWNrZXIgdGZvb3QgdGg6aG92ZXIge1xcblxcdGJhY2tncm91bmQ6ICNlZWVlZWU7XFxufVxcblxcbi5pbnB1dC1hcHBlbmQuZGF0ZSAuYWRkLW9uIGksXFxuLmlucHV0LXByZXBlbmQuZGF0ZSAuYWRkLW9uIGksXFxuLmlucHV0LWdyb3VwLmRhdGUgLmlucHV0LWdyb3VwLWFkZG9uIHNwYW4ge1xcblxcdGN1cnNvcjogcG9pbnRlcjtcXG5cXHR3aWR0aDogMTRweDtcXG5cXHRoZWlnaHQ6IDE0cHg7XFxufVxcblwiLCBcIlwiXSk7XG5cbi8vIGV4cG9ydHNcbiIsIi8qXG5cdE1JVCBMaWNlbnNlIGh0dHA6Ly93d3cub3BlbnNvdXJjZS5vcmcvbGljZW5zZXMvbWl0LWxpY2Vuc2UucGhwXG5cdEF1dGhvciBUb2JpYXMgS29wcGVycyBAc29rcmFcbiovXG4vLyBjc3MgYmFzZSBjb2RlLCBpbmplY3RlZCBieSB0aGUgY3NzLWxvYWRlclxubW9kdWxlLmV4cG9ydHMgPSBmdW5jdGlvbih1c2VTb3VyY2VNYXApIHtcblx0dmFyIGxpc3QgPSBbXTtcblxuXHQvLyByZXR1cm4gdGhlIGxpc3Qgb2YgbW9kdWxlcyBhcyBjc3Mgc3RyaW5nXG5cdGxpc3QudG9TdHJpbmcgPSBmdW5jdGlvbiB0b1N0cmluZygpIHtcblx0XHRyZXR1cm4gdGhpcy5tYXAoZnVuY3Rpb24gKGl0ZW0pIHtcblx0XHRcdHZhciBjb250ZW50ID0gY3NzV2l0aE1hcHBpbmdUb1N0cmluZyhpdGVtLCB1c2VTb3VyY2VNYXApO1xuXHRcdFx0aWYoaXRlbVsyXSkge1xuXHRcdFx0XHRyZXR1cm4gXCJAbWVkaWEgXCIgKyBpdGVtWzJdICsgXCJ7XCIgKyBjb250ZW50ICsgXCJ9XCI7XG5cdFx0XHR9IGVsc2Uge1xuXHRcdFx0XHRyZXR1cm4gY29udGVudDtcblx0XHRcdH1cblx0XHR9KS5qb2luKFwiXCIpO1xuXHR9O1xuXG5cdC8vIGltcG9ydCBhIGxpc3Qgb2YgbW9kdWxlcyBpbnRvIHRoZSBsaXN0XG5cdGxpc3QuaSA9IGZ1bmN0aW9uKG1vZHVsZXMsIG1lZGlhUXVlcnkpIHtcblx0XHRpZih0eXBlb2YgbW9kdWxlcyA9PT0gXCJzdHJpbmdcIilcblx0XHRcdG1vZHVsZXMgPSBbW251bGwsIG1vZHVsZXMsIFwiXCJdXTtcblx0XHR2YXIgYWxyZWFkeUltcG9ydGVkTW9kdWxlcyA9IHt9O1xuXHRcdGZvcih2YXIgaSA9IDA7IGkgPCB0aGlzLmxlbmd0aDsgaSsrKSB7XG5cdFx0XHR2YXIgaWQgPSB0aGlzW2ldWzBdO1xuXHRcdFx0aWYodHlwZW9mIGlkID09PSBcIm51bWJlclwiKVxuXHRcdFx0XHRhbHJlYWR5SW1wb3J0ZWRNb2R1bGVzW2lkXSA9IHRydWU7XG5cdFx0fVxuXHRcdGZvcihpID0gMDsgaSA8IG1vZHVsZXMubGVuZ3RoOyBpKyspIHtcblx0XHRcdHZhciBpdGVtID0gbW9kdWxlc1tpXTtcblx0XHRcdC8vIHNraXAgYWxyZWFkeSBpbXBvcnRlZCBtb2R1bGVcblx0XHRcdC8vIHRoaXMgaW1wbGVtZW50YXRpb24gaXMgbm90IDEwMCUgcGVyZmVjdCBmb3Igd2VpcmQgbWVkaWEgcXVlcnkgY29tYmluYXRpb25zXG5cdFx0XHQvLyAgd2hlbiBhIG1vZHVsZSBpcyBpbXBvcnRlZCBtdWx0aXBsZSB0aW1lcyB3aXRoIGRpZmZlcmVudCBtZWRpYSBxdWVyaWVzLlxuXHRcdFx0Ly8gIEkgaG9wZSB0aGlzIHdpbGwgbmV2ZXIgb2NjdXIgKEhleSB0aGlzIHdheSB3ZSBoYXZlIHNtYWxsZXIgYnVuZGxlcylcblx0XHRcdGlmKHR5cGVvZiBpdGVtWzBdICE9PSBcIm51bWJlclwiIHx8ICFhbHJlYWR5SW1wb3J0ZWRNb2R1bGVzW2l0ZW1bMF1dKSB7XG5cdFx0XHRcdGlmKG1lZGlhUXVlcnkgJiYgIWl0ZW1bMl0pIHtcblx0XHRcdFx0XHRpdGVtWzJdID0gbWVkaWFRdWVyeTtcblx0XHRcdFx0fSBlbHNlIGlmKG1lZGlhUXVlcnkpIHtcblx0XHRcdFx0XHRpdGVtWzJdID0gXCIoXCIgKyBpdGVtWzJdICsgXCIpIGFuZCAoXCIgKyBtZWRpYVF1ZXJ5ICsgXCIpXCI7XG5cdFx0XHRcdH1cblx0XHRcdFx0bGlzdC5wdXNoKGl0ZW0pO1xuXHRcdFx0fVxuXHRcdH1cblx0fTtcblx0cmV0dXJuIGxpc3Q7XG59O1xuXG5mdW5jdGlvbiBjc3NXaXRoTWFwcGluZ1RvU3RyaW5nKGl0ZW0sIHVzZVNvdXJjZU1hcCkge1xuXHR2YXIgY29udGVudCA9IGl0ZW1bMV0gfHwgJyc7XG5cdHZhciBjc3NNYXBwaW5nID0gaXRlbVszXTtcblx0aWYgKCFjc3NNYXBwaW5nKSB7XG5cdFx0cmV0dXJuIGNvbnRlbnQ7XG5cdH1cblxuXHRpZiAodXNlU291cmNlTWFwICYmIHR5cGVvZiBidG9hID09PSAnZnVuY3Rpb24nKSB7XG5cdFx0dmFyIHNvdXJjZU1hcHBpbmcgPSB0b0NvbW1lbnQoY3NzTWFwcGluZyk7XG5cdFx0dmFyIHNvdXJjZVVSTHMgPSBjc3NNYXBwaW5nLnNvdXJjZXMubWFwKGZ1bmN0aW9uIChzb3VyY2UpIHtcblx0XHRcdHJldHVybiAnLyojIHNvdXJjZVVSTD0nICsgY3NzTWFwcGluZy5zb3VyY2VSb290ICsgc291cmNlICsgJyAqLydcblx0XHR9KTtcblxuXHRcdHJldHVybiBbY29udGVudF0uY29uY2F0KHNvdXJjZVVSTHMpLmNvbmNhdChbc291cmNlTWFwcGluZ10pLmpvaW4oJ1xcbicpO1xuXHR9XG5cblx0cmV0dXJuIFtjb250ZW50XS5qb2luKCdcXG4nKTtcbn1cblxuLy8gQWRhcHRlZCBmcm9tIGNvbnZlcnQtc291cmNlLW1hcCAoTUlUKVxuZnVuY3Rpb24gdG9Db21tZW50KHNvdXJjZU1hcCkge1xuXHQvLyBlc2xpbnQtZGlzYWJsZS1uZXh0LWxpbmUgbm8tdW5kZWZcblx0dmFyIGJhc2U2NCA9IGJ0b2EodW5lc2NhcGUoZW5jb2RlVVJJQ29tcG9uZW50KEpTT04uc3RyaW5naWZ5KHNvdXJjZU1hcCkpKSk7XG5cdHZhciBkYXRhID0gJ3NvdXJjZU1hcHBpbmdVUkw9ZGF0YTphcHBsaWNhdGlvbi9qc29uO2NoYXJzZXQ9dXRmLTg7YmFzZTY0LCcgKyBiYXNlNjQ7XG5cblx0cmV0dXJuICcvKiMgJyArIGRhdGEgKyAnICovJztcbn1cbiIsIi8qXG5cdE1JVCBMaWNlbnNlIGh0dHA6Ly93d3cub3BlbnNvdXJjZS5vcmcvbGljZW5zZXMvbWl0LWxpY2Vuc2UucGhwXG5cdEF1dGhvciBUb2JpYXMgS29wcGVycyBAc29rcmFcbiovXG52YXIgc3R5bGVzSW5Eb20gPSB7fSxcblx0bWVtb2l6ZSA9IGZ1bmN0aW9uKGZuKSB7XG5cdFx0dmFyIG1lbW87XG5cdFx0cmV0dXJuIGZ1bmN0aW9uICgpIHtcblx0XHRcdGlmICh0eXBlb2YgbWVtbyA9PT0gXCJ1bmRlZmluZWRcIikgbWVtbyA9IGZuLmFwcGx5KHRoaXMsIGFyZ3VtZW50cyk7XG5cdFx0XHRyZXR1cm4gbWVtbztcblx0XHR9O1xuXHR9LFxuXHRpc09sZElFID0gbWVtb2l6ZShmdW5jdGlvbigpIHtcblx0XHRyZXR1cm4gL21zaWUgWzYtOV1cXGIvLnRlc3Qoc2VsZi5uYXZpZ2F0b3IudXNlckFnZW50LnRvTG93ZXJDYXNlKCkpO1xuXHR9KSxcblx0Z2V0SGVhZEVsZW1lbnQgPSBtZW1vaXplKGZ1bmN0aW9uICgpIHtcblx0XHRyZXR1cm4gZG9jdW1lbnQuaGVhZCB8fCBkb2N1bWVudC5nZXRFbGVtZW50c0J5VGFnTmFtZShcImhlYWRcIilbMF07XG5cdH0pLFxuXHRzaW5nbGV0b25FbGVtZW50ID0gbnVsbCxcblx0c2luZ2xldG9uQ291bnRlciA9IDAsXG5cdHN0eWxlRWxlbWVudHNJbnNlcnRlZEF0VG9wID0gW107XG5cbm1vZHVsZS5leHBvcnRzID0gZnVuY3Rpb24obGlzdCwgb3B0aW9ucykge1xuXHRpZih0eXBlb2YgREVCVUcgIT09IFwidW5kZWZpbmVkXCIgJiYgREVCVUcpIHtcblx0XHRpZih0eXBlb2YgZG9jdW1lbnQgIT09IFwib2JqZWN0XCIpIHRocm93IG5ldyBFcnJvcihcIlRoZSBzdHlsZS1sb2FkZXIgY2Fubm90IGJlIHVzZWQgaW4gYSBub24tYnJvd3NlciBlbnZpcm9ubWVudFwiKTtcblx0fVxuXG5cdG9wdGlvbnMgPSBvcHRpb25zIHx8IHt9O1xuXHQvLyBGb3JjZSBzaW5nbGUtdGFnIHNvbHV0aW9uIG9uIElFNi05LCB3aGljaCBoYXMgYSBoYXJkIGxpbWl0IG9uIHRoZSAjIG9mIDxzdHlsZT5cblx0Ly8gdGFncyBpdCB3aWxsIGFsbG93IG9uIGEgcGFnZVxuXHRpZiAodHlwZW9mIG9wdGlvbnMuc2luZ2xldG9uID09PSBcInVuZGVmaW5lZFwiKSBvcHRpb25zLnNpbmdsZXRvbiA9IGlzT2xkSUUoKTtcblxuXHQvLyBCeSBkZWZhdWx0LCBhZGQgPHN0eWxlPiB0YWdzIHRvIHRoZSBib3R0b20gb2YgPGhlYWQ+LlxuXHRpZiAodHlwZW9mIG9wdGlvbnMuaW5zZXJ0QXQgPT09IFwidW5kZWZpbmVkXCIpIG9wdGlvbnMuaW5zZXJ0QXQgPSBcImJvdHRvbVwiO1xuXG5cdHZhciBzdHlsZXMgPSBsaXN0VG9TdHlsZXMobGlzdCk7XG5cdGFkZFN0eWxlc1RvRG9tKHN0eWxlcywgb3B0aW9ucyk7XG5cblx0cmV0dXJuIGZ1bmN0aW9uIHVwZGF0ZShuZXdMaXN0KSB7XG5cdFx0dmFyIG1heVJlbW92ZSA9IFtdO1xuXHRcdGZvcih2YXIgaSA9IDA7IGkgPCBzdHlsZXMubGVuZ3RoOyBpKyspIHtcblx0XHRcdHZhciBpdGVtID0gc3R5bGVzW2ldO1xuXHRcdFx0dmFyIGRvbVN0eWxlID0gc3R5bGVzSW5Eb21baXRlbS5pZF07XG5cdFx0XHRkb21TdHlsZS5yZWZzLS07XG5cdFx0XHRtYXlSZW1vdmUucHVzaChkb21TdHlsZSk7XG5cdFx0fVxuXHRcdGlmKG5ld0xpc3QpIHtcblx0XHRcdHZhciBuZXdTdHlsZXMgPSBsaXN0VG9TdHlsZXMobmV3TGlzdCk7XG5cdFx0XHRhZGRTdHlsZXNUb0RvbShuZXdTdHlsZXMsIG9wdGlvbnMpO1xuXHRcdH1cblx0XHRmb3IodmFyIGkgPSAwOyBpIDwgbWF5UmVtb3ZlLmxlbmd0aDsgaSsrKSB7XG5cdFx0XHR2YXIgZG9tU3R5bGUgPSBtYXlSZW1vdmVbaV07XG5cdFx0XHRpZihkb21TdHlsZS5yZWZzID09PSAwKSB7XG5cdFx0XHRcdGZvcih2YXIgaiA9IDA7IGogPCBkb21TdHlsZS5wYXJ0cy5sZW5ndGg7IGorKylcblx0XHRcdFx0XHRkb21TdHlsZS5wYXJ0c1tqXSgpO1xuXHRcdFx0XHRkZWxldGUgc3R5bGVzSW5Eb21bZG9tU3R5bGUuaWRdO1xuXHRcdFx0fVxuXHRcdH1cblx0fTtcbn1cblxuZnVuY3Rpb24gYWRkU3R5bGVzVG9Eb20oc3R5bGVzLCBvcHRpb25zKSB7XG5cdGZvcih2YXIgaSA9IDA7IGkgPCBzdHlsZXMubGVuZ3RoOyBpKyspIHtcblx0XHR2YXIgaXRlbSA9IHN0eWxlc1tpXTtcblx0XHR2YXIgZG9tU3R5bGUgPSBzdHlsZXNJbkRvbVtpdGVtLmlkXTtcblx0XHRpZihkb21TdHlsZSkge1xuXHRcdFx0ZG9tU3R5bGUucmVmcysrO1xuXHRcdFx0Zm9yKHZhciBqID0gMDsgaiA8IGRvbVN0eWxlLnBhcnRzLmxlbmd0aDsgaisrKSB7XG5cdFx0XHRcdGRvbVN0eWxlLnBhcnRzW2pdKGl0ZW0ucGFydHNbal0pO1xuXHRcdFx0fVxuXHRcdFx0Zm9yKDsgaiA8IGl0ZW0ucGFydHMubGVuZ3RoOyBqKyspIHtcblx0XHRcdFx0ZG9tU3R5bGUucGFydHMucHVzaChhZGRTdHlsZShpdGVtLnBhcnRzW2pdLCBvcHRpb25zKSk7XG5cdFx0XHR9XG5cdFx0fSBlbHNlIHtcblx0XHRcdHZhciBwYXJ0cyA9IFtdO1xuXHRcdFx0Zm9yKHZhciBqID0gMDsgaiA8IGl0ZW0ucGFydHMubGVuZ3RoOyBqKyspIHtcblx0XHRcdFx0cGFydHMucHVzaChhZGRTdHlsZShpdGVtLnBhcnRzW2pdLCBvcHRpb25zKSk7XG5cdFx0XHR9XG5cdFx0XHRzdHlsZXNJbkRvbVtpdGVtLmlkXSA9IHtpZDogaXRlbS5pZCwgcmVmczogMSwgcGFydHM6IHBhcnRzfTtcblx0XHR9XG5cdH1cbn1cblxuZnVuY3Rpb24gbGlzdFRvU3R5bGVzKGxpc3QpIHtcblx0dmFyIHN0eWxlcyA9IFtdO1xuXHR2YXIgbmV3U3R5bGVzID0ge307XG5cdGZvcih2YXIgaSA9IDA7IGkgPCBsaXN0Lmxlbmd0aDsgaSsrKSB7XG5cdFx0dmFyIGl0ZW0gPSBsaXN0W2ldO1xuXHRcdHZhciBpZCA9IGl0ZW1bMF07XG5cdFx0dmFyIGNzcyA9IGl0ZW1bMV07XG5cdFx0dmFyIG1lZGlhID0gaXRlbVsyXTtcblx0XHR2YXIgc291cmNlTWFwID0gaXRlbVszXTtcblx0XHR2YXIgcGFydCA9IHtjc3M6IGNzcywgbWVkaWE6IG1lZGlhLCBzb3VyY2VNYXA6IHNvdXJjZU1hcH07XG5cdFx0aWYoIW5ld1N0eWxlc1tpZF0pXG5cdFx0XHRzdHlsZXMucHVzaChuZXdTdHlsZXNbaWRdID0ge2lkOiBpZCwgcGFydHM6IFtwYXJ0XX0pO1xuXHRcdGVsc2Vcblx0XHRcdG5ld1N0eWxlc1tpZF0ucGFydHMucHVzaChwYXJ0KTtcblx0fVxuXHRyZXR1cm4gc3R5bGVzO1xufVxuXG5mdW5jdGlvbiBpbnNlcnRTdHlsZUVsZW1lbnQob3B0aW9ucywgc3R5bGVFbGVtZW50KSB7XG5cdHZhciBoZWFkID0gZ2V0SGVhZEVsZW1lbnQoKTtcblx0dmFyIGxhc3RTdHlsZUVsZW1lbnRJbnNlcnRlZEF0VG9wID0gc3R5bGVFbGVtZW50c0luc2VydGVkQXRUb3Bbc3R5bGVFbGVtZW50c0luc2VydGVkQXRUb3AubGVuZ3RoIC0gMV07XG5cdGlmIChvcHRpb25zLmluc2VydEF0ID09PSBcInRvcFwiKSB7XG5cdFx0aWYoIWxhc3RTdHlsZUVsZW1lbnRJbnNlcnRlZEF0VG9wKSB7XG5cdFx0XHRoZWFkLmluc2VydEJlZm9yZShzdHlsZUVsZW1lbnQsIGhlYWQuZmlyc3RDaGlsZCk7XG5cdFx0fSBlbHNlIGlmKGxhc3RTdHlsZUVsZW1lbnRJbnNlcnRlZEF0VG9wLm5leHRTaWJsaW5nKSB7XG5cdFx0XHRoZWFkLmluc2VydEJlZm9yZShzdHlsZUVsZW1lbnQsIGxhc3RTdHlsZUVsZW1lbnRJbnNlcnRlZEF0VG9wLm5leHRTaWJsaW5nKTtcblx0XHR9IGVsc2Uge1xuXHRcdFx0aGVhZC5hcHBlbmRDaGlsZChzdHlsZUVsZW1lbnQpO1xuXHRcdH1cblx0XHRzdHlsZUVsZW1lbnRzSW5zZXJ0ZWRBdFRvcC5wdXNoKHN0eWxlRWxlbWVudCk7XG5cdH0gZWxzZSBpZiAob3B0aW9ucy5pbnNlcnRBdCA9PT0gXCJib3R0b21cIikge1xuXHRcdGhlYWQuYXBwZW5kQ2hpbGQoc3R5bGVFbGVtZW50KTtcblx0fSBlbHNlIHtcblx0XHR0aHJvdyBuZXcgRXJyb3IoXCJJbnZhbGlkIHZhbHVlIGZvciBwYXJhbWV0ZXIgJ2luc2VydEF0Jy4gTXVzdCBiZSAndG9wJyBvciAnYm90dG9tJy5cIik7XG5cdH1cbn1cblxuZnVuY3Rpb24gcmVtb3ZlU3R5bGVFbGVtZW50KHN0eWxlRWxlbWVudCkge1xuXHRzdHlsZUVsZW1lbnQucGFyZW50Tm9kZS5yZW1vdmVDaGlsZChzdHlsZUVsZW1lbnQpO1xuXHR2YXIgaWR4ID0gc3R5bGVFbGVtZW50c0luc2VydGVkQXRUb3AuaW5kZXhPZihzdHlsZUVsZW1lbnQpO1xuXHRpZihpZHggPj0gMCkge1xuXHRcdHN0eWxlRWxlbWVudHNJbnNlcnRlZEF0VG9wLnNwbGljZShpZHgsIDEpO1xuXHR9XG59XG5cbmZ1bmN0aW9uIGNyZWF0ZVN0eWxlRWxlbWVudChvcHRpb25zKSB7XG5cdHZhciBzdHlsZUVsZW1lbnQgPSBkb2N1bWVudC5jcmVhdGVFbGVtZW50KFwic3R5bGVcIik7XG5cdHN0eWxlRWxlbWVudC50eXBlID0gXCJ0ZXh0L2Nzc1wiO1xuXHRpbnNlcnRTdHlsZUVsZW1lbnQob3B0aW9ucywgc3R5bGVFbGVtZW50KTtcblx0cmV0dXJuIHN0eWxlRWxlbWVudDtcbn1cblxuZnVuY3Rpb24gY3JlYXRlTGlua0VsZW1lbnQob3B0aW9ucykge1xuXHR2YXIgbGlua0VsZW1lbnQgPSBkb2N1bWVudC5jcmVhdGVFbGVtZW50KFwibGlua1wiKTtcblx0bGlua0VsZW1lbnQucmVsID0gXCJzdHlsZXNoZWV0XCI7XG5cdGluc2VydFN0eWxlRWxlbWVudChvcHRpb25zLCBsaW5rRWxlbWVudCk7XG5cdHJldHVybiBsaW5rRWxlbWVudDtcbn1cblxuZnVuY3Rpb24gYWRkU3R5bGUob2JqLCBvcHRpb25zKSB7XG5cdHZhciBzdHlsZUVsZW1lbnQsIHVwZGF0ZSwgcmVtb3ZlO1xuXG5cdGlmIChvcHRpb25zLnNpbmdsZXRvbikge1xuXHRcdHZhciBzdHlsZUluZGV4ID0gc2luZ2xldG9uQ291bnRlcisrO1xuXHRcdHN0eWxlRWxlbWVudCA9IHNpbmdsZXRvbkVsZW1lbnQgfHwgKHNpbmdsZXRvbkVsZW1lbnQgPSBjcmVhdGVTdHlsZUVsZW1lbnQob3B0aW9ucykpO1xuXHRcdHVwZGF0ZSA9IGFwcGx5VG9TaW5nbGV0b25UYWcuYmluZChudWxsLCBzdHlsZUVsZW1lbnQsIHN0eWxlSW5kZXgsIGZhbHNlKTtcblx0XHRyZW1vdmUgPSBhcHBseVRvU2luZ2xldG9uVGFnLmJpbmQobnVsbCwgc3R5bGVFbGVtZW50LCBzdHlsZUluZGV4LCB0cnVlKTtcblx0fSBlbHNlIGlmKG9iai5zb3VyY2VNYXAgJiZcblx0XHR0eXBlb2YgVVJMID09PSBcImZ1bmN0aW9uXCIgJiZcblx0XHR0eXBlb2YgVVJMLmNyZWF0ZU9iamVjdFVSTCA9PT0gXCJmdW5jdGlvblwiICYmXG5cdFx0dHlwZW9mIFVSTC5yZXZva2VPYmplY3RVUkwgPT09IFwiZnVuY3Rpb25cIiAmJlxuXHRcdHR5cGVvZiBCbG9iID09PSBcImZ1bmN0aW9uXCIgJiZcblx0XHR0eXBlb2YgYnRvYSA9PT0gXCJmdW5jdGlvblwiKSB7XG5cdFx0c3R5bGVFbGVtZW50ID0gY3JlYXRlTGlua0VsZW1lbnQob3B0aW9ucyk7XG5cdFx0dXBkYXRlID0gdXBkYXRlTGluay5iaW5kKG51bGwsIHN0eWxlRWxlbWVudCk7XG5cdFx0cmVtb3ZlID0gZnVuY3Rpb24oKSB7XG5cdFx0XHRyZW1vdmVTdHlsZUVsZW1lbnQoc3R5bGVFbGVtZW50KTtcblx0XHRcdGlmKHN0eWxlRWxlbWVudC5ocmVmKVxuXHRcdFx0XHRVUkwucmV2b2tlT2JqZWN0VVJMKHN0eWxlRWxlbWVudC5ocmVmKTtcblx0XHR9O1xuXHR9IGVsc2Uge1xuXHRcdHN0eWxlRWxlbWVudCA9IGNyZWF0ZVN0eWxlRWxlbWVudChvcHRpb25zKTtcblx0XHR1cGRhdGUgPSBhcHBseVRvVGFnLmJpbmQobnVsbCwgc3R5bGVFbGVtZW50KTtcblx0XHRyZW1vdmUgPSBmdW5jdGlvbigpIHtcblx0XHRcdHJlbW92ZVN0eWxlRWxlbWVudChzdHlsZUVsZW1lbnQpO1xuXHRcdH07XG5cdH1cblxuXHR1cGRhdGUob2JqKTtcblxuXHRyZXR1cm4gZnVuY3Rpb24gdXBkYXRlU3R5bGUobmV3T2JqKSB7XG5cdFx0aWYobmV3T2JqKSB7XG5cdFx0XHRpZihuZXdPYmouY3NzID09PSBvYmouY3NzICYmIG5ld09iai5tZWRpYSA9PT0gb2JqLm1lZGlhICYmIG5ld09iai5zb3VyY2VNYXAgPT09IG9iai5zb3VyY2VNYXApXG5cdFx0XHRcdHJldHVybjtcblx0XHRcdHVwZGF0ZShvYmogPSBuZXdPYmopO1xuXHRcdH0gZWxzZSB7XG5cdFx0XHRyZW1vdmUoKTtcblx0XHR9XG5cdH07XG59XG5cbnZhciByZXBsYWNlVGV4dCA9IChmdW5jdGlvbiAoKSB7XG5cdHZhciB0ZXh0U3RvcmUgPSBbXTtcblxuXHRyZXR1cm4gZnVuY3Rpb24gKGluZGV4LCByZXBsYWNlbWVudCkge1xuXHRcdHRleHRTdG9yZVtpbmRleF0gPSByZXBsYWNlbWVudDtcblx0XHRyZXR1cm4gdGV4dFN0b3JlLmZpbHRlcihCb29sZWFuKS5qb2luKCdcXG4nKTtcblx0fTtcbn0pKCk7XG5cbmZ1bmN0aW9uIGFwcGx5VG9TaW5nbGV0b25UYWcoc3R5bGVFbGVtZW50LCBpbmRleCwgcmVtb3ZlLCBvYmopIHtcblx0dmFyIGNzcyA9IHJlbW92ZSA/IFwiXCIgOiBvYmouY3NzO1xuXG5cdGlmIChzdHlsZUVsZW1lbnQuc3R5bGVTaGVldCkge1xuXHRcdHN0eWxlRWxlbWVudC5zdHlsZVNoZWV0LmNzc1RleHQgPSByZXBsYWNlVGV4dChpbmRleCwgY3NzKTtcblx0fSBlbHNlIHtcblx0XHR2YXIgY3NzTm9kZSA9IGRvY3VtZW50LmNyZWF0ZVRleHROb2RlKGNzcyk7XG5cdFx0dmFyIGNoaWxkTm9kZXMgPSBzdHlsZUVsZW1lbnQuY2hpbGROb2Rlcztcblx0XHRpZiAoY2hpbGROb2Rlc1tpbmRleF0pIHN0eWxlRWxlbWVudC5yZW1vdmVDaGlsZChjaGlsZE5vZGVzW2luZGV4XSk7XG5cdFx0aWYgKGNoaWxkTm9kZXMubGVuZ3RoKSB7XG5cdFx0XHRzdHlsZUVsZW1lbnQuaW5zZXJ0QmVmb3JlKGNzc05vZGUsIGNoaWxkTm9kZXNbaW5kZXhdKTtcblx0XHR9IGVsc2Uge1xuXHRcdFx0c3R5bGVFbGVtZW50LmFwcGVuZENoaWxkKGNzc05vZGUpO1xuXHRcdH1cblx0fVxufVxuXG5mdW5jdGlvbiBhcHBseVRvVGFnKHN0eWxlRWxlbWVudCwgb2JqKSB7XG5cdHZhciBjc3MgPSBvYmouY3NzO1xuXHR2YXIgbWVkaWEgPSBvYmoubWVkaWE7XG5cblx0aWYobWVkaWEpIHtcblx0XHRzdHlsZUVsZW1lbnQuc2V0QXR0cmlidXRlKFwibWVkaWFcIiwgbWVkaWEpXG5cdH1cblxuXHRpZihzdHlsZUVsZW1lbnQuc3R5bGVTaGVldCkge1xuXHRcdHN0eWxlRWxlbWVudC5zdHlsZVNoZWV0LmNzc1RleHQgPSBjc3M7XG5cdH0gZWxzZSB7XG5cdFx0d2hpbGUoc3R5bGVFbGVtZW50LmZpcnN0Q2hpbGQpIHtcblx0XHRcdHN0eWxlRWxlbWVudC5yZW1vdmVDaGlsZChzdHlsZUVsZW1lbnQuZmlyc3RDaGlsZCk7XG5cdFx0fVxuXHRcdHN0eWxlRWxlbWVudC5hcHBlbmRDaGlsZChkb2N1bWVudC5jcmVhdGVUZXh0Tm9kZShjc3MpKTtcblx0fVxufVxuXG5mdW5jdGlvbiB1cGRhdGVMaW5rKGxpbmtFbGVtZW50LCBvYmopIHtcblx0dmFyIGNzcyA9IG9iai5jc3M7XG5cdHZhciBzb3VyY2VNYXAgPSBvYmouc291cmNlTWFwO1xuXG5cdGlmKHNvdXJjZU1hcCkge1xuXHRcdC8vIGh0dHA6Ly9zdGFja292ZXJmbG93LmNvbS9hLzI2NjAzODc1XG5cdFx0Y3NzICs9IFwiXFxuLyojIHNvdXJjZU1hcHBpbmdVUkw9ZGF0YTphcHBsaWNhdGlvbi9qc29uO2Jhc2U2NCxcIiArIGJ0b2EodW5lc2NhcGUoZW5jb2RlVVJJQ29tcG9uZW50KEpTT04uc3RyaW5naWZ5KHNvdXJjZU1hcCkpKSkgKyBcIiAqL1wiO1xuXHR9XG5cblx0dmFyIGJsb2IgPSBuZXcgQmxvYihbY3NzXSwgeyB0eXBlOiBcInRleHQvY3NzXCIgfSk7XG5cblx0dmFyIG9sZFNyYyA9IGxpbmtFbGVtZW50LmhyZWY7XG5cblx0bGlua0VsZW1lbnQuaHJlZiA9IFVSTC5jcmVhdGVPYmplY3RVUkwoYmxvYik7XG5cblx0aWYob2xkU3JjKVxuXHRcdFVSTC5yZXZva2VPYmplY3RVUkwob2xkU3JjKTtcbn1cbiIsIi8qXG5TaW1wbGUgSmF2YXNjcmlwdCB1bmRvIGFuZCByZWRvLlxuaHR0cHM6Ly9naXRodWIuY29tL0FydGh1ckNsZW1lbnMvSmF2YXNjcmlwdC1VbmRvLU1hbmFnZXJcbiovXG5cbjsoZnVuY3Rpb24oKSB7XG5cblx0J3VzZSBzdHJpY3QnO1xuXG4gICAgZnVuY3Rpb24gcmVtb3ZlRnJvbVRvKGFycmF5LCBmcm9tLCB0bykge1xuICAgICAgICBhcnJheS5zcGxpY2UoZnJvbSxcbiAgICAgICAgICAgICF0byB8fFxuICAgICAgICAgICAgMSArIHRvIC0gZnJvbSArICghKHRvIDwgMCBeIGZyb20gPj0gMCkgJiYgKHRvIDwgMCB8fCAtMSkgKiBhcnJheS5sZW5ndGgpKTtcbiAgICAgICAgcmV0dXJuIGFycmF5Lmxlbmd0aDtcbiAgICB9XG5cbiAgICB2YXIgVW5kb01hbmFnZXIgPSBmdW5jdGlvbigpIHtcblxuICAgICAgICB2YXIgY29tbWFuZHMgPSBbXSxcbiAgICAgICAgICAgIGluZGV4ID0gLTEsXG4gICAgICAgICAgICBsaW1pdCA9IDAsXG4gICAgICAgICAgICBpc0V4ZWN1dGluZyA9IGZhbHNlLFxuICAgICAgICAgICAgY2FsbGJhY2ssXG4gICAgICAgICAgICBcbiAgICAgICAgICAgIC8vIGZ1bmN0aW9uc1xuICAgICAgICAgICAgZXhlY3V0ZTtcblxuICAgICAgICBleGVjdXRlID0gZnVuY3Rpb24oY29tbWFuZCwgYWN0aW9uKSB7XG4gICAgICAgICAgICBpZiAoIWNvbW1hbmQgfHwgdHlwZW9mIGNvbW1hbmRbYWN0aW9uXSAhPT0gXCJmdW5jdGlvblwiKSB7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHRoaXM7XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBpc0V4ZWN1dGluZyA9IHRydWU7XG5cbiAgICAgICAgICAgIGNvbW1hbmRbYWN0aW9uXSgpO1xuXG4gICAgICAgICAgICBpc0V4ZWN1dGluZyA9IGZhbHNlO1xuICAgICAgICAgICAgcmV0dXJuIHRoaXM7XG4gICAgICAgIH07XG5cbiAgICAgICAgcmV0dXJuIHtcblxuICAgICAgICAgICAgLypcbiAgICAgICAgICAgIEFkZCBhIGNvbW1hbmQgdG8gdGhlIHF1ZXVlLlxuICAgICAgICAgICAgKi9cbiAgICAgICAgICAgIGFkZDogZnVuY3Rpb24gKGNvbW1hbmQpIHtcbiAgICAgICAgICAgICAgICBpZiAoaXNFeGVjdXRpbmcpIHtcbiAgICAgICAgICAgICAgICAgICAgcmV0dXJuIHRoaXM7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIC8vIGlmIHdlIGFyZSBoZXJlIGFmdGVyIGhhdmluZyBjYWxsZWQgdW5kbyxcbiAgICAgICAgICAgICAgICAvLyBpbnZhbGlkYXRlIGl0ZW1zIGhpZ2hlciBvbiB0aGUgc3RhY2tcbiAgICAgICAgICAgICAgICBjb21tYW5kcy5zcGxpY2UoaW5kZXggKyAxLCBjb21tYW5kcy5sZW5ndGggLSBpbmRleCk7XG5cbiAgICAgICAgICAgICAgICBjb21tYW5kcy5wdXNoKGNvbW1hbmQpO1xuICAgICAgICAgICAgICAgIFxuICAgICAgICAgICAgICAgIC8vIGlmIGxpbWl0IGlzIHNldCwgcmVtb3ZlIGl0ZW1zIGZyb20gdGhlIHN0YXJ0XG4gICAgICAgICAgICAgICAgaWYgKGxpbWl0ICYmIGNvbW1hbmRzLmxlbmd0aCA+IGxpbWl0KSB7XG4gICAgICAgICAgICAgICAgICAgIHJlbW92ZUZyb21Ubyhjb21tYW5kcywgMCwgLShsaW1pdCsxKSk7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIFxuICAgICAgICAgICAgICAgIC8vIHNldCB0aGUgY3VycmVudCBpbmRleCB0byB0aGUgZW5kXG4gICAgICAgICAgICAgICAgaW5kZXggPSBjb21tYW5kcy5sZW5ndGggLSAxO1xuICAgICAgICAgICAgICAgIGlmIChjYWxsYmFjaykge1xuICAgICAgICAgICAgICAgICAgICBjYWxsYmFjaygpO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICByZXR1cm4gdGhpcztcbiAgICAgICAgICAgIH0sXG5cbiAgICAgICAgICAgIC8qXG4gICAgICAgICAgICBQYXNzIGEgZnVuY3Rpb24gdG8gYmUgY2FsbGVkIG9uIHVuZG8gYW5kIHJlZG8gYWN0aW9ucy5cbiAgICAgICAgICAgICovXG4gICAgICAgICAgICBzZXRDYWxsYmFjazogZnVuY3Rpb24gKGNhbGxiYWNrRnVuYykge1xuICAgICAgICAgICAgICAgIGNhbGxiYWNrID0gY2FsbGJhY2tGdW5jO1xuICAgICAgICAgICAgfSxcblxuICAgICAgICAgICAgLypcbiAgICAgICAgICAgIFBlcmZvcm0gdW5kbzogY2FsbCB0aGUgdW5kbyBmdW5jdGlvbiBhdCB0aGUgY3VycmVudCBpbmRleCBhbmQgZGVjcmVhc2UgdGhlIGluZGV4IGJ5IDEuXG4gICAgICAgICAgICAqL1xuICAgICAgICAgICAgdW5kbzogZnVuY3Rpb24gKCkge1xuICAgICAgICAgICAgICAgIHZhciBjb21tYW5kID0gY29tbWFuZHNbaW5kZXhdO1xuICAgICAgICAgICAgICAgIGlmICghY29tbWFuZCkge1xuICAgICAgICAgICAgICAgICAgICByZXR1cm4gdGhpcztcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgZXhlY3V0ZShjb21tYW5kLCBcInVuZG9cIik7XG4gICAgICAgICAgICAgICAgaW5kZXggLT0gMTtcbiAgICAgICAgICAgICAgICBpZiAoY2FsbGJhY2spIHtcbiAgICAgICAgICAgICAgICAgICAgY2FsbGJhY2soKTtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgcmV0dXJuIHRoaXM7XG4gICAgICAgICAgICB9LFxuXG4gICAgICAgICAgICAvKlxuICAgICAgICAgICAgUGVyZm9ybSByZWRvOiBjYWxsIHRoZSByZWRvIGZ1bmN0aW9uIGF0IHRoZSBuZXh0IGluZGV4IGFuZCBpbmNyZWFzZSB0aGUgaW5kZXggYnkgMS5cbiAgICAgICAgICAgICovXG4gICAgICAgICAgICByZWRvOiBmdW5jdGlvbiAoKSB7XG4gICAgICAgICAgICAgICAgdmFyIGNvbW1hbmQgPSBjb21tYW5kc1tpbmRleCArIDFdO1xuICAgICAgICAgICAgICAgIGlmICghY29tbWFuZCkge1xuICAgICAgICAgICAgICAgICAgICByZXR1cm4gdGhpcztcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgZXhlY3V0ZShjb21tYW5kLCBcInJlZG9cIik7XG4gICAgICAgICAgICAgICAgaW5kZXggKz0gMTtcbiAgICAgICAgICAgICAgICBpZiAoY2FsbGJhY2spIHtcbiAgICAgICAgICAgICAgICAgICAgY2FsbGJhY2soKTtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgcmV0dXJuIHRoaXM7XG4gICAgICAgICAgICB9LFxuXG4gICAgICAgICAgICAvKlxuICAgICAgICAgICAgQ2xlYXJzIHRoZSBtZW1vcnksIGxvc2luZyBhbGwgc3RvcmVkIHN0YXRlcy4gUmVzZXQgdGhlIGluZGV4LlxuICAgICAgICAgICAgKi9cbiAgICAgICAgICAgIGNsZWFyOiBmdW5jdGlvbiAoKSB7XG4gICAgICAgICAgICAgICAgdmFyIHByZXZfc2l6ZSA9IGNvbW1hbmRzLmxlbmd0aDtcblxuICAgICAgICAgICAgICAgIGNvbW1hbmRzID0gW107XG4gICAgICAgICAgICAgICAgaW5kZXggPSAtMTtcblxuICAgICAgICAgICAgICAgIGlmIChjYWxsYmFjayAmJiAocHJldl9zaXplID4gMCkpIHtcbiAgICAgICAgICAgICAgICAgICAgY2FsbGJhY2soKTtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICB9LFxuXG4gICAgICAgICAgICBoYXNVbmRvOiBmdW5jdGlvbiAoKSB7XG4gICAgICAgICAgICAgICAgcmV0dXJuIGluZGV4ICE9PSAtMTtcbiAgICAgICAgICAgIH0sXG5cbiAgICAgICAgICAgIGhhc1JlZG86IGZ1bmN0aW9uICgpIHtcbiAgICAgICAgICAgICAgICByZXR1cm4gaW5kZXggPCAoY29tbWFuZHMubGVuZ3RoIC0gMSk7XG4gICAgICAgICAgICB9LFxuXG4gICAgICAgICAgICBnZXRDb21tYW5kczogZnVuY3Rpb24gKCkge1xuICAgICAgICAgICAgICAgIHJldHVybiBjb21tYW5kcztcbiAgICAgICAgICAgIH0sXG5cbiAgICAgICAgICAgIGdldEluZGV4OiBmdW5jdGlvbigpIHtcbiAgICAgICAgICAgICAgICByZXR1cm4gaW5kZXg7XG4gICAgICAgICAgICB9LFxuICAgICAgICAgICAgXG4gICAgICAgICAgICBzZXRMaW1pdDogZnVuY3Rpb24gKGwpIHtcbiAgICAgICAgICAgICAgICBsaW1pdCA9IGw7XG4gICAgICAgICAgICB9XG4gICAgICAgIH07XG4gICAgfTtcblxuXHRpZiAodHlwZW9mIGRlZmluZSA9PT0gJ2Z1bmN0aW9uJyAmJiB0eXBlb2YgZGVmaW5lLmFtZCA9PT0gJ29iamVjdCcgJiYgZGVmaW5lLmFtZCkge1xuXHRcdC8vIEFNRC4gUmVnaXN0ZXIgYXMgYW4gYW5vbnltb3VzIG1vZHVsZS5cblx0XHRkZWZpbmUoZnVuY3Rpb24oKSB7XG5cdFx0XHRyZXR1cm4gVW5kb01hbmFnZXI7XG5cdFx0fSk7XG5cdH0gZWxzZSBpZiAodHlwZW9mIG1vZHVsZSAhPT0gJ3VuZGVmaW5lZCcgJiYgbW9kdWxlLmV4cG9ydHMpIHtcblx0XHRtb2R1bGUuZXhwb3J0cyA9IFVuZG9NYW5hZ2VyO1xuXHR9IGVsc2Uge1xuXHRcdHdpbmRvdy5VbmRvTWFuYWdlciA9IFVuZG9NYW5hZ2VyO1xuXHR9XG5cbn0oKSk7XG4iLCIvKipcbiAqIENyZWF0ZWQgYnkgamFja3kgb24gMjAxNi83LzkuXG4gKi9cbmV4cG9ydCBmdW5jdGlvbiBhbGVydChtc2cpIHtcbiAgICB0b2FzdHIub3B0aW9ucyA9IHtcbiAgICAgICAgY2xvc2VCdXR0b246IGZhbHNlLFxuICAgICAgICBkZWJ1ZzogZmFsc2UsXG4gICAgICAgIHByb2dyZXNzQmFyOiBmYWxzZSxcbiAgICAgICAgcG9zaXRpb25DbGFzczogXCJ0b2FzdC10b3AtY2VudGVyXCIsXG4gICAgICAgIG9uY2xpY2s6IG51bGwsXG4gICAgICAgIHNob3dEdXJhdGlvbjogXCIzMDBcIixcbiAgICAgICAgaGlkZUR1cmF0aW9uOiBcIjEwMDBcIixcbiAgICAgICAgdGltZU91dDogXCI1MDAwXCIsXG4gICAgICAgIGV4dGVuZGVkVGltZU91dDogXCIxMDAwXCIsXG4gICAgICAgIHNob3dFYXNpbmc6IFwic3dpbmdcIixcbiAgICAgICAgaGlkZUVhc2luZzogXCJsaW5lYXJcIixcbiAgICAgICAgc2hvd01ldGhvZDogXCJmYWRlSW5cIixcbiAgICAgICAgaGlkZU1ldGhvZDogXCJmYWRlT3V0XCJcbiAgICB9O1xuICAgIHRvYXN0ci5lcnJvcihtc2cpO1xuICAgIC8vIGNvbnN0IGRpYWxvZyA9IGJ1aWxkRGlhbG9nKCfmtojmga/mj5DnpLonLG1zZyk7XG4gICAgLy8gZGlhbG9nLm1vZGFsKCdzaG93Jyk7XG59O1xuXG5leHBvcnQgZnVuY3Rpb24gc3VjY2Vzc0FsZXJ0KG1zZykge1xuICAgIHRvYXN0ci5vcHRpb25zID0ge1xuICAgICAgICBjbG9zZUJ1dHRvbjogZmFsc2UsXG4gICAgICAgIGRlYnVnOiBmYWxzZSxcbiAgICAgICAgcHJvZ3Jlc3NCYXI6IGZhbHNlLFxuICAgICAgICBwb3NpdGlvbkNsYXNzOiBcInRvYXN0LXRvcC1jZW50ZXJcIixcbiAgICAgICAgb25jbGljazogbnVsbCxcbiAgICAgICAgc2hvd0R1cmF0aW9uOiBcIjMwMFwiLFxuICAgICAgICBoaWRlRHVyYXRpb246IFwiMTAwMFwiLFxuICAgICAgICB0aW1lT3V0OiBcIjUwMDBcIixcbiAgICAgICAgZXh0ZW5kZWRUaW1lT3V0OiBcIjEwMDBcIixcbiAgICAgICAgc2hvd0Vhc2luZzogXCJzd2luZ1wiLFxuICAgICAgICBoaWRlRWFzaW5nOiBcImxpbmVhclwiLFxuICAgICAgICBzaG93TWV0aG9kOiBcImZhZGVJblwiLFxuICAgICAgICBoaWRlTWV0aG9kOiBcImZhZGVPdXRcIlxuICAgIH07XG4gICAgdG9hc3RyLnN1Y2Nlc3MobXNnKTtcbn07XG5cbmV4cG9ydCBmdW5jdGlvbiBjb25maXJtKG1zZywgY2FsbGJhY2spIHtcbiAgICBjb25zdCBkaWFsb2cgPSBidWlsZERpYWxvZygn56Gu6K6k5o+Q56S6JywgbXNnLCBbe1xuICAgICAgICBuYW1lOiAn56Gu6K6kJyxcbiAgICAgICAgY2xpY2s6IGZ1bmN0aW9uICgpIHtcbiAgICAgICAgICAgIGNhbGxiYWNrLmNhbGwodGhpcyk7XG4gICAgICAgIH1cbiAgICB9XSk7XG4gICAgZGlhbG9nLm1vZGFsKCdzaG93Jyk7XG59O1xuXG5leHBvcnQgZnVuY3Rpb24gZGlhbG9nKHRpdGxlLCBjb250ZW50LCBjYWxsYmFjaykge1xuICAgIGNvbnN0IGRpYWxvZyA9IGJ1aWxkRGlhbG9nKHRpdGxlLCBjb250ZW50LCBbe1xuICAgICAgICBuYW1lOiAn56Gu6K6kJyxcbiAgICAgICAgY2xpY2s6IGZ1bmN0aW9uICgpIHtcbiAgICAgICAgICAgIGNhbGxiYWNrLmNhbGwodGhpcyk7XG4gICAgICAgIH1cbiAgICB9XSk7XG4gICAgZGlhbG9nLm1vZGFsKCdzaG93Jyk7XG59O1xuXG5leHBvcnQgZnVuY3Rpb24gc2hvd0RpYWxvZyh0aXRsZSwgZGlhbG9nQ29udGVudCwgYnV0dG9ucywgZXZlbnRzLCBsYXJnZSkge1xuICAgIGNvbnN0IGRpYWxvZyA9IGJ1aWxkRGlhbG9nKHRpdGxlLCBkaWFsb2dDb250ZW50LCBidXR0b25zLCBsYXJnZSk7XG4gICAgZGlhbG9nLm1vZGFsKCdzaG93Jyk7XG4gICAgaWYgKGV2ZW50cykge1xuICAgICAgICBmb3IgKGxldCBldmVudCBvZiBldmVudHMpIHtcbiAgICAgICAgICAgIGRpYWxvZy5vbihldmVudC5uYW1lLCBldmVudC5jYWxsYmFjayk7XG4gICAgICAgIH1cbiAgICB9XG59O1xuXG5mdW5jdGlvbiBidWlsZERpYWxvZyh0aXRsZSwgZGlhbG9nQ29udGVudCwgYnV0dG9ucywgbGFyZ2UpIHtcbiAgICBjb25zdCBjbGFzc05hbWUgPSAnbW9kYWwtZGlhbG9nJyArIChsYXJnZSA/ICcgbW9kYWwtbGcnIDogJycpO1xuICAgIGxldCBtb2RhbCA9ICQoYDxkaXYgY2xhc3M9XCJtb2RhbCBmYWRlIGRhdGEtcmVwb3J0LW1vZGFsXCIgdGFiaW5kZXg9XCItMVwiIHJvbGU9XCJkaWFsb2dcIiBhcmlhLWhpZGRlbj1cInRydWVcIj48L2Rpdj5gKTtcbiAgICBsZXQgZGlhbG9nID0gJChgPGRpdiBjbGFzcz1cIiR7Y2xhc3NOYW1lfVwiIHN0eWxlPVwid2lkdGg6IDM5MHB4O1wiPjwvZGl2PmApO1xuICAgIG1vZGFsLmFwcGVuZChkaWFsb2cpO1xuICAgIGxldCBjb250ZW50ID0gJChgPGRpdiBjbGFzcz1cIm1vZGFsLWNvbnRlbnRcIj5cbiAgICAgICAgIDxkaXYgY2xhc3M9XCJtb2RhbC1oZWFkZXJcIj5cbiAgICAgICAgICAgIDxidXR0b24gdHlwZT1cImJ1dHRvblwiIGNsYXNzPVwiY2xvc2VcIiBkYXRhLWRpc21pc3M9XCJtb2RhbFwiIGFyaWEtaGlkZGVuPVwidHJ1ZVwiPlxuICAgICAgICAgICAgICAgIDxpIGNsYXNzPVwicmVwb3J0LWljb24gcmVwb3J0LWljb24taHVhYmFuMTZmdWJlbjRcIj48L2k+XG4gICAgICAgICAgICA8L2J1dHRvbj5cbiAgICAgICAgICAgIDxoNCBjbGFzcz1cIm1vZGFsLXRpdGxlXCI+XG4gICAgICAgICAgICAgICAke3RpdGxlfVxuICAgICAgICAgICAgPC9oND5cbiAgICAgICAgIDwvZGl2PlxuICAgICAgICAgPGRpdiBjbGFzcz1cIm1vZGFsLWJvZHlcIj5cbiAgICAgICAgICAgICR7dHlwZW9mKGRpYWxvZ0NvbnRlbnQpPT09J3N0cmluZycgPyBkaWFsb2dDb250ZW50IDogJyd9XG4gICAgICAgICA8L2Rpdj5gKTtcbiAgICBpZiAodHlwZW9mIChkaWFsb2dDb250ZW50KSA9PT0gJ29iamVjdCcpIHtcbiAgICAgICAgY29udGVudC5maW5kKCcubW9kYWwtYm9keScpLmFwcGVuZChkaWFsb2dDb250ZW50KTtcbiAgICB9XG4gICAgZGlhbG9nLmFwcGVuZChjb250ZW50KTtcbiAgICBsZXQgZm9vdGVyID0gJChgPGRpdiBjbGFzcz1cIm1vZGFsLWZvb3RlclwiPjwvZGl2PmApO1xuICAgIGNvbnRlbnQuYXBwZW5kKGZvb3Rlcik7XG4gICAgaWYgKGJ1dHRvbnMpIHtcbiAgICAgICAgYnV0dG9ucy5mb3JFYWNoKChidG4sIGluZGV4KSA9PiB7XG4gICAgICAgICAgICBsZXQgYnV0dG9uID0gJChgPGJ1dHRvbiB0eXBlPVwiYnV0dG9uXCIgY2xhc3M9XCJlbC1idXR0b24gZWwtYnV0dG9uLS1wcmltYXJ5IGVsLWJ1dHRvbi0tc21hbGxcIj4ke2J0bi5uYW1lfTwvYnV0dG9uPmApO1xuICAgICAgICAgICAgYnV0dG9uLmNsaWNrKGZ1bmN0aW9uIChlKSB7XG4gICAgICAgICAgICAgICAgYnRuLmNsaWNrLmNhbGwodGhpcyk7XG4gICAgICAgICAgICAgICAgaWYgKCFidG4uaG9sZERpYWxvZykge1xuICAgICAgICAgICAgICAgICAgICBtb2RhbC5tb2RhbCgnaGlkZScpO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgIH0uYmluZCh0aGlzKSk7XG4gICAgICAgICAgICBmb290ZXIuYXBwZW5kKGJ1dHRvbik7XG4gICAgICAgIH0pO1xuICAgIH0gZWxzZSB7XG4gICAgICAgIGxldCBva0J0biA9ICQoYDxidXR0b24gdHlwZT1cImJ1dHRvblwiIGNsYXNzPVwiZWwtYnV0dG9uIGVsLWJ1dHRvbi0tcHJpbWFyeSBlbC1idXR0b24tLXNtYWxsXCIgZGF0YS1kaXNtaXNzPVwibW9kYWxcIj7noa7lrpo8L2J1dHRvbj5gKTtcbiAgICAgICAgZm9vdGVyLmFwcGVuZChva0J0bik7XG4gICAgfVxuXG4gICAgbW9kYWwub24oXCJzaG93LmJzLm1vZGFsXCIsIGZ1bmN0aW9uICgpIHtcbiAgICAgICAgdmFyIGluZGV4ID0gMTA1MDtcbiAgICAgICAgJChkb2N1bWVudCkuZmluZCgnLm1vZGFsJykuZWFjaChmdW5jdGlvbiAoaSwgZCkge1xuICAgICAgICAgICAgdmFyIHpJbmRleCA9ICQoZCkuY3NzKCd6LWluZGV4Jyk7XG4gICAgICAgICAgICBpZiAoekluZGV4ICYmIHpJbmRleCAhPT0gJycgJiYgIWlzTmFOKHpJbmRleCkpIHtcbiAgICAgICAgICAgICAgICB6SW5kZXggPSBwYXJzZUludCh6SW5kZXgpO1xuICAgICAgICAgICAgICAgIGlmICh6SW5kZXggPiBpbmRleCkge1xuICAgICAgICAgICAgICAgICAgICBpbmRleCA9IHpJbmRleDtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuICAgICAgICBpbmRleCsrO1xuICAgICAgICBtb2RhbC5jc3Moe1xuICAgICAgICAgICAgJ3otaW5kZXgnOiBpbmRleFxuICAgICAgICB9KTtcbiAgICB9KTtcbiAgICByZXR1cm4gbW9kYWw7XG59OyIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5nYW8gb24gMjAxNi83LzI3LlxuICovXG5pbXBvcnQgVW5kb01hbmFnZXIgZnJvbSAndW5kby1tYW5hZ2VyJztcbmltcG9ydCB7XG4gICAgYWxlcnRcbn0gZnJvbSAnLi9Nc2dCb3guanMnO1xuXG5leHBvcnQgY29uc3Qgc2hvd0xvYWRpbmcgPSAoKSA9PiB7XG4gICAgY29uc3QgdXJsID0gJy4vaWNvbnMvbG9hZGluZy5naWYnO1xuICAgIGNvbnN0IGggPSAkKHdpbmRvdykuaGVpZ2h0KCkgLyAyLFxuICAgICAgICB3ID0gJCh3aW5kb3cpLndpZHRoKCkgLyAyO1xuICAgIGNvbnN0IGNvdmVyID0gJChgPGRpdiBjbGFzcz1cInVyZXBvcnQtbG9hZGluZy1jb3ZlclwiIHN0eWxlPVwicG9zaXRpb246IGFic29sdXRlO2xlZnQ6IDBweDt0b3A6IDBweDt3aWR0aDoke3cqMn1weDtoZWlnaHQ6JHtoKjJ9cHg7ei1pbmRleDogMTE5OTtiYWNrZ3JvdW5kOnJnYmEoMjIyLDIyMiwyMjIsLjUpXCI+PC9kaXY+YCk7XG4gICAgJChkb2N1bWVudC5ib2R5KS5hcHBlbmQoY292ZXIpO1xuICAgIGNvbnN0IGxvYWRpbmcgPSAkKGA8ZGl2IGNsYXNzPVwidXJlcG9ydC1sb2FkaW5nXCIgc3R5bGU9XCJ0ZXh0LWFsaWduOiBjZW50ZXI7cG9zaXRpb246IGFic29sdXRlO3otaW5kZXg6IDExMjA7bGVmdDogJHt3LTM1fXB4O3RvcDogJHtoLTM1fXB4O1wiPjxpbWcgc3JjPVwiJHt1cmx9XCI+XG4gICAgPGRpdiBzdHlsZT1cIm1hcmdpbi10b3A6IDVweFwiPuaVsOaNruWKoOi9veS4rS4uLjwvZGl2PjwvZGl2PmApO1xuICAgICQoZG9jdW1lbnQuYm9keSkuYXBwZW5kKGxvYWRpbmcpO1xufTtcblxuZXhwb3J0IGZ1bmN0aW9uIGhpZGVMb2FkaW5nKCkge1xuICAgICQoJy51cmVwb3J0LWxvYWRpbmctY292ZXInKS5yZW1vdmUoKTtcbiAgICAkKCcudXJlcG9ydC1sb2FkaW5nJykucmVtb3ZlKCk7XG59O1xuXG5leHBvcnQgZnVuY3Rpb24gcmVzZXRUYWJsZURhdGEoaG90KSB7XG4gICAgY29uc3QgY291bnRDb2xzID0gaG90LmNvdW50Q29scygpLFxuICAgICAgICBjb3VudFJvd3MgPSBob3QuY291bnRSb3dzKCksXG4gICAgICAgIGNvbnRleHQgPSBob3QuY29udGV4dCxcbiAgICAgICAgZGF0YSA9IFtdO1xuICAgIGZvciAobGV0IGkgPSAwOyBpIDwgY291bnRSb3dzOyBpKyspIHtcbiAgICAgICAgbGV0IHJvd0RhdGEgPSBbXTtcbiAgICAgICAgZm9yIChsZXQgaiA9IDA7IGogPCBjb3VudENvbHM7IGorKykge1xuICAgICAgICAgICAgbGV0IHRkID0gaG90LmdldENlbGwoaSwgaik7XG4gICAgICAgICAgICBpZiAoIXRkKSB7XG4gICAgICAgICAgICAgICAgcm93RGF0YS5wdXNoKFwiXCIpO1xuICAgICAgICAgICAgICAgIGNvbnRpbnVlO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgbGV0IGNlbGxEZWYgPSBjb250ZXh0LmdldENlbGwoaSwgaik7XG4gICAgICAgICAgICBpZiAoY2VsbERlZikge1xuICAgICAgICAgICAgICAgIGxldCB2YWx1ZVR5cGUgPSBjZWxsRGVmLnZhbHVlLnR5cGUsXG4gICAgICAgICAgICAgICAgICAgIHZhbHVlID0gY2VsbERlZi52YWx1ZTtcbiAgICAgICAgICAgICAgICBpZiAodmFsdWVUeXBlID09PSAnZGF0YXNldCcpIHtcbiAgICAgICAgICAgICAgICAgICAgbGV0IHRleHQgPSB2YWx1ZS5kYXRhc2V0TmFtZSArIFwiLlwiICsgdmFsdWUuYWdncmVnYXRlICsgXCIoXCI7XG4gICAgICAgICAgICAgICAgICAgIGxldCBwcm9wID0gdmFsdWUucHJvcGVydHk7XG4gICAgICAgICAgICAgICAgICAgIGlmIChwcm9wLmxlbmd0aCA+IDEzKSB7XG4gICAgICAgICAgICAgICAgICAgICAgICB0ZXh0ICs9IHByb3Auc3Vic3RyaW5nKDAsIDEwKSArICcuLiknO1xuICAgICAgICAgICAgICAgICAgICB9IGVsc2Uge1xuICAgICAgICAgICAgICAgICAgICAgICAgdGV4dCArPSBwcm9wICsgXCIpXCI7XG4gICAgICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICAgICAgcm93RGF0YS5wdXNoKHRleHQpO1xuICAgICAgICAgICAgICAgIH0gZWxzZSBpZiAodmFsdWVUeXBlID09PSAnZXhwcmVzc2lvbicpIHtcbiAgICAgICAgICAgICAgICAgICAgbGV0IHYgPSB2YWx1ZS52YWx1ZSB8fCAnJztcbiAgICAgICAgICAgICAgICAgICAgaWYgKHYubGVuZ3RoID4gMTYpIHtcbiAgICAgICAgICAgICAgICAgICAgICAgIHYgPSB2LnN1YnN0cmluZygwLCAxMykgKyAnLi4uJztcbiAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgICAgICByb3dEYXRhLnB1c2godik7XG4gICAgICAgICAgICAgICAgfSBlbHNlIHtcbiAgICAgICAgICAgICAgICAgICAgcm93RGF0YS5wdXNoKHZhbHVlLnZhbHVlIHx8IFwiXCIpO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgIH0gZWxzZSB7XG4gICAgICAgICAgICAgICAgcm93RGF0YS5wdXNoKFwiXCIpO1xuICAgICAgICAgICAgfVxuICAgICAgICB9XG4gICAgICAgIGRhdGEucHVzaChyb3dEYXRhKTtcbiAgICB9XG4gICAgaG90LmxvYWREYXRhKGRhdGEpO1xufTtcblxuZXhwb3J0IGZ1bmN0aW9uIGJ1aWxkTmV3Q2VsbERlZihyb3dOdW1iZXIsIGNvbHVtbk51bWJlcikge1xuICAgIGxldCBjZWxsRGVmID0ge1xuICAgICAgICByb3dOdW1iZXIsXG4gICAgICAgIGNvbHVtbk51bWJlcixcbiAgICAgICAgZXhwYW5kOiAnTm9uZScsXG4gICAgICAgIGNlbGxTdHlsZToge1xuICAgICAgICAgICAgZm9udFNpemU6IDksXG4gICAgICAgICAgICBmb3JlY29sb3I6ICcwLDAsMCcsXG4gICAgICAgICAgICBmb250RmFtaWx5OiAn5a6L5L2TJyxcbiAgICAgICAgICAgIGFsaWduOiAnY2VudGVyJyxcbiAgICAgICAgICAgIHZhbGlnbjogJ21pZGRsZSdcbiAgICAgICAgfSxcbiAgICAgICAgdmFsdWU6IHtcbiAgICAgICAgICAgIHR5cGU6ICdzaW1wbGUnLFxuICAgICAgICAgICAgdmFsdWU6ICcnXG4gICAgICAgIH1cbiAgICB9O1xuICAgIHJldHVybiBjZWxsRGVmO1xufTtcblxuZXhwb3J0IGZ1bmN0aW9uIHRhYmxlVG9YbWwoY29udGV4dCkge1xuICAgIGNvbnN0IGhvdCA9IGNvbnRleHQuaG90O1xuICAgIGNvbnN0IGNvdW50Um93cyA9IGhvdC5jb3VudFJvd3MoKSxcbiAgICAgICAgY291bnRDb2xzID0gaG90LmNvdW50Q29scygpO1xuICAgIGxldCB4bWwgPSBgPD94bWwgdmVyc2lvbj1cIjEuMFwiIGVuY29kaW5nPVwiVVRGLThcIj8+PHVyZXBvcnQ+YDtcbiAgICBsZXQgcm93c1htbCA9ICcnLFxuICAgICAgICBjb2x1bW5YbWwgPSAnJztcbiAgICBjb25zdCByb3dIZWFkZXJzID0gY29udGV4dC5yb3dIZWFkZXJzO1xuICAgIGZvciAobGV0IGkgPSAwOyBpIDwgY291bnRSb3dzOyBpKyspIHtcbiAgICAgICAgbGV0IGhlaWdodCA9IGhvdC5nZXRSb3dIZWlnaHQoaSkgfHwgMTY7XG4gICAgICAgIGhlaWdodCA9IHBpeGVsVG9Qb2ludChoZWlnaHQpO1xuICAgICAgICBsZXQgYmFuZCA9IG51bGw7XG4gICAgICAgIGZvciAobGV0IGhlYWRlciBvZiByb3dIZWFkZXJzKSB7XG4gICAgICAgICAgICBpZiAoaGVhZGVyLnJvd051bWJlciA9PT0gaSkge1xuICAgICAgICAgICAgICAgIGJhbmQgPSBoZWFkZXIuYmFuZDtcbiAgICAgICAgICAgICAgICBicmVhaztcbiAgICAgICAgICAgIH1cbiAgICAgICAgfVxuICAgICAgICBpZiAoYmFuZCkge1xuICAgICAgICAgICAgcm93c1htbCArPSBgPHJvdyByb3ctbnVtYmVyPVwiJHtpKzF9XCIgaGVpZ2h0PVwiJHtoZWlnaHR9XCIgYmFuZD1cIiR7YmFuZH1cIi8+YDtcbiAgICAgICAgfSBlbHNlIHtcbiAgICAgICAgICAgIHJvd3NYbWwgKz0gYDxyb3cgcm93LW51bWJlcj1cIiR7aSsxfVwiIGhlaWdodD1cIiR7aGVpZ2h0fVwiLz5gO1xuICAgICAgICB9XG4gICAgfVxuICAgIGZvciAobGV0IGkgPSAwOyBpIDwgY291bnRDb2xzOyBpKyspIHtcbiAgICAgICAgbGV0IHdpZHRoID0gaG90LmdldENvbFdpZHRoKGkpIHx8IDMwO1xuICAgICAgICB3aWR0aCA9IHBpeGVsVG9Qb2ludCh3aWR0aCk7XG4gICAgICAgIGNvbHVtblhtbCArPSBgPGNvbHVtbiBjb2wtbnVtYmVyPVwiJHtpKzF9XCIgd2lkdGg9XCIke3dpZHRofVwiLz5gO1xuICAgIH1cbiAgICBsZXQgY2VsbFhtbCA9ICcnLFxuICAgICAgICBzcGFuRGF0YSA9IFtdO1xuICAgIGZvciAobGV0IGkgPSAwOyBpIDwgY291bnRSb3dzOyBpKyspIHtcbiAgICAgICAgZm9yIChsZXQgaiA9IDA7IGogPCBjb3VudENvbHM7IGorKykge1xuICAgICAgICAgICAgaWYgKHNwYW5EYXRhLmluZGV4T2YoaSArIFwiLFwiICsgaikgPiAtMSkge1xuICAgICAgICAgICAgICAgIGNvbnRpbnVlO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgbGV0IGNlbGxEZWYgPSBjb250ZXh0LmdldENlbGwoaSwgaik7XG4gICAgICAgICAgICBpZiAoIWNlbGxEZWYpIHtcbiAgICAgICAgICAgICAgICBjb250aW51ZTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIGxldCBjZWxsTmFtZSA9IGNvbnRleHQuZ2V0Q2VsbE5hbWUoaSwgaik7XG4gICAgICAgICAgICBjZWxsWG1sICs9IGA8Y2VsbCBleHBhbmQ9XCIke2NlbGxEZWYuZXhwYW5kfVwiIG5hbWU9XCIke2NlbGxOYW1lfVwiIHJvdz1cIiR7KGkrMSl9XCIgY29sPVwiJHsoaisxKX1cImA7XG4gICAgICAgICAgICBpZiAoY2VsbERlZi5sZWZ0UGFyZW50Q2VsbE5hbWUgJiYgY2VsbERlZi5sZWZ0UGFyZW50Q2VsbE5hbWUgIT09ICcnKSB7XG4gICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgIGxlZnQtY2VsbD1cIiR7Y2VsbERlZi5sZWZ0UGFyZW50Q2VsbE5hbWV9XCJgO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgaWYgKGNlbGxEZWYudG9wUGFyZW50Q2VsbE5hbWUgJiYgY2VsbERlZi50b3BQYXJlbnRDZWxsTmFtZSAhPT0gJycpIHtcbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgdG9wLWNlbGw9XCIke2NlbGxEZWYudG9wUGFyZW50Q2VsbE5hbWV9XCJgO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgaWYgKGNlbGxEZWYuZmlsbEJsYW5rUm93cykge1xuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYCBmaWxsLWJsYW5rLXJvd3M9XCIke2NlbGxEZWYuZmlsbEJsYW5rUm93c31cImA7XG4gICAgICAgICAgICAgICAgaWYgKGNlbGxEZWYubXVsdGlwbGUpIHtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgIG11bHRpcGxlPVwiJHtjZWxsRGVmLm11bHRpcGxlfVwiYDtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICB9XG5cbiAgICAgICAgICAgIGNvbnN0IHNwYW4gPSBnZXRTcGFuKGhvdCwgaSwgaik7XG4gICAgICAgICAgICBsZXQgcm93U3BhbiA9IHNwYW4ucm93c3BhbixcbiAgICAgICAgICAgICAgICBjb2xTcGFuID0gc3Bhbi5jb2xzcGFuO1xuICAgICAgICAgICAgbGV0IHN0YXJ0Um93ID0gaSxcbiAgICAgICAgICAgICAgICBlbmRSb3cgPSBpICsgcm93U3BhbiAtIDEsXG4gICAgICAgICAgICAgICAgc3RhcnRDb2wgPSBqLFxuICAgICAgICAgICAgICAgIGVuZENvbCA9IGogKyBjb2xTcGFuIC0gMTtcbiAgICAgICAgICAgIGZvciAobGV0IHIgPSBzdGFydFJvdzsgciA8PSBlbmRSb3c7IHIrKykge1xuICAgICAgICAgICAgICAgIGZvciAobGV0IGMgPSBzdGFydENvbDsgYyA8PSBlbmRDb2w7IGMrKykge1xuICAgICAgICAgICAgICAgICAgICBzcGFuRGF0YS5wdXNoKHIgKyBcIixcIiArIGMpO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIGlmIChyb3dTcGFuID4gMSkge1xuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYCByb3ctc3Bhbj1cIiR7cm93U3Bhbn1cImA7XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBpZiAoY29sU3BhbiA+IDEpIHtcbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgY29sLXNwYW49XCIke2NvbFNwYW59XCJgO1xuICAgICAgICAgICAgfVxuXG4gICAgICAgICAgICBpZiAoY2VsbERlZi5saW5rVXJsICYmIGNlbGxEZWYubGlua1VybCAhPT0gJycpIHtcbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgbGluay11cmw9XCIke2NlbGxEZWYubGlua1VybH1cImA7XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBpZiAoY2VsbERlZi5saW5rVGFyZ2V0V2luZG93ICYmIGNlbGxEZWYubGlua1RhcmdldFdpbmRvdyAhPT0gJycpIHtcbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgbGluay10YXJnZXQtd2luZG93PVwiJHtjZWxsRGVmLmxpbmtUYXJnZXRXaW5kb3d9XCJgO1xuICAgICAgICAgICAgfVxuXG4gICAgICAgICAgICBjZWxsWG1sICs9ICc+JztcbiAgICAgICAgICAgIGxldCBjZWxsU3R5bGUgPSBjZWxsRGVmLmNlbGxTdHlsZTtcbiAgICAgICAgICAgIGNlbGxYbWwgKz0gYnVpbGRDZWxsU3R5bGUoY2VsbFN0eWxlKTtcbiAgICAgICAgICAgIGlmIChjZWxsRGVmLmxpbmtQYXJhbWV0ZXJzICYmIGNlbGxEZWYubGlua1BhcmFtZXRlcnMubGVuZ3RoID4gMCkge1xuICAgICAgICAgICAgICAgIGZvciAobGV0IHBhcmFtIG9mIGNlbGxEZWYubGlua1BhcmFtZXRlcnMpIHtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPGxpbmstcGFyYW1ldGVyIG5hbWU9XCIke3BhcmFtLm5hbWV9XCI+YDtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPHZhbHVlPjwhW0NEQVRBWyR7cGFyYW0udmFsdWV9XV0+PC92YWx1ZT5gO1xuICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8L2xpbmstcGFyYW1ldGVyPmA7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgfVxuICAgICAgICAgICAgY29uc3QgdmFsdWUgPSBjZWxsRGVmLnZhbHVlO1xuICAgICAgICAgICAgaWYgKHZhbHVlLnR5cGUgPT09ICdkYXRhc2V0Jykge1xuICAgICAgICAgICAgICAgIGxldCBtc2cgPSBudWxsO1xuICAgICAgICAgICAgICAgIGlmICghdmFsdWUuZGF0YXNldE5hbWUpIHtcbiAgICAgICAgICAgICAgICAgICAgbXNnID0gYCR7Y2VsbE5hbWV95Y2V5YWD5qC85pWw5o2u6ZuG5bGe5oCn5LiN6IO95Li656m677yBYDtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgaWYgKCFtc2cgJiYgIXZhbHVlLnByb3BlcnR5KSB7XG4gICAgICAgICAgICAgICAgICAgIG1zZyA9IGAke2NlbGxOYW1lfeWNleWFg+agvOWxnuaAp+S4jeiDveS4uuepuu+8gWA7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGlmICghbXNnICYmICF2YWx1ZS5hZ2dyZWdhdGUpIHtcbiAgICAgICAgICAgICAgICAgICAgbXNnID0gYCR7Y2VsbE5hbWV95Y2V5YWD5qC86IGa5ZCI5pa55byP5bGe5oCn5LiN6IO95Li656m677yBYDtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgaWYgKG1zZykge1xuICAgICAgICAgICAgICAgICAgICBhbGVydChtc2cpO1xuICAgICAgICAgICAgICAgICAgICB0aHJvdyBtc2c7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGNvbnN0IG1hcHBpbmdUeXBlID0gdmFsdWUubWFwcGluZ1R5cGUgfHwgJ3NpbXBsZSc7XG4gICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPGRhdGFzZXQtdmFsdWUgZGF0YXNldC1uYW1lPVwiJHtlbmNvZGUodmFsdWUuZGF0YXNldE5hbWUpfVwiIGFnZ3JlZ2F0ZT1cIiR7dmFsdWUuYWdncmVnYXRlfVwiIHByb3BlcnR5PVwiJHt2YWx1ZS5wcm9wZXJ0eX1cIiBvcmRlcj1cIiR7dmFsdWUub3JkZXJ9XCIgbWFwcGluZy10eXBlPVwiJHttYXBwaW5nVHlwZX1cImA7XG4gICAgICAgICAgICAgICAgaWYgKG1hcHBpbmdUeXBlID09PSAnZGF0YXNldCcpIHtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgIG1hcHBpbmctZGF0YXNldD1cIiR7dmFsdWUubWFwcGluZ0RhdGFzZXR9XCIgbWFwcGluZy1rZXktcHJvcGVydHk9XCIke3ZhbHVlLm1hcHBpbmdLZXlQcm9wZXJ0eX1cIiBtYXBwaW5nLXZhbHVlLXByb3BlcnR5PVwiJHt2YWx1ZS5tYXBwaW5nVmFsdWVQcm9wZXJ0eX1cImA7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gJz4nO1xuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYnVpbGRDb25kaXRpb25zKHZhbHVlLmNvbmRpdGlvbnMpO1xuICAgICAgICAgICAgICAgIGlmICh2YWx1ZS5hZ2dyZWdhdGUgPT09ICdjdXN0b21ncm91cCcpIHtcbiAgICAgICAgICAgICAgICAgICAgY29uc3QgZ3JvdXBJdGVtcyA9IHZhbHVlLmdyb3VwSXRlbXM7XG4gICAgICAgICAgICAgICAgICAgIGZvciAobGV0IGdyb3VwSXRlbSBvZiBncm91cEl0ZW1zKSB7XG4gICAgICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8Z3JvdXAtaXRlbSBuYW1lPVwiJHtncm91cEl0ZW0ubmFtZX1cIj5gO1xuICAgICAgICAgICAgICAgICAgICAgICAgZm9yIChsZXQgY29uZGl0aW9uIG9mIGdyb3VwSXRlbS5jb25kaXRpb25zKSB7XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPGNvbmRpdGlvbiBwcm9wZXJ0eT1cIiR7Y29uZGl0aW9uLmxlZnR9XCIgb3A9XCIke2VuY29kZShjb25kaXRpb24ub3BlcmF0aW9uIHx8IGNvbmRpdGlvbi5vcCl9XCIgaWQ9XCIke2NvbmRpdGlvbi5pZH1cImA7XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgaWYgKGNvbmRpdGlvbi5qb2luKSB7XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYCBqb2luPVwiJHtjb25kaXRpb24uam9pbn1cIj5gO1xuICAgICAgICAgICAgICAgICAgICAgICAgICAgIH0gZWxzZSB7XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYD5gO1xuICAgICAgICAgICAgICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8dmFsdWU+PCFbQ0RBVEFbJHtjb25kaXRpb24ucmlnaHR9XV0+PC92YWx1ZT5gO1xuICAgICAgICAgICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDwvY29uZGl0aW9uPmA7XG4gICAgICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9ICc8L2dyb3VwLWl0ZW0+JztcbiAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBpZiAobWFwcGluZ1R5cGUgPT09ICdzaW1wbGUnKSB7XG4gICAgICAgICAgICAgICAgICAgIGNvbnN0IG1hcHBpbmdJdGVtcyA9IHZhbHVlLm1hcHBpbmdJdGVtcztcbiAgICAgICAgICAgICAgICAgICAgaWYgKG1hcHBpbmdJdGVtcyAmJiBtYXBwaW5nSXRlbXMubGVuZ3RoID4gMCkge1xuICAgICAgICAgICAgICAgICAgICAgICAgZm9yIChsZXQgbWFwcGluZ0l0ZW0gb2YgbWFwcGluZ0l0ZW1zKSB7XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPG1hcHBpbmctaXRlbSB2YWx1ZT1cIiR7ZW5jb2RlKG1hcHBpbmdJdGVtLnZhbHVlKX1cIiBsYWJlbD1cIiR7ZW5jb2RlKG1hcHBpbmdJdGVtLmxhYmVsKX1cIi8+YDtcbiAgICAgICAgICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8L2RhdGFzZXQtdmFsdWU+YDtcbiAgICAgICAgICAgIH0gZWxzZSBpZiAodmFsdWUudHlwZSA9PT0gJ2V4cHJlc3Npb24nKSB7XG4gICAgICAgICAgICAgICAgaWYgKCF2YWx1ZS52YWx1ZSB8fCB2YWx1ZS52YWx1ZSA9PT0gJycpIHtcbiAgICAgICAgICAgICAgICAgICAgY29uc3QgbXNnID0gYCR7Y2VsbE5hbWV95Y2V5YWD5qC86KGo6L6+5byP5LiN6IO95Li656m6YDtcbiAgICAgICAgICAgICAgICAgICAgYWxlcnQobXNnKTtcbiAgICAgICAgICAgICAgICAgICAgdGhyb3cgbXNnO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8ZXhwcmVzc2lvbi12YWx1ZT5gO1xuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDwhW0NEQVRBWyR7dmFsdWUudmFsdWV9XV0+YDtcbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8L2V4cHJlc3Npb24tdmFsdWU+YDtcbiAgICAgICAgICAgIH0gZWxzZSBpZiAodmFsdWUudHlwZSA9PT0gJ3NpbXBsZScpIHtcbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8c2ltcGxlLXZhbHVlPmA7XG4gICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPCFbQ0RBVEFbJHt2YWx1ZS52YWx1ZSB8fCAnJ31dXT5gO1xuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDwvc2ltcGxlLXZhbHVlPmA7XG4gICAgICAgICAgICB9IGVsc2UgaWYgKHZhbHVlLnR5cGUgPT09ICdpbWFnZScpIHtcbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8aW1hZ2UtdmFsdWUgc291cmNlPVwiJHt2YWx1ZS5zb3VyY2V9XCJgO1xuICAgICAgICAgICAgICAgIGlmICh2YWx1ZS53aWR0aCkge1xuICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgd2lkdGg9XCIke3ZhbHVlLndpZHRofVwiYFxuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBpZiAodmFsdWUuaGVpZ2h0KSB7XG4gICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYCBoZWlnaHQ9XCIke3ZhbHVlLmhlaWdodH1cImA7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYD5gO1xuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDx0ZXh0PmA7XG4gICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPCFbQ0RBVEFbJHt2YWx1ZS52YWx1ZX1dXT5gO1xuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDwvdGV4dD5gO1xuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDwvaW1hZ2UtdmFsdWU+YDtcbiAgICAgICAgICAgIH0gZWxzZSBpZiAodmFsdWUudHlwZSA9PT0gJ3p4aW5nJykge1xuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDx6eGluZy12YWx1ZSBzb3VyY2U9XCIke3ZhbHVlLnNvdXJjZX1cIiBjYXRlZ29yeT1cIiR7dmFsdWUuY2F0ZWdvcnl9XCIgd2lkdGg9XCIke3ZhbHVlLndpZHRofVwiIGhlaWdodD1cIiR7dmFsdWUuaGVpZ2h0fVwiYDtcbiAgICAgICAgICAgICAgICBpZiAodmFsdWUuZm9ybWF0KSB7XG4gICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYCBmb3JtYXQ9XCIke3ZhbHVlLmZvcm1hdH1cImA7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYD5gO1xuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDx0ZXh0PmA7XG4gICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPCFbQ0RBVEFbJHt2YWx1ZS52YWx1ZX1dXT5gO1xuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDwvdGV4dD5gO1xuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDwvenhpbmctdmFsdWU+YDtcbiAgICAgICAgICAgIH0gZWxzZSBpZiAodmFsdWUudHlwZSA9PT0gJ3NsYXNoJykge1xuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDxzbGFzaC12YWx1ZT5gO1xuICAgICAgICAgICAgICAgIGNvbnN0IHNsYXNoZXMgPSB2YWx1ZS5zbGFzaGVzO1xuICAgICAgICAgICAgICAgIGZvciAobGV0IHNsYXNoIG9mIHNsYXNoZXMpIHtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPHNsYXNoIHRleHQ9XCIke3NsYXNoLnRleHR9XCIgeD1cIiR7c2xhc2gueH1cIiB5PVwiJHtzbGFzaC55fVwiIGRlZ3JlZT1cIiR7c2xhc2guZGVncmVlfVwiLz5gO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8YmFzZTY0LWRhdGE+YDtcbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8IVtDREFUQVske3ZhbHVlLmJhc2U2NERhdGF9XV0+YDtcbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8L2Jhc2U2NC1kYXRhPmA7XG4gICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPC9zbGFzaC12YWx1ZT5gO1xuICAgICAgICAgICAgfSBlbHNlIGlmICh2YWx1ZS50eXBlID09PSAnY2hhcnQnKSB7XG4gICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPGNoYXJ0LXZhbHVlPmA7XG4gICAgICAgICAgICAgICAgY29uc3QgY2hhcnQgPSB2YWx1ZS5jaGFydDtcbiAgICAgICAgICAgICAgICBjb25zdCBkYXRhc2V0ID0gY2hhcnQuZGF0YXNldDtcbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8ZGF0YXNldCBkYXRhc2V0LW5hbWU9XCIke2RhdGFzZXQuZGF0YXNldE5hbWV9XCIgdHlwZT1cIiR7ZGF0YXNldC50eXBlfVwiYDtcbiAgICAgICAgICAgICAgICBpZiAoZGF0YXNldC5jYXRlZ29yeVByb3BlcnR5KSB7XG4gICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYCBjYXRlZ29yeS1wcm9wZXJ0eT1cIiR7ZGF0YXNldC5jYXRlZ29yeVByb3BlcnR5fVwiYDtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgaWYgKGRhdGFzZXQuc2VyaWVzUHJvcGVydHkpIHtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgIHNlcmllcy1wcm9wZXJ0eT1cIiR7ZGF0YXNldC5zZXJpZXNQcm9wZXJ0eX1cImA7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGlmIChkYXRhc2V0LnNlcmllc1R5cGUpIHtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgIHNlcmllcy10eXBlPVwiJHtkYXRhc2V0LnNlcmllc1R5cGV9XCJgO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBpZiAoZGF0YXNldC5zZXJpZXNUZXh0KSB7XG4gICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYCBzZXJpZXMtdGV4dD1cIiR7ZGF0YXNldC5zZXJpZXNUZXh0fVwiYDtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgaWYgKGRhdGFzZXQudmFsdWVQcm9wZXJ0eSkge1xuICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgdmFsdWUtcHJvcGVydHk9XCIke2RhdGFzZXQudmFsdWVQcm9wZXJ0eX1cImA7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGlmIChkYXRhc2V0LnJQcm9wZXJ0eSkge1xuICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgci1wcm9wZXJ0eT1cIiR7ZGF0YXNldC5yUHJvcGVydHl9XCJgO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBpZiAoZGF0YXNldC54UHJvcGVydHkpIHtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgIHgtcHJvcGVydHk9XCIke2RhdGFzZXQueFByb3BlcnR5fVwiYDtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgaWYgKGRhdGFzZXQueVByb3BlcnR5KSB7XG4gICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYCB5LXByb3BlcnR5PVwiJHtkYXRhc2V0LnlQcm9wZXJ0eX1cImA7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGlmIChkYXRhc2V0LmNvbGxlY3RUeXBlKSB7XG4gICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYCBjb2xsZWN0LXR5cGU9XCIke2RhdGFzZXQuY29sbGVjdFR5cGV9XCJgO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAvPmA7XG4gICAgICAgICAgICAgICAgY29uc3QgeGF4ZXMgPSBjaGFydC54YXhlcztcbiAgICAgICAgICAgICAgICBpZiAoeGF4ZXMpIHtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPHhheGVzYDtcbiAgICAgICAgICAgICAgICAgICAgaWYgKHhheGVzLnJvdGF0aW9uKSB7XG4gICAgICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgcm90YXRpb249XCIke3hheGVzLnJvdGF0aW9ufVwiYDtcbiAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA+YDtcbiAgICAgICAgICAgICAgICAgICAgY29uc3Qgc2NhbGVMYWJlbCA9IHhheGVzLnNjYWxlTGFiZWw7XG4gICAgICAgICAgICAgICAgICAgIGlmIChzY2FsZUxhYmVsKSB7XG4gICAgICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8c2NhbGUtbGFiZWwgZGlzcGxheT1cIiR7c2NhbGVMYWJlbC5kaXNwbGF5fVwiYDtcbiAgICAgICAgICAgICAgICAgICAgICAgIGlmIChzY2FsZUxhYmVsLmxhYmVsU3RyaW5nKSB7XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgIGxhYmVsLXN0cmluZz1cIiR7c2NhbGVMYWJlbC5sYWJlbFN0cmluZ31cImA7XG4gICAgICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAvPmA7XG4gICAgICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPC94YXhlcz5gO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBjb25zdCB5YXhlcyA9IGNoYXJ0LnlheGVzO1xuICAgICAgICAgICAgICAgIGlmICh5YXhlcykge1xuICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8eWF4ZXNgO1xuICAgICAgICAgICAgICAgICAgICBpZiAoeWF4ZXMucm90YXRpb24pIHtcbiAgICAgICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYCByb3RhdGlvbj1cIiR7eWF4ZXMucm90YXRpb259XCJgO1xuICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYD5gO1xuICAgICAgICAgICAgICAgICAgICBjb25zdCBzY2FsZUxhYmVsID0geWF4ZXMuc2NhbGVMYWJlbDtcbiAgICAgICAgICAgICAgICAgICAgaWYgKHNjYWxlTGFiZWwpIHtcbiAgICAgICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDxzY2FsZS1sYWJlbCBkaXNwbGF5PVwiJHtzY2FsZUxhYmVsLmRpc3BsYXl9XCJgO1xuICAgICAgICAgICAgICAgICAgICAgICAgaWYgKHNjYWxlTGFiZWwubGFiZWxTdHJpbmcpIHtcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgbGFiZWwtc3RyaW5nPVwiJHtzY2FsZUxhYmVsLmxhYmVsU3RyaW5nfVwiYDtcbiAgICAgICAgICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYC8+YDtcbiAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8L3lheGVzPmA7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGNvbnN0IG9wdGlvbnMgPSBjaGFydC5vcHRpb25zO1xuICAgICAgICAgICAgICAgIGlmIChvcHRpb25zKSB7XG4gICAgICAgICAgICAgICAgICAgIGZvciAobGV0IG9wdGlvbiBvZiBvcHRpb25zKSB7XG4gICAgICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8b3B0aW9uIHR5cGU9XCIke29wdGlvbi50eXBlfVwiYDtcbiAgICAgICAgICAgICAgICAgICAgICAgIGlmIChvcHRpb24ucG9zaXRpb24pIHtcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgcG9zaXRpb249XCIke29wdGlvbi5wb3NpdGlvbn1cImA7XG4gICAgICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgICAgICAgICBpZiAob3B0aW9uLmRpc3BsYXkgIT09IHVuZGVmaW5lZCAmJiBvcHRpb24uZGlzcGxheSAhPT0gbnVsbCkge1xuICAgICAgICAgICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYCBkaXNwbGF5PVwiJHtvcHRpb24uZGlzcGxheX1cImA7XG4gICAgICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgICAgICAgICBpZiAob3B0aW9uLmR1cmF0aW9uKSB7XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgIGR1cmF0aW9uPVwiJHtvcHRpb24uZHVyYXRpb259XCJgO1xuICAgICAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgICAgICAgICAgaWYgKG9wdGlvbi5lYXNpbmcpIHtcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgZWFzaW5nPVwiJHtvcHRpb24uZWFzaW5nfVwiYDtcbiAgICAgICAgICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICAgICAgICAgIGlmIChvcHRpb24udGV4dCkge1xuICAgICAgICAgICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYCB0ZXh0PVwiJHtvcHRpb24udGV4dH1cImA7XG4gICAgICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAvPmA7XG4gICAgICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgY29uc3QgcGx1Z2lucyA9IGNoYXJ0LnBsdWdpbnMgfHwgW107XG4gICAgICAgICAgICAgICAgZm9yIChsZXQgcGx1Z2luIG9mIHBsdWdpbnMpIHtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPHBsdWdpbiBuYW1lPVwiJHtwbHVnaW4ubmFtZX1cIiBkaXNwbGF5PVwiJHtwbHVnaW4uZGlzcGxheX1cIi8+YDtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgaWYgKHBsdWdpbnMpIHtcblxuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8L2NoYXJ0LXZhbHVlPmA7XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBjb25zdCBwcm9wZXJ0eUNvbmRpdGlvbnMgPSBjZWxsRGVmLmNvbmRpdGlvblByb3BlcnR5SXRlbXMgfHwgW107XG4gICAgICAgICAgICBmb3IgKGxldCBwYyBvZiBwcm9wZXJ0eUNvbmRpdGlvbnMpIHtcbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8Y29uZGl0aW9uLXByb3BlcnR5LWl0ZW0gbmFtZT1cIiR7cGMubmFtZX1cImA7XG4gICAgICAgICAgICAgICAgY29uc3Qgcm93SGVpZ2h0ID0gcGMucm93SGVpZ2h0O1xuICAgICAgICAgICAgICAgIGlmIChyb3dIZWlnaHQgIT09IG51bGwgJiYgcm93SGVpZ2h0ICE9PSB1bmRlZmluZWQgJiYgcm93SGVpZ2h0ICE9PSAtMSkge1xuICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgcm93LWhlaWdodD1cIiR7cm93SGVpZ2h0fVwiYDtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgY29uc3QgY29sV2lkdGggPSBwYy5jb2xXaWR0aDtcbiAgICAgICAgICAgICAgICBpZiAoY29sV2lkdGggIT09IG51bGwgJiYgY29sV2lkdGggIT09IHVuZGVmaW5lZCAmJiBjb2xXaWR0aCAhPT0gLTEpIHtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgIGNvbC13aWR0aD1cIiR7Y29sV2lkdGh9XCJgO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBpZiAocGMubmV3VmFsdWUgJiYgcGMubmV3VmFsdWUgIT09ICcnKSB7XG4gICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYCBuZXctdmFsdWU9XCIke3BjLm5ld1ZhbHVlfVwiYDtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgaWYgKHBjLmxpbmtVcmwgJiYgcGMubGlua1VybCAhPT0gJycpIHtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgIGxpbmstdXJsPVwiJHtwYy5saW5rVXJsfVwiYDtcbiAgICAgICAgICAgICAgICAgICAgbGV0IHRhcmdldFdpbmRvdyA9IHBjLmxpbmtUYXJnZXRXaW5kb3c7XG4gICAgICAgICAgICAgICAgICAgIGlmICghdGFyZ2V0V2luZG93IHx8IHRhcmdldFdpbmRvdyA9PT0gJycpIHtcbiAgICAgICAgICAgICAgICAgICAgICAgIHRhcmdldFdpbmRvdyA9IFwiX3NlbGZcIjtcbiAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgbGluay10YXJnZXQtd2luZG93PVwiJHtwYy5saW5rVGFyZ2V0V2luZG93fVwiYDtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPmA7XG4gICAgICAgICAgICAgICAgY29uc3QgcGFnaW5nID0gcGMucGFnaW5nO1xuICAgICAgICAgICAgICAgIGlmIChwYWdpbmcpIHtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPHBhZ2luZyBwb3NpdGlvbj1cIiR7cGFnaW5nLnBvc2l0aW9ufVwiIGxpbmU9XCIke3BhZ2luZy5saW5lfVwiLz5gO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBpZiAocGMubGlua1BhcmFtZXRlcnMgJiYgcGMubGlua1BhcmFtZXRlcnMubGVuZ3RoID4gMCkge1xuICAgICAgICAgICAgICAgICAgICBmb3IgKGxldCBwYXJhbSBvZiBwYy5saW5rUGFyYW1ldGVycykge1xuICAgICAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPGxpbmstcGFyYW1ldGVyIG5hbWU9XCIke3BhcmFtLm5hbWV9XCI+YDtcbiAgICAgICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDx2YWx1ZT48IVtDREFUQVske3BhcmFtLnZhbHVlfV1dPjwvdmFsdWU+YDtcbiAgICAgICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDwvbGluay1wYXJhbWV0ZXI+YDtcbiAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBjb25zdCBzdHlsZSA9IHBjLmNlbGxTdHlsZTtcbiAgICAgICAgICAgICAgICBpZiAoc3R5bGUpIHtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBidWlsZENlbGxTdHlsZShzdHlsZSwgdHJ1ZSk7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYnVpbGRDb25kaXRpb25zKHBjLmNvbmRpdGlvbnMpO1xuICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDwvY29uZGl0aW9uLXByb3BlcnR5LWl0ZW0+YDtcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIGNlbGxYbWwgKz0gJzwvY2VsbD4nO1xuICAgICAgICB9XG4gICAgfVxuICAgIHhtbCArPSBjZWxsWG1sO1xuICAgIHhtbCArPSByb3dzWG1sO1xuICAgIHhtbCArPSBjb2x1bW5YbWw7XG4gICAgY29uc3QgaGVhZGVyID0gY29udGV4dC5yZXBvcnREZWYuaGVhZGVyO1xuICAgIGlmIChoZWFkZXIgJiYgKGhlYWRlci5sZWZ0IHx8IGhlYWRlci5jZW50ZXIgfHwgaGVhZGVyLnJpZ2h0KSkge1xuICAgICAgICB4bWwgKz0gJzxoZWFkZXIgJztcbiAgICAgICAgaWYgKGhlYWRlci5mb250RmFtaWx5KSB7XG4gICAgICAgICAgICB4bWwgKz0gYCBmb250LWZhbWlseT1cIiR7aGVhZGVyLmZvbnRGYW1pbHl9XCJgXG4gICAgICAgIH1cbiAgICAgICAgaWYgKGhlYWRlci5mb250U2l6ZSkge1xuICAgICAgICAgICAgeG1sICs9IGAgZm9udC1zaXplPVwiJHtoZWFkZXIuZm9udFNpemV9XCJgXG4gICAgICAgIH1cbiAgICAgICAgaWYgKGhlYWRlci5mb3JlY29sb3IpIHtcbiAgICAgICAgICAgIHhtbCArPSBgIGZvcmVjb2xvcj1cIiR7aGVhZGVyLmZvcmVjb2xvcn1cImBcbiAgICAgICAgfVxuICAgICAgICBpZiAoaGVhZGVyLmJvbGQpIHtcbiAgICAgICAgICAgIHhtbCArPSBgIGJvbGQ9XCIke2hlYWRlci5ib2xkfVwiYFxuICAgICAgICB9XG4gICAgICAgIGlmIChoZWFkZXIuaXRhbGljKSB7XG4gICAgICAgICAgICB4bWwgKz0gYCBpdGFsaWM9XCIke2hlYWRlci5pdGFsaWN9XCJgXG4gICAgICAgIH1cbiAgICAgICAgaWYgKGhlYWRlci51bmRlcmxpbmUpIHtcbiAgICAgICAgICAgIHhtbCArPSBgIHVuZGVybGluZT1cIiR7aGVhZGVyLnVuZGVybGluZX1cImBcbiAgICAgICAgfVxuICAgICAgICBpZiAoaGVhZGVyLm1hcmdpbikge1xuICAgICAgICAgICAgeG1sICs9IGAgbWFyZ2luPVwiJHtoZWFkZXIubWFyZ2lufVwiYFxuICAgICAgICB9XG4gICAgICAgIHhtbCArPSAnPic7XG4gICAgICAgIGlmIChoZWFkZXIubGVmdCkge1xuICAgICAgICAgICAgeG1sICs9IGA8bGVmdD48IVtDREFUQVske2hlYWRlci5sZWZ0fV1dPjwvbGVmdD5gO1xuICAgICAgICB9XG4gICAgICAgIGlmIChoZWFkZXIuY2VudGVyKSB7XG4gICAgICAgICAgICB4bWwgKz0gYDxjZW50ZXI+PCFbQ0RBVEFbJHtoZWFkZXIuY2VudGVyfV1dPjwvY2VudGVyPmA7XG4gICAgICAgIH1cbiAgICAgICAgaWYgKGhlYWRlci5yaWdodCkge1xuICAgICAgICAgICAgeG1sICs9IGA8cmlnaHQ+PCFbQ0RBVEFbJHtoZWFkZXIucmlnaHR9XV0+PC9yaWdodD5gO1xuICAgICAgICB9XG4gICAgICAgIHhtbCArPSAnPC9oZWFkZXI+JztcbiAgICB9XG4gICAgY29uc3QgZm9vdGVyID0gY29udGV4dC5yZXBvcnREZWYuZm9vdGVyO1xuICAgIGlmIChmb290ZXIgJiYgKGZvb3Rlci5sZWZ0IHx8IGZvb3Rlci5jZW50ZXIgfHwgZm9vdGVyLnJpZ2h0KSkge1xuICAgICAgICB4bWwgKz0gJzxmb290ZXIgJztcbiAgICAgICAgaWYgKGZvb3Rlci5mb250RmFtaWx5KSB7XG4gICAgICAgICAgICB4bWwgKz0gYCBmb250LWZhbWlseT1cIiR7Zm9vdGVyLmZvbnRGYW1pbHl9XCJgXG4gICAgICAgIH1cbiAgICAgICAgaWYgKGZvb3Rlci5mb250U2l6ZSkge1xuICAgICAgICAgICAgeG1sICs9IGAgZm9udC1zaXplPVwiJHtmb290ZXIuZm9udFNpemV9XCJgXG4gICAgICAgIH1cbiAgICAgICAgaWYgKGZvb3Rlci5mb3JlY29sb3IpIHtcbiAgICAgICAgICAgIHhtbCArPSBgIGZvcmVjb2xvcj1cIiR7Zm9vdGVyLmZvcmVjb2xvcn1cImBcbiAgICAgICAgfVxuICAgICAgICBpZiAoZm9vdGVyLmJvbGQpIHtcbiAgICAgICAgICAgIHhtbCArPSBgIGJvbGQ9XCIke2Zvb3Rlci5ib2xkfVwiYFxuICAgICAgICB9XG4gICAgICAgIGlmIChmb290ZXIuaXRhbGljKSB7XG4gICAgICAgICAgICB4bWwgKz0gYCBpdGFsaWM9XCIke2Zvb3Rlci5pdGFsaWN9XCJgXG4gICAgICAgIH1cbiAgICAgICAgaWYgKGZvb3Rlci51bmRlcmxpbmUpIHtcbiAgICAgICAgICAgIHhtbCArPSBgIHVuZGVybGluZT1cIiR7Zm9vdGVyLnVuZGVybGluZX1cImBcbiAgICAgICAgfVxuICAgICAgICBpZiAoZm9vdGVyLm1hcmdpbikge1xuICAgICAgICAgICAgeG1sICs9IGAgbWFyZ2luPVwiJHtmb290ZXIubWFyZ2lufVwiYFxuICAgICAgICB9XG4gICAgICAgIHhtbCArPSAnPic7XG4gICAgICAgIGlmIChmb290ZXIubGVmdCkge1xuICAgICAgICAgICAgeG1sICs9IGA8bGVmdD48IVtDREFUQVske2Zvb3Rlci5sZWZ0fV1dPjwvbGVmdD5gO1xuICAgICAgICB9XG4gICAgICAgIGlmIChmb290ZXIuY2VudGVyKSB7XG4gICAgICAgICAgICB4bWwgKz0gYDxjZW50ZXI+PCFbQ0RBVEFbJHtmb290ZXIuY2VudGVyfV1dPjwvY2VudGVyPmA7XG4gICAgICAgIH1cbiAgICAgICAgaWYgKGZvb3Rlci5yaWdodCkge1xuICAgICAgICAgICAgeG1sICs9IGA8cmlnaHQ+PCFbQ0RBVEFbJHtmb290ZXIucmlnaHR9XV0+PC9yaWdodD5gO1xuICAgICAgICB9XG4gICAgICAgIHhtbCArPSAnPC9mb290ZXI+JztcbiAgICB9XG4gICAgbGV0IGRhdGFzb3VyY2VYbWwgPSBcIlwiO1xuICAgIGNvbnN0IGRhdGFzb3VyY2VzID0gY29udGV4dC5yZXBvcnREZWYuZGF0YXNvdXJjZXM7XG4gICAgZm9yIChsZXQgZGF0YXNvdXJjZSBvZiBkYXRhc291cmNlcykge1xuICAgICAgICBsZXQgZHMgPSBgPGRhdGFzb3VyY2UgbmFtZT1cIiR7ZW5jb2RlKGRhdGFzb3VyY2UubmFtZSl9XCIgdHlwZT1cIiR7ZGF0YXNvdXJjZS50eXBlfVwiYDtcbiAgICAgICAgbGV0IHR5cGUgPSBkYXRhc291cmNlLnR5cGU7XG4gICAgICAgIGlmICh0eXBlID09PSAnamRiYycpIHtcbiAgICAgICAgICAgIGRzICs9IGAgdXNlcm5hbWU9XCIke2VuY29kZShkYXRhc291cmNlLnVzZXJuYW1lKX1cImA7XG4gICAgICAgICAgICBkcyArPSBgIHBhc3N3b3JkPVwiJHtlbmNvZGUoZGF0YXNvdXJjZS5wYXNzd29yZCl9XCJgO1xuICAgICAgICAgICAgZHMgKz0gYCB1cmw9XCIke2VuY29kZShkYXRhc291cmNlLnVybCl9XCJgO1xuICAgICAgICAgICAgZHMgKz0gYCBkcml2ZXI9XCIke2RhdGFzb3VyY2UuZHJpdmVyfVwiYDtcbiAgICAgICAgICAgIGRzICs9ICc+JztcbiAgICAgICAgICAgIGZvciAobGV0IGRhdGFzZXQgb2YgZGF0YXNvdXJjZS5kYXRhc2V0cykge1xuICAgICAgICAgICAgICAgIGRzICs9IGA8ZGF0YXNldCBuYW1lPVwiJHtlbmNvZGUoZGF0YXNldC5uYW1lKX1cIiB0eXBlPVwic3FsXCI+YDtcbiAgICAgICAgICAgICAgICBkcyArPSBgPHNxbD48IVtDREFUQVske2RhdGFzZXQuc3FsfV1dPjwvc3FsPmA7XG4gICAgICAgICAgICAgICAgZm9yIChsZXQgZmllbGQgb2YgZGF0YXNldC5maWVsZHMpIHtcbiAgICAgICAgICAgICAgICAgICAgZHMgKz0gYDxmaWVsZCBuYW1lPVwiJHtmaWVsZC5uYW1lfVwiLz5gO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBmb3IgKGxldCBwYXJhbWV0ZXIgb2YgZGF0YXNldC5wYXJhbWV0ZXJzKSB7XG4gICAgICAgICAgICAgICAgICAgIGRzICs9IGA8cGFyYW1ldGVyIG5hbWU9XCIke2VuY29kZShwYXJhbWV0ZXIubmFtZSl9XCIgdHlwZT1cIiR7cGFyYW1ldGVyLnR5cGV9XCIgZGVmYXVsdC12YWx1ZT1cIiR7ZW5jb2RlKHBhcmFtZXRlci5kZWZhdWx0VmFsdWUpfVwiLz5gO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBkcyArPSBgPC9kYXRhc2V0PmA7XG4gICAgICAgICAgICB9XG4gICAgICAgIH0gZWxzZSBpZiAodHlwZSA9PT0gJ3NwcmluZycpIHtcbiAgICAgICAgICAgIGRzICs9IGAgYmVhbj1cIiR7ZGF0YXNvdXJjZS5iZWFuSWR9XCI+YDtcbiAgICAgICAgICAgIGZvciAobGV0IGRhdGFzZXQgb2YgZGF0YXNvdXJjZS5kYXRhc2V0cykge1xuICAgICAgICAgICAgICAgIGRzICs9IGA8ZGF0YXNldCBuYW1lPVwiJHtlbmNvZGUoZGF0YXNldC5uYW1lKX1cIiB0eXBlPVwiYmVhblwiIG1ldGhvZD1cIiR7ZGF0YXNldC5tZXRob2R9XCIgY2xheno9XCIke2RhdGFzZXQuY2xhenp9XCI+YDtcbiAgICAgICAgICAgICAgICBmb3IgKGxldCBmaWVsZCBvZiBkYXRhc2V0LmZpZWxkcykge1xuICAgICAgICAgICAgICAgICAgICBkcyArPSBgPGZpZWxkIG5hbWU9XCIke2ZpZWxkLm5hbWV9XCIvPmA7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGRzICs9IGA8L2RhdGFzZXQ+YDtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfSBlbHNlIGlmICh0eXBlID09PSAnYnVpbGRpbicpIHtcbiAgICAgICAgICAgIGRzICs9ICc+JztcbiAgICAgICAgICAgIGZvciAobGV0IGRhdGFzZXQgb2YgZGF0YXNvdXJjZS5kYXRhc2V0cykge1xuICAgICAgICAgICAgICAgIGRzICs9IGA8ZGF0YXNldCBuYW1lPVwiJHtlbmNvZGUoZGF0YXNldC5uYW1lKX1cIiB0eXBlPVwic3FsXCI+YDtcbiAgICAgICAgICAgICAgICBkcyArPSBgPHNxbD48IVtDREFUQVske2RhdGFzZXQuc3FsfV1dPjwvc3FsPmA7XG4gICAgICAgICAgICAgICAgZm9yIChsZXQgZmllbGQgb2YgZGF0YXNldC5maWVsZHMpIHtcbiAgICAgICAgICAgICAgICAgICAgZHMgKz0gYDxmaWVsZCBuYW1lPVwiJHtmaWVsZC5uYW1lfVwiLz5gO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBmb3IgKGxldCBwYXJhbWV0ZXIgb2YgZGF0YXNldC5wYXJhbWV0ZXJzKSB7XG4gICAgICAgICAgICAgICAgICAgIGRzICs9IGA8cGFyYW1ldGVyIG5hbWU9XCIke3BhcmFtZXRlci5uYW1lfVwiIHR5cGU9XCIke3BhcmFtZXRlci50eXBlfVwiIGRlZmF1bHQtdmFsdWU9XCIke3BhcmFtZXRlci5kZWZhdWx0VmFsdWV9XCIvPmA7XG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIGRzICs9IGA8L2RhdGFzZXQ+YDtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfVxuICAgICAgICBkcyArPSBcIjwvZGF0YXNvdXJjZT5cIjtcbiAgICAgICAgZGF0YXNvdXJjZVhtbCArPSBkcztcbiAgICB9XG4gICAgeG1sICs9IGRhdGFzb3VyY2VYbWw7XG4gICAgY29uc3QgcGFwZXIgPSBjb250ZXh0LnJlcG9ydERlZi5wYXBlcjtcbiAgICBsZXQgaHRtbEludGVydmFsUmVmcmVzaFZhbHVlID0gMDtcbiAgICBpZiAocGFwZXIuaHRtbEludGVydmFsUmVmcmVzaFZhbHVlICE9PSBudWxsICYmIHBhcGVyLmh0bWxJbnRlcnZhbFJlZnJlc2hWYWx1ZSAhPT0gdW5kZWZpbmVkKSB7XG4gICAgICAgIGh0bWxJbnRlcnZhbFJlZnJlc2hWYWx1ZSA9IHBhcGVyLmh0bWxJbnRlcnZhbFJlZnJlc2hWYWx1ZTtcbiAgICB9XG4gICAgeG1sICs9IGA8cGFwZXIgdHlwZT1cIiR7cGFwZXIucGFwZXJUeXBlfVwiIGxlZnQtbWFyZ2luPVwiJHtwYXBlci5sZWZ0TWFyZ2lufVwiIHJpZ2h0LW1hcmdpbj1cIiR7cGFwZXIucmlnaHRNYXJnaW59XCJcbiAgICB0b3AtbWFyZ2luPVwiJHtwYXBlci50b3BNYXJnaW59XCIgYm90dG9tLW1hcmdpbj1cIiR7cGFwZXIuYm90dG9tTWFyZ2lufVwiIHBhZ2luZy1tb2RlPVwiJHtwYXBlci5wYWdpbmdNb2RlfVwiIGZpeHJvd3M9XCIke3BhcGVyLmZpeFJvd3N9XCJcbiAgICB3aWR0aD1cIiR7cGFwZXIud2lkdGh9XCIgaGVpZ2h0PVwiJHtwYXBlci5oZWlnaHR9XCIgb3JpZW50YXRpb249XCIke3BhcGVyLm9yaWVudGF0aW9ufVwiIGh0bWwtcmVwb3J0LWFsaWduPVwiJHtwYXBlci5odG1sUmVwb3J0QWxpZ259XCIgYmctaW1hZ2U9XCIke3BhcGVyLmJnSW1hZ2UgfHwgJyd9XCIgaHRtbC1pbnRlcnZhbC1yZWZyZXNoLXZhbHVlPVwiJHtodG1sSW50ZXJ2YWxSZWZyZXNoVmFsdWV9XCIgY29sdW1uLWVuYWJsZWQ9XCIke3BhcGVyLmNvbHVtbkVuYWJsZWR9XCJgO1xuICAgIGlmIChwYXBlci5jb2x1bW5FbmFibGVkKSB7XG4gICAgICAgIHhtbCArPSBgIGNvbHVtbi1jb3VudD1cIiR7cGFwZXIuY29sdW1uQ291bnR9XCIgY29sdW1uLW1hcmdpbj1cIiR7cGFwZXIuY29sdW1uTWFyZ2lufVwiYDtcbiAgICB9XG4gICAgeG1sICs9IGA+PC9wYXBlcj5gO1xuICAgIGlmIChjb250ZXh0LnJlcG9ydERlZi5zZWFyY2hGb3JtWG1sKSB7XG4gICAgICAgIHhtbCArPSBjb250ZXh0LnJlcG9ydERlZi5zZWFyY2hGb3JtWG1sO1xuICAgIH1cbiAgICB4bWwgKz0gYDwvdXJlcG9ydD5gO1xuICAgIHhtbCA9IGVuY29kZVVSSUNvbXBvbmVudCh4bWwpO1xuICAgIHJldHVybiB4bWw7XG59O1xuXG5mdW5jdGlvbiBnZXRTcGFuKGhvdCwgcm93LCBjb2wpIHtcbiAgICBjb25zdCBtZXJnZUNlbGxzID0gaG90LmdldFNldHRpbmdzKCkubWVyZ2VDZWxscyB8fCBbXTtcbiAgICBmb3IgKGxldCBpdGVtIG9mIG1lcmdlQ2VsbHMpIHtcbiAgICAgICAgaWYgKGl0ZW0ucm93ID09PSByb3cgJiYgaXRlbS5jb2wgPT09IGNvbCkge1xuICAgICAgICAgICAgcmV0dXJuIGl0ZW07XG4gICAgICAgIH1cbiAgICB9XG4gICAgcmV0dXJuIHtcbiAgICAgICAgcm93c3BhbjogMCxcbiAgICAgICAgY29sc3BhbjogMFxuICAgIH07XG59XG5cbmZ1bmN0aW9uIGJ1aWxkQ29uZGl0aW9ucyhjb25kaXRpb25zKSB7XG4gICAgbGV0IGNlbGxYbWwgPSAnJztcbiAgICBpZiAoY29uZGl0aW9ucykge1xuICAgICAgICBjb25zdCBzaXplID0gY29uZGl0aW9ucy5sZW5ndGg7XG4gICAgICAgIGZvciAobGV0IGNvbmRpdGlvbiBvZiBjb25kaXRpb25zKSB7XG4gICAgICAgICAgICBpZiAoIWNvbmRpdGlvbi50eXBlIHx8IGNvbmRpdGlvbi50eXBlID09PSAncHJvcGVydHknKSB7XG4gICAgICAgICAgICAgICAgaWYgKGNvbmRpdGlvbi5sZWZ0KSB7XG4gICAgICAgICAgICAgICAgICAgIGNlbGxYbWwgKz0gYDxjb25kaXRpb24gcHJvcGVydHk9XCIke2NvbmRpdGlvbi5sZWZ0fVwiIG9wPVwiJHtlbmNvZGUoY29uZGl0aW9uLm9wZXJhdGlvbil9XCIgaWQ9XCIke2NvbmRpdGlvbi5pZH1cImA7XG4gICAgICAgICAgICAgICAgfSBlbHNlIHtcbiAgICAgICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPGNvbmRpdGlvbiBvcD1cIiR7ZW5jb2RlKGNvbmRpdGlvbi5vcGVyYXRpb24pfVwiIGlkPVwiJHtjb25kaXRpb24uaWR9XCJgO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgdHlwZT1cIiR7Y29uZGl0aW9uLnR5cGV9XCJgO1xuICAgICAgICAgICAgICAgIGlmIChjb25kaXRpb24uam9pbiAmJiBzaXplID4gMSkge1xuICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgam9pbj1cIiR7Y29uZGl0aW9uLmpvaW59XCI+YDtcbiAgICAgICAgICAgICAgICB9IGVsc2Uge1xuICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA+YDtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPHZhbHVlPjwhW0NEQVRBWyR7Y29uZGl0aW9uLnJpZ2h0fV1dPjwvdmFsdWU+YDtcbiAgICAgICAgICAgIH0gZWxzZSB7XG4gICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPGNvbmRpdGlvbiB0eXBlPVwiJHtjb25kaXRpb24udHlwZX1cIiBvcD1cIiR7ZW5jb2RlKGNvbmRpdGlvbi5vcGVyYXRpb24pfVwiIGlkPVwiJHtjb25kaXRpb24uaWR9XCJgO1xuICAgICAgICAgICAgICAgIGlmIChjb25kaXRpb24uam9pbiAmJiBzaXplID4gMSkge1xuICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGAgam9pbj1cIiR7Y29uZGl0aW9uLmpvaW59XCI+YDtcbiAgICAgICAgICAgICAgICB9IGVsc2Uge1xuICAgICAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA+YDtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgY2VsbFhtbCArPSBgPGxlZnQ+PCFbQ0RBVEFbJHtjb25kaXRpb24ubGVmdH1dXT48L2xlZnQ+YDtcbiAgICAgICAgICAgICAgICBjZWxsWG1sICs9IGA8cmlnaHQ+PCFbQ0RBVEFbJHtjb25kaXRpb24ucmlnaHR9XV0+PC9yaWdodD5gO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgY2VsbFhtbCArPSBgPC9jb25kaXRpb24+YDtcbiAgICAgICAgfVxuICAgIH1cbiAgICByZXR1cm4gY2VsbFhtbDtcbn07XG5cbmZ1bmN0aW9uIGJ1aWxkQ2VsbFN0eWxlKGNlbGxTdHlsZSwgY29uZGl0aW9uKSB7XG4gICAgbGV0IGNlbGxYbWwgPSBcIjxjZWxsLXN0eWxlXCI7XG4gICAgaWYgKGNvbmRpdGlvbikge1xuICAgICAgICBjZWxsWG1sICs9IGAgZm9yLWNvbmRpdGlvbj1cInRydWVcImA7XG4gICAgfVxuICAgIGlmIChjZWxsU3R5bGUuZm9udFNpemUgJiYgY2VsbFN0eWxlLmZvbnRTaXplICE9PSAnJykge1xuICAgICAgICBjZWxsWG1sICs9IGAgZm9udC1zaXplPVwiJHtjZWxsU3R5bGUuZm9udFNpemV9XCJgO1xuICAgIH1cbiAgICBpZiAoY2VsbFN0eWxlLmZvbnRTaXplU2NvcGUpIHtcbiAgICAgICAgY2VsbFhtbCArPSBgIGZvbnQtc2l6ZS1zY29wZT1cIiR7Y2VsbFN0eWxlLmZvbnRTaXplU2NvcGV9XCJgO1xuICAgIH1cbiAgICBpZiAoY2VsbFN0eWxlLmZvcmVjb2xvciAmJiBjZWxsU3R5bGUuZm9yZWNvbG9yICE9PSAnJykge1xuICAgICAgICBjZWxsWG1sICs9IGAgZm9yZWNvbG9yPVwiJHtjZWxsU3R5bGUuZm9yZWNvbG9yfVwiYDtcbiAgICB9XG4gICAgaWYgKGNlbGxTdHlsZS5mb3JlY29sb3JTY29wZSkge1xuICAgICAgICBjZWxsWG1sICs9IGAgZm9yZWNvbG9yLXNjb3BlPVwiJHtjZWxsU3R5bGUuZm9yZWNvbG9yU2NvcGV9XCJgO1xuICAgIH1cbiAgICBpZiAoY2VsbFN0eWxlLmZvbnRGYW1pbHkpIHtcbiAgICAgICAgaWYgKGNlbGxTdHlsZS5mb250RmFtaWx5ID09PSAnMCcpIHtcbiAgICAgICAgICAgIGNlbGxYbWwgKz0gYCBmb250LWZhbWlseT1cIlwiYDtcbiAgICAgICAgfSBlbHNlIHtcbiAgICAgICAgICAgIGNlbGxYbWwgKz0gYCBmb250LWZhbWlseT1cIiR7Y2VsbFN0eWxlLmZvbnRGYW1pbHl9XCJgO1xuICAgICAgICB9XG4gICAgfVxuICAgIGlmIChjZWxsU3R5bGUuZm9udEZhbWlseVNjb3BlKSB7XG4gICAgICAgIGNlbGxYbWwgKz0gYCBmb250LWZhbWlseS1zY29wZT1cIiR7Y2VsbFN0eWxlLmZvbnRGYW1pbHlTY29wZX1cImA7XG4gICAgfVxuICAgIGlmIChjZWxsU3R5bGUuYmdjb2xvciAmJiBjZWxsU3R5bGUuYmdjb2xvciAhPT0gJycpIHtcbiAgICAgICAgY2VsbFhtbCArPSBgIGJnY29sb3I9XCIke2NlbGxTdHlsZS5iZ2NvbG9yfVwiYDtcbiAgICB9XG4gICAgaWYgKGNlbGxTdHlsZS5iZ2NvbG9yU2NvcGUpIHtcbiAgICAgICAgY2VsbFhtbCArPSBgIGJnY29sb3Itc2NvcGU9XCIke2NlbGxTdHlsZS5iZ2NvbG9yU2NvcGV9XCJgO1xuICAgIH1cbiAgICBpZiAoY2VsbFN0eWxlLmZvcm1hdCAmJiBjZWxsU3R5bGUuZm9ybWF0ICE9PSAnJykge1xuICAgICAgICBjZWxsWG1sICs9IGAgZm9ybWF0PVwiJHtjZWxsU3R5bGUuZm9ybWF0fVwiYDtcbiAgICB9XG4gICAgaWYgKGNlbGxTdHlsZS5ib2xkICE9PSB1bmRlZmluZWQgJiYgY2VsbFN0eWxlLmJvbGQgIT09IG51bGwpIHtcbiAgICAgICAgY2VsbFhtbCArPSBgIGJvbGQ9XCIke2NlbGxTdHlsZS5ib2xkfVwiYDtcbiAgICB9XG4gICAgaWYgKGNlbGxTdHlsZS5ib2xkU2NvcGUpIHtcbiAgICAgICAgY2VsbFhtbCArPSBgIGJvbGQtc2NvcGU9XCIke2NlbGxTdHlsZS5ib2xkU2NvcGV9XCJgO1xuICAgIH1cbiAgICBpZiAoY2VsbFN0eWxlLml0YWxpYyAhPT0gdW5kZWZpbmVkICYmIGNlbGxTdHlsZS5pdGFsaWMgIT09IG51bGwpIHtcbiAgICAgICAgY2VsbFhtbCArPSBgIGl0YWxpYz1cIiR7Y2VsbFN0eWxlLml0YWxpY31cImA7XG4gICAgfVxuICAgIGlmIChjZWxsU3R5bGUuaXRhbGljU2NvcGUpIHtcbiAgICAgICAgY2VsbFhtbCArPSBgIGl0YWxpYy1zY29wZT1cIiR7Y2VsbFN0eWxlLml0YWxpY1Njb3BlfVwiYDtcbiAgICB9XG4gICAgaWYgKGNlbGxTdHlsZS51bmRlcmxpbmUgIT09IHVuZGVmaW5lZCAmJiBjZWxsU3R5bGUudW5kZXJsaW5lICE9PSBudWxsKSB7XG4gICAgICAgIGNlbGxYbWwgKz0gYCB1bmRlcmxpbmU9XCIke2NlbGxTdHlsZS51bmRlcmxpbmV9XCJgO1xuICAgIH1cbiAgICBpZiAoY2VsbFN0eWxlLnVuZGVybGluZVNjb3BlKSB7XG4gICAgICAgIGNlbGxYbWwgKz0gYCB1bmRlcmxpbmUtc2NvcGU9XCIke2NlbGxTdHlsZS51bmRlcmxpbmVTY29wZX1cImA7XG4gICAgfVxuICAgIGlmIChjZWxsU3R5bGUud3JhcENvbXB1dGUgIT09IHVuZGVmaW5lZCAmJiBjZWxsU3R5bGUud3JhcENvbXB1dGUgIT09IG51bGwpIHtcbiAgICAgICAgY2VsbFhtbCArPSBgIHdyYXAtY29tcHV0ZT1cIiR7Y2VsbFN0eWxlLndyYXBDb21wdXRlfVwiYDtcbiAgICB9XG4gICAgaWYgKGNlbGxTdHlsZS5hbGlnbiAmJiBjZWxsU3R5bGUuYWxpZ24gIT09ICcnKSB7XG4gICAgICAgIGNlbGxYbWwgKz0gYCBhbGlnbj1cIiR7Y2VsbFN0eWxlLmFsaWdufVwiYDtcbiAgICB9XG4gICAgaWYgKGNlbGxTdHlsZS5hbGlnblNjb3BlKSB7XG4gICAgICAgIGNlbGxYbWwgKz0gYCBhbGlnbi1zY29wZT1cIiR7Y2VsbFN0eWxlLmFsaWduU2NvcGV9XCJgO1xuICAgIH1cbiAgICBpZiAoY2VsbFN0eWxlLnZhbGlnbiAmJiBjZWxsU3R5bGUudmFsaWduICE9PSAnJykge1xuICAgICAgICBjZWxsWG1sICs9IGAgdmFsaWduPVwiJHtjZWxsU3R5bGUudmFsaWdufVwiYDtcbiAgICB9XG4gICAgaWYgKGNlbGxTdHlsZS52YWxpZ25TY29wZSkge1xuICAgICAgICBjZWxsWG1sICs9IGAgdmFsaWduLXNjb3BlPVwiJHtjZWxsU3R5bGUudmFsaWduU2NvcGV9XCJgO1xuICAgIH1cbiAgICBpZiAoY2VsbFN0eWxlLmxpbmVIZWlnaHQpIHtcbiAgICAgICAgY2VsbFhtbCArPSBgIGxpbmUtaGVpZ2h0PVwiJHtjZWxsU3R5bGUubGluZUhlaWdodH1cImA7XG4gICAgfVxuICAgIGNlbGxYbWwgKz0gJz4nO1xuICAgIGxldCBsZWZ0Qm9yZGVyID0gY2VsbFN0eWxlLmxlZnRCb3JkZXI7XG4gICAgaWYgKGxlZnRCb3JkZXIgJiYgbGVmdEJvcmRlci5zdHlsZSAhPT0gXCJub25lXCIpIHtcbiAgICAgICAgY2VsbFhtbCArPSBgPGxlZnQtYm9yZGVyIHdpZHRoPVwiJHtsZWZ0Qm9yZGVyLndpZHRofVwiIHN0eWxlPVwiJHtsZWZ0Qm9yZGVyLnN0eWxlfVwiIGNvbG9yPVwiJHtsZWZ0Qm9yZGVyLmNvbG9yfVwiLz5gO1xuICAgIH1cbiAgICBsZXQgcmlnaHRCb3JkZXIgPSBjZWxsU3R5bGUucmlnaHRCb3JkZXI7XG4gICAgaWYgKHJpZ2h0Qm9yZGVyICYmIHJpZ2h0Qm9yZGVyLnN0eWxlICE9PSBcIm5vbmVcIikge1xuICAgICAgICBjZWxsWG1sICs9IGA8cmlnaHQtYm9yZGVyIHdpZHRoPVwiJHtyaWdodEJvcmRlci53aWR0aH1cIiBzdHlsZT1cIiR7cmlnaHRCb3JkZXIuc3R5bGV9XCIgY29sb3I9XCIke3JpZ2h0Qm9yZGVyLmNvbG9yfVwiLz5gO1xuICAgIH1cbiAgICBsZXQgdG9wQm9yZGVyID0gY2VsbFN0eWxlLnRvcEJvcmRlcjtcbiAgICBpZiAodG9wQm9yZGVyICYmIHRvcEJvcmRlci5zdHlsZSAhPT0gXCJub25lXCIpIHtcbiAgICAgICAgY2VsbFhtbCArPSBgPHRvcC1ib3JkZXIgd2lkdGg9XCIke3RvcEJvcmRlci53aWR0aH1cIiBzdHlsZT1cIiR7dG9wQm9yZGVyLnN0eWxlfVwiIGNvbG9yPVwiJHt0b3BCb3JkZXIuY29sb3J9XCIvPmA7XG4gICAgfVxuICAgIGxldCBib3R0b21Cb3JkZXIgPSBjZWxsU3R5bGUuYm90dG9tQm9yZGVyO1xuICAgIGlmIChib3R0b21Cb3JkZXIgJiYgYm90dG9tQm9yZGVyLnN0eWxlICE9PSBcIm5vbmVcIikge1xuICAgICAgICBjZWxsWG1sICs9IGA8Ym90dG9tLWJvcmRlciB3aWR0aD1cIiR7Ym90dG9tQm9yZGVyLndpZHRofVwiIHN0eWxlPVwiJHtib3R0b21Cb3JkZXIuc3R5bGV9XCIgY29sb3I9XCIke2JvdHRvbUJvcmRlci5jb2xvcn1cIi8+YDtcbiAgICB9XG4gICAgY2VsbFhtbCArPSAnPC9jZWxsLXN0eWxlPic7XG4gICAgcmV0dXJuIGNlbGxYbWw7XG59O1xuXG5leHBvcnQgZnVuY3Rpb24gZW5jb2RlKHRleHQpIHtcbiAgICBsZXQgcmVzdWx0ID0gdGV4dC5yZXBsYWNlKC9bPD4mXCJdL2csIGZ1bmN0aW9uIChjKSB7XG4gICAgICAgIHJldHVybiB7XG4gICAgICAgICAgICAnPCc6ICcmbHQ7JyxcbiAgICAgICAgICAgICc+JzogJyZndDsnLFxuICAgICAgICAgICAgJyYnOiAnJmFtcDsnLFxuICAgICAgICAgICAgJ1wiJzogJyZxdW90OydcbiAgICAgICAgfSBbY107XG4gICAgfSk7XG4gICAgcmV0dXJuIHJlc3VsdDtcbn07XG5cbmV4cG9ydCBmdW5jdGlvbiBnZXRQYXJhbWV0ZXIobmFtZSkge1xuICAgIHZhciByZWcgPSBuZXcgUmVnRXhwKFwiKF58JilcIiArIG5hbWUgKyBcIj0oW14mXSopKCZ8JClcIik7XG4gICAgdmFyIHIgPSB3aW5kb3cubG9jYXRpb24uc2VhcmNoLnN1YnN0cigxKS5tYXRjaChyZWcpO1xuICAgIGlmIChyICE9IG51bGwpIHJldHVybiByWzJdO1xuICAgIHJldHVybiBudWxsO1xufTtcblxuZXhwb3J0IGZ1bmN0aW9uIG1tVG9Qb2ludChtbSkge1xuICAgIGxldCB2YWx1ZSA9IG1tICogMi44MzQ2NDY7XG4gICAgcmV0dXJuIE1hdGgucm91bmQodmFsdWUpO1xufTtcbmV4cG9ydCBmdW5jdGlvbiBwb2ludFRvTU0ocG9pbnQpIHtcbiAgICBsZXQgdmFsdWUgPSBwb2ludCAqIDAuMzUyNzc4O1xuICAgIHJldHVybiBNYXRoLnJvdW5kKHZhbHVlKTtcbn07XG5cbmV4cG9ydCBmdW5jdGlvbiBwb2ludFRvUGl4ZWwocG9pbnQpIHtcbiAgICBjb25zdCB2YWx1ZSA9IHBvaW50ICogMS4zMztcbiAgICByZXR1cm4gTWF0aC5yb3VuZCh2YWx1ZSk7XG59O1xuXG5leHBvcnQgZnVuY3Rpb24gcGl4ZWxUb1BvaW50KHBpeGVsKSB7XG4gICAgY29uc3QgdmFsdWUgPSBwaXhlbCAqIDAuNzU7XG4gICAgcmV0dXJuIE1hdGgucm91bmQodmFsdWUpO1xufTtcblxuZXhwb3J0IGZ1bmN0aW9uIHNldERpcnR5KCkge1xuICAgICQoJyNfX3NhdmVfYnRuJykucmVtb3ZlQ2xhc3MoJ2Rpc2FibGVkJyk7XG59O1xuXG5leHBvcnQgZnVuY3Rpb24gcmVzZXREaXJ0eSgpIHtcbiAgICAkKCcjX19zYXZlX2J0bicpLmFkZENsYXNzKCdkaXNhYmxlZCcpO1xufTtcblxuZXhwb3J0IGZ1bmN0aW9uIGZvcm1hdERhdGUoZGF0ZSwgZm9ybWF0KSB7XG4gICAgaWYgKHR5cGVvZiBkYXRlID09PSAnbnVtYmVyJykge1xuICAgICAgICBkYXRlID0gbmV3IERhdGUoZGF0ZSk7XG4gICAgfVxuICAgIGlmICh0eXBlb2YgZGF0ZSA9PT0gJ3N0cmluZycpIHtcbiAgICAgICAgcmV0dXJuIGRhdGU7XG4gICAgfVxuICAgIHZhciBvID0ge1xuICAgICAgICBcIk0rXCI6IGRhdGUuZ2V0TW9udGgoKSArIDEsXG4gICAgICAgIFwiZCtcIjogZGF0ZS5nZXREYXRlKCksXG4gICAgICAgIFwiSCtcIjogZGF0ZS5nZXRIb3VycygpLFxuICAgICAgICBcIm0rXCI6IGRhdGUuZ2V0TWludXRlcygpLFxuICAgICAgICBcInMrXCI6IGRhdGUuZ2V0U2Vjb25kcygpXG4gICAgfTtcbiAgICBpZiAoLyh5KykvLnRlc3QoZm9ybWF0KSlcbiAgICAgICAgZm9ybWF0ID0gZm9ybWF0LnJlcGxhY2UoUmVnRXhwLiQxLCAoZGF0ZS5nZXRGdWxsWWVhcigpICsgXCJcIikuc3Vic3RyKDQgLSBSZWdFeHAuJDEubGVuZ3RoKSk7XG4gICAgZm9yICh2YXIgayBpbiBvKVxuICAgICAgICBpZiAobmV3IFJlZ0V4cChcIihcIiArIGsgKyBcIilcIikudGVzdChmb3JtYXQpKVxuICAgICAgICAgICAgZm9ybWF0ID0gZm9ybWF0LnJlcGxhY2UoUmVnRXhwLiQxLCAoUmVnRXhwLiQxLmxlbmd0aCA9PSAxKSA/IChvW2tdKSA6ICgoXCIwMFwiICsgb1trXSkuc3Vic3RyKChcIlwiICsgb1trXSkubGVuZ3RoKSkpO1xuICAgIHJldHVybiBmb3JtYXQ7XG59O1xuXG5leHBvcnQgZnVuY3Rpb24gYnVpbGRQYWdlU2l6ZUxpc3QoKSB7XG4gICAgcmV0dXJuIHtcbiAgICAgICAgQTA6IHtcbiAgICAgICAgICAgIHdpZHRoOiA4NDEsXG4gICAgICAgICAgICBoZWlnaHQ6IDExODlcbiAgICAgICAgfSxcbiAgICAgICAgQTE6IHtcbiAgICAgICAgICAgIHdpZHRoOiA1OTQsXG4gICAgICAgICAgICBoZWlnaHQ6IDg0MVxuICAgICAgICB9LFxuICAgICAgICBBMjoge1xuICAgICAgICAgICAgd2lkdGg6IDQyMCxcbiAgICAgICAgICAgIGhlaWdodDogNTk0XG4gICAgICAgIH0sXG4gICAgICAgIEEzOiB7XG4gICAgICAgICAgICB3aWR0aDogMjk3LFxuICAgICAgICAgICAgaGVpZ2h0OiA0MjBcbiAgICAgICAgfSxcbiAgICAgICAgQTQ6IHtcbiAgICAgICAgICAgIHdpZHRoOiAyMTAsXG4gICAgICAgICAgICBoZWlnaHQ6IDI5N1xuICAgICAgICB9LFxuICAgICAgICBBNToge1xuICAgICAgICAgICAgd2lkdGg6IDE0OCxcbiAgICAgICAgICAgIGhlaWdodDogMjEwXG4gICAgICAgIH0sXG4gICAgICAgIEE2OiB7XG4gICAgICAgICAgICB3aWR0aDogMTA1LFxuICAgICAgICAgICAgaGVpZ2h0OiAxNDhcbiAgICAgICAgfSxcbiAgICAgICAgQTc6IHtcbiAgICAgICAgICAgIHdpZHRoOiA3NCxcbiAgICAgICAgICAgIGhlaWdodDogMTA1XG4gICAgICAgIH0sXG4gICAgICAgIEE4OiB7XG4gICAgICAgICAgICB3aWR0aDogNTIsXG4gICAgICAgICAgICBoZWlnaHQ6IDc0XG4gICAgICAgIH0sXG4gICAgICAgIEE5OiB7XG4gICAgICAgICAgICB3aWR0aDogMzcsXG4gICAgICAgICAgICBoZWlnaHQ6IDUyXG4gICAgICAgIH0sXG4gICAgICAgIEExMDoge1xuICAgICAgICAgICAgd2lkdGg6IDI2LFxuICAgICAgICAgICAgaGVpZ2h0OiAzN1xuICAgICAgICB9LFxuICAgICAgICBCMDoge1xuICAgICAgICAgICAgd2lkdGg6IDEwMDAsXG4gICAgICAgICAgICBoZWlnaHQ6IDE0MTRcbiAgICAgICAgfSxcbiAgICAgICAgQjE6IHtcbiAgICAgICAgICAgIHdpZHRoOiA3MDcsXG4gICAgICAgICAgICBoZWlnaHQ6IDEwMDBcbiAgICAgICAgfSxcbiAgICAgICAgQjI6IHtcbiAgICAgICAgICAgIHdpZHRoOiA1MDAsXG4gICAgICAgICAgICBoZWlnaHQ6IDcwN1xuICAgICAgICB9LFxuICAgICAgICBCMzoge1xuICAgICAgICAgICAgd2lkdGg6IDM1MyxcbiAgICAgICAgICAgIGhlaWdodDogNTAwXG4gICAgICAgIH0sXG4gICAgICAgIEI0OiB7XG4gICAgICAgICAgICB3aWR0aDogMjUwLFxuICAgICAgICAgICAgaGVpZ2h0OiAzNTNcbiAgICAgICAgfSxcbiAgICAgICAgQjU6IHtcbiAgICAgICAgICAgIHdpZHRoOiAxNzYsXG4gICAgICAgICAgICBoZWlnaHQ6IDI1MFxuICAgICAgICB9LFxuICAgICAgICBCNjoge1xuICAgICAgICAgICAgd2lkdGg6IDEyNSxcbiAgICAgICAgICAgIGhlaWdodDogMTc2XG4gICAgICAgIH0sXG4gICAgICAgIEI3OiB7XG4gICAgICAgICAgICB3aWR0aDogODgsXG4gICAgICAgICAgICBoZWlnaHQ6IDEyNVxuICAgICAgICB9LFxuICAgICAgICBCODoge1xuICAgICAgICAgICAgd2lkdGg6IDYyLFxuICAgICAgICAgICAgaGVpZ2h0OiA4OFxuICAgICAgICB9LFxuICAgICAgICBCOToge1xuICAgICAgICAgICAgd2lkdGg6IDQ0LFxuICAgICAgICAgICAgaGVpZ2h0OiA2MlxuICAgICAgICB9LFxuICAgICAgICBCMTA6IHtcbiAgICAgICAgICAgIHdpZHRoOiAzMSxcbiAgICAgICAgICAgIGhlaWdodDogNDRcbiAgICAgICAgfVxuICAgIH1cbn1cblxuZXhwb3J0IGNvbnN0IHVuZG9NYW5hZ2VyID0gbmV3IFVuZG9NYW5hZ2VyKClcblxuZXhwb3J0IGNvbnN0IGdldFVybFBhcmFtID0gbmFtZSA9PiB7XG4gICAgbGV0IHJlZyA9IG5ldyBSZWdFeHAoXCIoXnwmKVwiICsgbmFtZSArIFwiPShbXiZdKikoJnwkKVwiKTtcbiAgICBsZXQgciA9IHdpbmRvdy5sb2NhdGlvbi5zZWFyY2guc3Vic3RyKDEpLm1hdGNoKHJlZyk7XG4gICAgaWYgKHIgIT0gbnVsbCkge1xuICAgICAgICByZXR1cm4gdW5lc2NhcGUoclsyXSk7XG4gICAgfSBlbHNlIHtcbiAgICAgICAgcmV0dXJuIG51bGw7XG4gICAgfVxufSIsIi8qKlxuICogQ3JlYXRlZCBieSBKYWNreS5HYW8gb24gMjAxNy0wMi0wNy5cbiAqL1xuaW1wb3J0IHtcbiAgICBtbVRvUG9pbnQsXG4gICAgcG9pbnRUb01NLFxuICAgIGJ1aWxkUGFnZVNpemVMaXN0LFxuICAgIGdldFBhcmFtZXRlcixcbiAgICBzaG93TG9hZGluZyxcbiAgICBoaWRlTG9hZGluZyxcbiAgICBnZXRVcmxQYXJhbVxufSBmcm9tICcuLi9VdGlscy5qcyc7XG5pbXBvcnQge1xuICAgIGFsZXJ0XG59IGZyb20gJy4uL01zZ0JveC5qcyc7XG5cbmV4cG9ydCBkZWZhdWx0IGNsYXNzIFBERlByaW50RGlhbG9nIHtcbiAgICBjb25zdHJ1Y3RvcigpIHtcbiAgICAgICAgY29uc3QgdyA9ICQod2luZG93KS53aWR0aCgpLFxuICAgICAgICAgICAgaCA9ICQod2luZG93KS5oZWlnaHQoKSAtIDI4MDtcbiAgICAgICAgdGhpcy5wYXBlclNpemVMaXN0ID0gYnVpbGRQYWdlU2l6ZUxpc3QoKTtcbiAgICAgICAgdGhpcy5kaWFsb2cgPSAkKGA8ZGl2IGNsYXNzPVwibW9kYWwgZmFkZSBkYXRhLXJlcG9ydC1tb2RhbFwiIHJvbGU9XCJkaWFsb2dcIiBhcmlhLWhpZGRlbj1cInRydWVcIiBzdHlsZT1cInotaW5kZXg6IDEwMDAwXCI+XG4gICAgICAgICAgICA8ZGl2IGNsYXNzPVwibW9kYWwtZGlhbG9nIG1vZGFsLWxnXCIgc3R5bGU9XCJ3aWR0aDogMTAzMHB4O1wiPlxuICAgICAgICAgICAgICAgIDxkaXYgY2xhc3M9XCJtb2RhbC1jb250ZW50XCI+XG4gICAgICAgICAgICAgICAgICAgIDxkaXYgY2xhc3M9XCJtb2RhbC1oZWFkZXJcIj5cbiAgICAgICAgICAgICAgICAgICAgICAgIDxidXR0b24gdHlwZT1cImJ1dHRvblwiIGNsYXNzPVwiY2xvc2UgcGRmY2xvc2VcIiBkYXRhLWRpc21pc3M9XCJtb2RhbFwiIGFyaWEtaGlkZGVuPVwidHJ1ZVwiPlxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIFhcbiAgICAgICAgICAgICAgICAgICAgICAgIDwvYnV0dG9uPlxuICAgICAgICAgICAgICAgICAgICAgICAgPGg0IGNsYXNzPVwibW9kYWwtdGl0bGVcIj5cbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAke3dpbmRvdy5pMThuLnBkZlByaW50LnRpdGxlfVxuICAgICAgICAgICAgICAgICAgICAgICAgPC9oND5cbiAgICAgICAgICAgICAgICAgICAgPC9kaXY+XG4gICAgICAgICAgICAgICAgICAgIDxkaXYgY2xhc3M9XCJtb2RhbC1ib2R5XCIgc3R5bGU9XCJwYWRkaW5nLXRvcDo1cHg7XCI+PC9kaXY+XG4gICAgICAgICAgICAgICAgPC9kaXY+XG4gICAgICAgICAgICA8L2Rpdj5cbiAgICAgICAgPC9kaXY+YCk7XG4gICAgICAgIHRoaXMuYm9keSA9IHRoaXMuZGlhbG9nLmZpbmQoJy5tb2RhbC1ib2R5Jyk7XG4gICAgICAgIHRoaXMuaW5pdEJvZHkoKTtcbiAgICB9XG4gICAgaW5pdEJvZHkoKSB7XG4gICAgICAgIGNvbnN0IHRva2VuID0gZ2V0VXJsUGFyYW0oJ3Rva2VuJylcbiAgICAgICAgY29uc3QgdG9vbGJhciA9ICQoYDxmaWVsZHNldCBzdHlsZT1cIndpZHRoOiAxMDAlO2hlaWdodDogNjBweDtmb250LXNpemU6IDEycHg7Ym9yZGVyOiBzb2xpZCAxcHggI2RkZDtib3JkZXItcmFkaXVzOiA1cHg7cGFkZGluZzogMXB4IDhweDtcIj5cbiAgICAgICAgPGxlZ2VuZCBzdHlsZT1cImZvbnQtc2l6ZTogMTJweDt3aWR0aDogNjBweDtib3JkZXItYm90dG9tOiBub25lO21hcmdpbi1ib3R0b206IDA7XCI+JHt3aW5kb3cuaTE4bi5wZGZQcmludC5zZXR1cH08L2xlZ2VuZD5cbiAgICAgICAgPC9maWVsZHNldD5gKTtcbiAgICAgICAgdGhpcy5ib2R5LmFwcGVuZCh0b29sYmFyKTtcbiAgICAgICAgY29uc3QgcGFnZVR5cGVHcm91cCA9ICQoYDxkaXYgY2xhc3M9XCJmb3JtLWdyb3VwXCIgc3R5bGU9XCJkaXNwbGF5OiBpbmxpbmUtYmxvY2tcIj48bGFiZWw+JHt3aW5kb3cuaTE4bi5wZGZQcmludC5wYXBlcn08L2xhYmVsPjwvZGl2PmApO1xuICAgICAgICB0b29sYmFyLmFwcGVuZChwYWdlVHlwZUdyb3VwKTtcbiAgICAgICAgdGhpcy5wYWdlU2VsZWN0ID0gJChgPHNlbGVjdCBjbGFzcz1cImZvcm0tY29udHJvbFwiIHN0eWxlPVwiZGlzcGxheTogaW5saW5lLWJsb2NrO3dpZHRoOiA2OHB4O2ZvbnQtc2l6ZTogMTJweDtwYWRkaW5nOiAxcHg7aGVpZ2h0OiAyOHB4O1wiPlxuICAgICAgICAgICAgPG9wdGlvbj5BMDwvb3B0aW9uPlxuICAgICAgICAgICAgPG9wdGlvbj5BMTwvb3B0aW9uPlxuICAgICAgICAgICAgPG9wdGlvbj5BMjwvb3B0aW9uPlxuICAgICAgICAgICAgPG9wdGlvbj5BMzwvb3B0aW9uPlxuICAgICAgICAgICAgPG9wdGlvbj5BNDwvb3B0aW9uPlxuICAgICAgICAgICAgPG9wdGlvbj5BNTwvb3B0aW9uPlxuICAgICAgICAgICAgPG9wdGlvbj5BNjwvb3B0aW9uPlxuICAgICAgICAgICAgPG9wdGlvbj5BNzwvb3B0aW9uPlxuICAgICAgICAgICAgPG9wdGlvbj5BODwvb3B0aW9uPlxuICAgICAgICAgICAgPG9wdGlvbj5BOTwvb3B0aW9uPlxuICAgICAgICAgICAgPG9wdGlvbj5BMTA8L29wdGlvbj5cbiAgICAgICAgICAgIDxvcHRpb24+QjA8L29wdGlvbj5cbiAgICAgICAgICAgIDxvcHRpb24+QjE8L29wdGlvbj5cbiAgICAgICAgICAgIDxvcHRpb24+QjI8L29wdGlvbj5cbiAgICAgICAgICAgIDxvcHRpb24+QjM8L29wdGlvbj5cbiAgICAgICAgICAgIDxvcHRpb24+QjQ8L29wdGlvbj5cbiAgICAgICAgICAgIDxvcHRpb24+QjU8L29wdGlvbj5cbiAgICAgICAgICAgIDxvcHRpb24+QjY8L29wdGlvbj5cbiAgICAgICAgICAgIDxvcHRpb24+Qjc8L29wdGlvbj5cbiAgICAgICAgICAgIDxvcHRpb24+Qjg8L29wdGlvbj5cbiAgICAgICAgICAgIDxvcHRpb24+Qjk8L29wdGlvbj5cbiAgICAgICAgICAgIDxvcHRpb24+QjEwPC9vcHRpb24+XG4gICAgICAgICAgICA8b3B0aW9uIHZhbHVlPVwiQ1VTVE9NXCI+JHt3aW5kb3cuaTE4bi5wZGZQcmludC5jdXN0b219PC9vcHRpb24+XG4gICAgICAgIDwvc2VsZWN0PmApO1xuICAgICAgICBwYWdlVHlwZUdyb3VwLmFwcGVuZCh0aGlzLnBhZ2VTZWxlY3QpO1xuICAgICAgICBjb25zdCBfdGhpcyA9IHRoaXM7XG4gICAgICAgIHRoaXMucGFnZVNlbGVjdC5jaGFuZ2UoZnVuY3Rpb24gKCkge1xuICAgICAgICAgICAgbGV0IHZhbHVlID0gJCh0aGlzKS52YWwoKTtcbiAgICAgICAgICAgIGlmICh2YWx1ZSA9PT0gJ0NVU1RPTScpIHtcbiAgICAgICAgICAgICAgICBfdGhpcy5wYWdlV2lkdGhFZGl0b3IucHJvcCgncmVhZG9ubHknLCBmYWxzZSk7XG4gICAgICAgICAgICAgICAgX3RoaXMucGFnZUhlaWdodEVkaXRvci5wcm9wKCdyZWFkb25seScsIGZhbHNlKTtcbiAgICAgICAgICAgIH0gZWxzZSB7XG4gICAgICAgICAgICAgICAgX3RoaXMucGFnZVdpZHRoRWRpdG9yLnByb3AoJ3JlYWRvbmx5JywgdHJ1ZSk7XG4gICAgICAgICAgICAgICAgX3RoaXMucGFnZUhlaWdodEVkaXRvci5wcm9wKCdyZWFkb25seScsIHRydWUpO1xuICAgICAgICAgICAgICAgIGxldCBwYWdlU2l6ZSA9IF90aGlzLnBhcGVyU2l6ZUxpc3RbdmFsdWVdO1xuICAgICAgICAgICAgICAgIGNvbnNvbGUubG9nKHBhZ2VTaXplKVxuICAgICAgICAgICAgICAgIF90aGlzLnBhZ2VXaWR0aEVkaXRvci52YWwocGFnZVNpemUud2lkdGgpO1xuICAgICAgICAgICAgICAgIF90aGlzLnBhZ2VIZWlnaHRFZGl0b3IudmFsKHBhZ2VTaXplLmhlaWdodCk7XG4gICAgICAgICAgICAgICAgX3RoaXMucGFwZXIud2lkdGggPSBtbVRvUG9pbnQocGFnZVNpemUud2lkdGgpO1xuICAgICAgICAgICAgICAgIF90aGlzLnBhcGVyLmhlaWdodCA9IG1tVG9Qb2ludChwYWdlU2l6ZS5oZWlnaHQpO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgX3RoaXMucGFwZXIucGFwZXJUeXBlID0gdmFsdWU7XG4gICAgICAgIH0pO1xuXG4gICAgICAgIGNvbnN0IHBhZ2VXaWR0aEdyb3VwID0gJChgPGRpdiBjbGFzcz1cImZvcm0tZ3JvdXBcIiBzdHlsZT1cImRpc3BsYXk6IGlubGluZS1ibG9jazttYXJnaW4tbGVmdDogNnB4XCI+PHNwYW4+JHt3aW5kb3cuaTE4bi5wZGZQcmludC53aWR0aH08L3NwYW4+PC9kaXY+YCk7XG4gICAgICAgIHRvb2xiYXIuYXBwZW5kKHBhZ2VXaWR0aEdyb3VwKTtcbiAgICAgICAgdGhpcy5wYWdlV2lkdGhFZGl0b3IgPSAkKGA8aW5wdXQgdHlwZT1cIm51bWJlclwiIGNsYXNzPVwiZm9ybS1jb250cm9sXCIgcmVhZG9ubHkgc3R5bGU9XCJkaXNwbGF5OiBpbmxpbmUtYmxvY2s7d2lkdGg6IDQwcHg7Zm9udC1zaXplOiAxMnB4O3BhZGRpbmc6IDFweDtoZWlnaHQ6IDI4cHhcIj5gKTtcbiAgICAgICAgcGFnZVdpZHRoR3JvdXAuYXBwZW5kKHRoaXMucGFnZVdpZHRoRWRpdG9yKTtcbiAgICAgICAgdGhpcy5wYWdlV2lkdGhFZGl0b3IuY2hhbmdlKGZ1bmN0aW9uICgpIHtcbiAgICAgICAgICAgIGxldCB2YWx1ZSA9ICQodGhpcykudmFsKCk7XG4gICAgICAgICAgICBpZiAoIXZhbHVlIHx8IGlzTmFOKHZhbHVlKSkge1xuICAgICAgICAgICAgICAgIGFsZXJ0KGAke3dpbmRvdy5pMThuLnBkZlByaW50Lm51bWJlclRpcH1gKTtcbiAgICAgICAgICAgICAgICByZXR1cm47XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBfdGhpcy5wYXBlci53aWR0aCA9IG1tVG9Qb2ludCh2YWx1ZSk7XG4gICAgICAgICAgICBfdGhpcy5jb250ZXh0LnByaW50TGluZS5yZWZyZXNoKCk7XG4gICAgICAgIH0pO1xuXG4gICAgICAgIGNvbnN0IHBhZ2VIZWlnaHRHcm91cCA9ICQoYDxkaXYgY2xhc3M9XCJmb3JtLWdyb3VwXCIgc3R5bGU9XCJkaXNwbGF5OiBpbmxpbmUtYmxvY2s7bWFyZ2luLWxlZnQ6IDZweFwiPjxzcGFuPiR7d2luZG93LmkxOG4ucGRmUHJpbnQuaGVpZ2h0fTwvc3Bhbj48L2Rpdj5gKTtcbiAgICAgICAgdG9vbGJhci5hcHBlbmQocGFnZUhlaWdodEdyb3VwKTtcbiAgICAgICAgdGhpcy5wYWdlSGVpZ2h0RWRpdG9yID0gJChgPGlucHV0IHR5cGU9XCJudW1iZXJcIiBjbGFzcz1cImZvcm0tY29udHJvbFwiIHJlYWRvbmx5IHN0eWxlPVwiZGlzcGxheTogaW5saW5lLWJsb2NrO3dpZHRoOiA0MHB4O2ZvbnQtc2l6ZTogMTJweDtwYWRkaW5nOiAxcHg7aGVpZ2h0OiAyOHB4XCI+YCk7XG4gICAgICAgIHBhZ2VIZWlnaHRHcm91cC5hcHBlbmQodGhpcy5wYWdlSGVpZ2h0RWRpdG9yKTtcbiAgICAgICAgdGhpcy5wYWdlSGVpZ2h0RWRpdG9yLmNoYW5nZShmdW5jdGlvbiAoKSB7XG4gICAgICAgICAgICBsZXQgdmFsdWUgPSAkKHRoaXMpLnZhbCgpO1xuICAgICAgICAgICAgaWYgKCF2YWx1ZSB8fCBpc05hTih2YWx1ZSkpIHtcbiAgICAgICAgICAgICAgICBhbGVydChgJHt3aW5kb3cuaTE4bi5wZGZQcmludC5udW1iZXJUaXB9YCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgX3RoaXMucGFwZXIuaGVpZ2h0ID0gbW1Ub1BvaW50KHZhbHVlKTtcbiAgICAgICAgfSk7XG5cbiAgICAgICAgY29uc3Qgb3JpZW50YXRpb25Hcm91cCA9ICQoYDxkaXYgY2xhc3M9XCJmb3JtLWdyb3VwXCIgc3R5bGU9XCJkaXNwbGF5OiBpbmxpbmUtYmxvY2s7bWFyZ2luLWxlZnQ6IDZweFwiPjxsYWJlbD4ke3dpbmRvdy5pMThuLnBkZlByaW50Lm9yaWVudGF0aW9ufTwvbGFiZWw+PC9kaXY+YCk7XG4gICAgICAgIHRvb2xiYXIuYXBwZW5kKG9yaWVudGF0aW9uR3JvdXApO1xuICAgICAgICB0aGlzLm9yaWVudGF0aW9uU2VsZWN0ID0gJChgPHNlbGVjdCBjbGFzcz1cImZvcm0tY29udHJvbFwiIHN0eWxlPVwiZGlzcGxheTppbmxpbmUtYmxvY2s7d2lkdGg6IDYwcHg7Zm9udC1zaXplOiAxMnB4O3BhZGRpbmc6IDFweDtoZWlnaHQ6IDI4cHhcIj5cbiAgICAgICAgICAgIDxvcHRpb24gdmFsdWU9XCJwb3J0cmFpdFwiPiR7d2luZG93LmkxOG4ucGRmUHJpbnQucG9ydHJhaXR9PC9vcHRpb24+XG4gICAgICAgICAgICA8b3B0aW9uIHZhbHVlPVwibGFuZHNjYXBlXCI+JHt3aW5kb3cuaTE4bi5wZGZQcmludC5sYW5kc2NhcGV9PC9vcHRpb24+XG4gICAgICAgIDwvc2VsZWN0PmApO1xuICAgICAgICBvcmllbnRhdGlvbkdyb3VwLmFwcGVuZCh0aGlzLm9yaWVudGF0aW9uU2VsZWN0KTtcbiAgICAgICAgdGhpcy5vcmllbnRhdGlvblNlbGVjdC5jaGFuZ2UoZnVuY3Rpb24gKCkge1xuICAgICAgICAgICAgbGV0IHZhbHVlID0gJCh0aGlzKS52YWwoKTtcbiAgICAgICAgICAgIF90aGlzLnBhcGVyLm9yaWVudGF0aW9uID0gdmFsdWU7XG4gICAgICAgIH0pO1xuXG4gICAgICAgIGNvbnN0IG1hcmdpbkdyb3VwID0gJChgPGRpdiBzdHlsZT1cImRpc3BsYXk6IGlubGluZS1ibG9ja1wiPjwvZGl2PmApO1xuICAgICAgICB0b29sYmFyLmFwcGVuZChtYXJnaW5Hcm91cCk7XG5cbiAgICAgICAgY29uc3QgbGVmdE1hcmdpbkdyb3VwID0gJChgPGRpdiBjbGFzcz1cImZvcm0tZ3JvdXBcIiBzdHlsZT1cImRpc3BsYXk6IGlubGluZS1ibG9jazttYXJnaW4tbGVmdDo2cHhcIj48bGFiZWw+JHt3aW5kb3cuaTE4bi5wZGZQcmludC5sZWZ0TWFyZ2lufTwvbGFiZWw+PC9kaXY+YCk7XG4gICAgICAgIG1hcmdpbkdyb3VwLmFwcGVuZChsZWZ0TWFyZ2luR3JvdXApO1xuICAgICAgICB0aGlzLmxlZnRNYXJnaW5FZGl0b3IgPSAkKGA8aW5wdXQgdHlwZT1cIm51bWJlclwiIGNsYXNzPVwiZm9ybS1jb250cm9sXCIgc3R5bGU9XCJkaXNwbGF5OiBpbmxpbmUtYmxvY2s7d2lkdGg6IDQwcHg7Zm9udC1zaXplOiAxMnB4O3BhZGRpbmc6IDFweDtoZWlnaHQ6IDI4cHhcIj5gKTtcbiAgICAgICAgbGVmdE1hcmdpbkdyb3VwLmFwcGVuZCh0aGlzLmxlZnRNYXJnaW5FZGl0b3IpO1xuICAgICAgICB0aGlzLmxlZnRNYXJnaW5FZGl0b3IuY2hhbmdlKGZ1bmN0aW9uICgpIHtcbiAgICAgICAgICAgIGxldCB2YWx1ZSA9ICQodGhpcykudmFsKCk7XG4gICAgICAgICAgICBpZiAoIXZhbHVlIHx8IGlzTmFOKHZhbHVlKSkge1xuICAgICAgICAgICAgICAgIGFsZXJ0KGAke3dpbmRvdy5pMThuLnBkZlByaW50Lm51bWJlclRpcH1gKTtcbiAgICAgICAgICAgICAgICByZXR1cm47XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBfdGhpcy5wYXBlci5sZWZ0TWFyZ2luID0gbW1Ub1BvaW50KHZhbHVlKTtcbiAgICAgICAgICAgIF90aGlzLmNvbnRleHQucHJpbnRMaW5lLnJlZnJlc2goKTtcbiAgICAgICAgfSk7XG5cbiAgICAgICAgY29uc3QgcmlnaHRNYXJnaW5Hcm91cCA9ICQoYDxkaXYgY2xhc3M9XCJmb3JtLWdyb3VwXCIgc3R5bGU9XCJkaXNwbGF5OiBpbmxpbmUtYmxvY2s7bWFyZ2luLXRvcDogNXB4O21hcmdpbi1sZWZ0OiA2cHhcIlwiPjxsYWJlbD4ke3dpbmRvdy5pMThuLnBkZlByaW50LnJpZ2h0TWFyZ2lufTwvbGFiZWw+PC9kaXY+YCk7XG4gICAgICAgIG1hcmdpbkdyb3VwLmFwcGVuZChyaWdodE1hcmdpbkdyb3VwKTtcbiAgICAgICAgdGhpcy5yaWdodE1hcmdpbkVkaXRvciA9ICQoYDxpbnB1dCB0eXBlPVwibnVtYmVyXCIgY2xhc3M9XCJmb3JtLWNvbnRyb2xcIiBzdHlsZT1cImRpc3BsYXk6IGlubGluZS1ibG9jazt3aWR0aDogNDBweDtmb250LXNpemU6IDEycHg7cGFkZGluZzogMXB4O2hlaWdodDogMjhweFwiPmApO1xuICAgICAgICByaWdodE1hcmdpbkdyb3VwLmFwcGVuZCh0aGlzLnJpZ2h0TWFyZ2luRWRpdG9yKTtcbiAgICAgICAgdGhpcy5yaWdodE1hcmdpbkVkaXRvci5jaGFuZ2UoZnVuY3Rpb24gKCkge1xuICAgICAgICAgICAgbGV0IHZhbHVlID0gJCh0aGlzKS52YWwoKTtcbiAgICAgICAgICAgIGlmICghdmFsdWUgfHwgaXNOYU4odmFsdWUpKSB7XG4gICAgICAgICAgICAgICAgYWxlcnQoYCR7d2luZG93LmkxOG4ucGRmUHJpbnQubnVtYmVyVGlwfWApO1xuICAgICAgICAgICAgICAgIHJldHVybjtcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIF90aGlzLnBhcGVyLnJpZ2h0TWFyZ2luID0gbW1Ub1BvaW50KHZhbHVlKTtcbiAgICAgICAgICAgIF90aGlzLmNvbnRleHQucHJpbnRMaW5lLnJlZnJlc2goKTtcbiAgICAgICAgfSk7XG5cbiAgICAgICAgY29uc3QgdG9wTWFyZ2luR3JvdXAgPSAkKGA8ZGl2IGNsYXNzPVwiZm9ybS1ncm91cFwiIHN0eWxlPVwiZGlzcGxheTogaW5saW5lLWJsb2NrO21hcmdpbi1sZWZ0OiA2cHg7XCI+PGxhYmVsPiR7d2luZG93LmkxOG4ucGRmUHJpbnQudG9wTWFyZ2lufTwvbGFiZWw+PC9kaXY+YCk7XG4gICAgICAgIG1hcmdpbkdyb3VwLmFwcGVuZCh0b3BNYXJnaW5Hcm91cCk7XG4gICAgICAgIHRoaXMudG9wTWFyZ2luRWRpdG9yID0gJChgPGlucHV0IHR5cGU9XCJudW1iZXJcIiBjbGFzcz1cImZvcm0tY29udHJvbFwiIHN0eWxlPVwiZGlzcGxheTogaW5saW5lLWJsb2NrO3dpZHRoOiA0MHB4O2ZvbnQtc2l6ZTogMTJweDtwYWRkaW5nOiAxcHg7aGVpZ2h0OiAyOHB4XCI+YCk7XG4gICAgICAgIHRvcE1hcmdpbkdyb3VwLmFwcGVuZCh0aGlzLnRvcE1hcmdpbkVkaXRvcik7XG4gICAgICAgIHRoaXMudG9wTWFyZ2luRWRpdG9yLmNoYW5nZShmdW5jdGlvbiAoKSB7XG4gICAgICAgICAgICBsZXQgdmFsdWUgPSAkKHRoaXMpLnZhbCgpO1xuICAgICAgICAgICAgaWYgKCF2YWx1ZSB8fCBpc05hTih2YWx1ZSkpIHtcbiAgICAgICAgICAgICAgICBhbGVydChgJHt3aW5kb3cuaTE4bi5wZGZQcmludC5udW1iZXJUaXB9YCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgX3RoaXMucGFwZXIudG9wTWFyZ2luID0gbW1Ub1BvaW50KHZhbHVlKTtcbiAgICAgICAgfSk7XG5cbiAgICAgICAgY29uc3QgYm90dG9tTWFyZ2luR3JvdXAgPSAkKGA8ZGl2IGNsYXNzPVwiZm9ybS1ncm91cFwiIHN0eWxlPVwiZGlzcGxheTogaW5saW5lLWJsb2NrO21hcmdpbi1sZWZ0OiA2cHhcIlwiPjxsYWJlbD4ke3dpbmRvdy5pMThuLnBkZlByaW50LmJvdHRvbU1hcmdpbn08L2xhYmVsPjwvZGl2PmApO1xuICAgICAgICBtYXJnaW5Hcm91cC5hcHBlbmQoYm90dG9tTWFyZ2luR3JvdXApO1xuICAgICAgICB0aGlzLmJvdHRvbU1hcmdpbkVkaXRvciA9ICQoYDxpbnB1dCB0eXBlPVwibnVtYmVyXCIgY2xhc3M9XCJmb3JtLWNvbnRyb2xcIiBzdHlsZT1cImRpc3BsYXk6IGlubGluZS1ibG9jazt3aWR0aDogNDBweDtmb250LXNpemU6IDEycHg7cGFkZGluZzogMXB4O2hlaWdodDogMjhweFwiPmApO1xuICAgICAgICBib3R0b21NYXJnaW5Hcm91cC5hcHBlbmQodGhpcy5ib3R0b21NYXJnaW5FZGl0b3IpO1xuICAgICAgICB0aGlzLmJvdHRvbU1hcmdpbkVkaXRvci5jaGFuZ2UoZnVuY3Rpb24gKCkge1xuICAgICAgICAgICAgbGV0IHZhbHVlID0gJCh0aGlzKS52YWwoKTtcbiAgICAgICAgICAgIGlmICghdmFsdWUgfHwgaXNOYU4odmFsdWUpKSB7XG4gICAgICAgICAgICAgICAgYWxlcnQoYCR7d2luZG93LmkxOG4ucGRmUHJpbnQubnVtYmVyVGlwfWApO1xuICAgICAgICAgICAgICAgIHJldHVybjtcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIF90aGlzLnBhcGVyLmJvdHRvbU1hcmdpbiA9IG1tVG9Qb2ludCh2YWx1ZSk7XG4gICAgICAgIH0pO1xuICAgICAgICBjb25zdCBmaWxlID0gZ2V0UGFyYW1ldGVyKCdfdScpO1xuICAgICAgICBjb25zdCB1cmxQYXJhbWV0ZXJzID0gd2luZG93LmxvY2F0aW9uLnNlYXJjaDtcbiAgICAgICAgY29uc3QgYnV0dG9uID0gJChgPGJ1dHRvbiBjbGFzcz1cImJ0biBidG4tcHJpbWFyeVwiIHN0eWxlPVwicGFkZGluZy10b3A6NXB4O2hlaWdodDogMzBweDttYXJnaW4tbGVmdDogMTBweDtcIj4ke3dpbmRvdy5pMThuLnBkZlByaW50LmFwcGx5fTwvYnV0dG9uPmApO1xuICAgICAgICB0b29sYmFyLmFwcGVuZChidXR0b24pO1xuICAgICAgICBsZXQgaW5kZXggPSAwO1xuICAgICAgICBidXR0b24ub24oJ2NsaWNrJywgKCkgPT4ge1xuICAgICAgICAgICAgc2hvd0xvYWRpbmcoKTtcbiAgICAgICAgICAgIGNvbnN0IHBhcGVyRGF0YSA9IEpTT04uc3RyaW5naWZ5KF90aGlzLnBhcGVyKTtcbiAgICAgICAgICAgICQuYWpheCh7XG4gICAgICAgICAgICAgICAgdXJsOiB3aW5kb3cuX3NlcnZlciArICcvcGRmL25ld1BhZ2luZycgKyB1cmxQYXJhbWV0ZXJzLFxuICAgICAgICAgICAgICAgIHR5cGU6ICdQT1NUJyxcbiAgICAgICAgICAgICAgICBkYXRhOiB7XG4gICAgICAgICAgICAgICAgICAgIF9wYXBlcjogcGFwZXJEYXRhXG4gICAgICAgICAgICAgICAgfSxcbiAgICAgICAgICAgICAgICBoZWFkZXJzOiB7XG4gICAgICAgICAgICAgICAgICAgICdBdXRob3JpemF0aW9uJzogdG9rZW5cbiAgICAgICAgICAgICAgICB9LFxuICAgICAgICAgICAgICAgIHN1Y2Nlc3M6IGZ1bmN0aW9uICgpIHtcbiAgICAgICAgICAgICAgICAgICAgY29uc3QgbmV3VXJsID0gd2luZG93Ll9zZXJ2ZXIgKyAnL3BkZi9zaG93JyArIHVybFBhcmFtZXRlcnMgKyAnJl9yPScgKyAoaW5kZXgrKyk7XG4gICAgICAgICAgICAgICAgICAgIF90aGlzLmlGcmFtZS5wcm9wKCdzcmMnLCBuZXdVcmwpO1xuICAgICAgICAgICAgICAgIH0sXG4gICAgICAgICAgICAgICAgZXJyb3I6IGZ1bmN0aW9uICgpIHtcbiAgICAgICAgICAgICAgICAgICAgaGlkZUxvYWRpbmcoKTtcbiAgICAgICAgICAgICAgICAgICAgYWxlcnQoYCR7d2luZG93LmkxOG4ucGRmUHJpbnQuZmFpbH1gKTtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICB9KVxuICAgICAgICB9KTtcblxuICAgICAgICBjb25zdCBwcmludEJ1dHRvbiA9ICQoYDxidXR0b24gY2xhc3M9XCJidG4gYnRuLWRhbmdlclwiIHN0eWxlPVwicGFkZGluZy10b3A6NXB4O2hlaWdodDogMzBweDttYXJnaW4tbGVmdDogMTBweDtcIj4ke3dpbmRvdy5pMThuLnBkZlByaW50LnByaW50fTwvYnV0dG9uPmApO1xuICAgICAgICB0b29sYmFyLmFwcGVuZChwcmludEJ1dHRvbik7XG4gICAgICAgIHByaW50QnV0dG9uLm9uKCdjbGljaycsICgpID0+IHtcbiAgICAgICAgICAgIHdpbmRvdy5mcmFtZXNbJ19pZnJhbWVfZm9yX3BkZl9wcmludCddLndpbmRvdy5wcmludCgpO1xuICAgICAgICB9KTtcbiAgICB9XG5cbiAgICBpbml0SUZyYW1lKCkge1xuICAgICAgICBpZiAodGhpcy5pRnJhbWUpIHtcbiAgICAgICAgICAgIHJldHVybjtcbiAgICAgICAgfVxuICAgICAgICBjb25zdCB1cmxQYXJhbWV0ZXJzID0gYnVpbGRMb2NhdGlvblNlYXJjaFBhcmFtZXRlcnMoKTtcbiAgICAgICAgY29uc3QgaCA9ICQod2luZG93KS5oZWlnaHQoKTtcbiAgICAgICAgY29uc3QgdXJsID0gd2luZG93Ll9zZXJ2ZXIgKyBcIi9wZGYvc2hvd1wiICsgdXJsUGFyYW1ldGVycyArIFwiJl9wPTFcIjtcbiAgICAgICAgdGhpcy5pRnJhbWUgPSAkKGA8aWZyYW1lIG5hbWU9XCJfaWZyYW1lX2Zvcl9wZGZfcHJpbnRcIiBzdHlsZT1cIndpZHRoOiAxMDAlO2hlaWdodDo2MDBweDttYXJnaW4tdG9wOiA1cHg7Ym9yZGVyOnNvbGlkIDFweCAjYzJjMmMyXCIgZnJhbWVib3JkZXI9XCIwXCIgc3JjPVwiJHt1cmx9XCI+PC9pZnJhbWU+YCk7XG4gICAgICAgIHRoaXMuYm9keS5hcHBlbmQodGhpcy5pRnJhbWUpO1xuICAgICAgICBjb25zdCBpZnJhbWUgPSB0aGlzLmlGcmFtZS5nZXQoMCk7XG4gICAgICAgIGNvbnN0IG1zaWUgPSB3aW5kb3cubmF2aWdhdG9yLmFwcE5hbWUuaW5kZXhPZihcIkludGVybmV0IEV4cGxvcmVyXCIpO1xuICAgICAgICBjb25zdCBpZTExID0gISF3aW5kb3cuTVNJbnB1dE1ldGhvZENvbnRleHQgJiYgISFkb2N1bWVudC5kb2N1bWVudE1vZGU7XG4gICAgICAgIGlmIChtc2llID09PSAtMSAmJiAhaWUxMSkge1xuICAgICAgICAgICAgc2hvd0xvYWRpbmcoKTtcbiAgICAgICAgfVxuICAgICAgICB0aGlzLmlGcmFtZS5vbignbG9hZCcsIGZ1bmN0aW9uICgpIHtcbiAgICAgICAgICAgIGhpZGVMb2FkaW5nKCk7XG4gICAgICAgIH0pO1xuICAgIH1cblxuICAgIHNob3cocGFwZXIpIHtcbiAgICAgICAgY29uc29sZS5sb2cocGFwZXIpXG4gICAgICAgIHRoaXMucGFwZXIgPSBwYXBlcjtcbiAgICAgICAgdGhpcy5wYWdlU2VsZWN0LnZhbCh0aGlzLnBhcGVyLnBhcGVyVHlwZSk7XG4gICAgICAgIHRoaXMucGFnZVdpZHRoRWRpdG9yLnZhbChwb2ludFRvTU0odGhpcy5wYXBlci53aWR0aCkpO1xuICAgICAgICB0aGlzLnBhZ2VIZWlnaHRFZGl0b3IudmFsKHBvaW50VG9NTSh0aGlzLnBhcGVyLmhlaWdodCkpO1xuICAgICAgICB0aGlzLnBhZ2VTZWxlY3QudHJpZ2dlcignY2hhbmdlJyk7XG4gICAgICAgIHRoaXMubGVmdE1hcmdpbkVkaXRvci52YWwocG9pbnRUb01NKHRoaXMucGFwZXIubGVmdE1hcmdpbikpO1xuICAgICAgICB0aGlzLnJpZ2h0TWFyZ2luRWRpdG9yLnZhbChwb2ludFRvTU0odGhpcy5wYXBlci5yaWdodE1hcmdpbikpO1xuICAgICAgICB0aGlzLnRvcE1hcmdpbkVkaXRvci52YWwocG9pbnRUb01NKHRoaXMucGFwZXIudG9wTWFyZ2luKSk7XG4gICAgICAgIHRoaXMuYm90dG9tTWFyZ2luRWRpdG9yLnZhbChwb2ludFRvTU0odGhpcy5wYXBlci5ib3R0b21NYXJnaW4pKTtcbiAgICAgICAgdGhpcy5vcmllbnRhdGlvblNlbGVjdC52YWwodGhpcy5wYXBlci5vcmllbnRhdGlvbik7XG4gICAgICAgIHRoaXMuZGlhbG9nLm1vZGFsKCdzaG93Jyk7XG4gICAgICAgIHRoaXMuaW5pdElGcmFtZSgpO1xuICAgIH1cbn07IiwiLy8gc3R5bGUtbG9hZGVyOiBBZGRzIHNvbWUgY3NzIHRvIHRoZSBET00gYnkgYWRkaW5nIGEgPHN0eWxlPiB0YWdcblxuLy8gbG9hZCB0aGUgc3R5bGVzXG52YXIgY29udGVudCA9IHJlcXVpcmUoXCIhIS4uLy4uLy4uL25vZGVfbW9kdWxlcy9jc3MtbG9hZGVyL2luZGV4LmpzIS4vYm9vdHN0cmFwLWRhdGV0aW1lcGlja2VyLmNzc1wiKTtcbmlmKHR5cGVvZiBjb250ZW50ID09PSAnc3RyaW5nJykgY29udGVudCA9IFtbbW9kdWxlLmlkLCBjb250ZW50LCAnJ11dO1xuLy8gYWRkIHRoZSBzdHlsZXMgdG8gdGhlIERPTVxudmFyIHVwZGF0ZSA9IHJlcXVpcmUoXCIhLi4vLi4vLi4vbm9kZV9tb2R1bGVzL3N0eWxlLWxvYWRlci9hZGRTdHlsZXMuanNcIikoY29udGVudCwge30pO1xuaWYoY29udGVudC5sb2NhbHMpIG1vZHVsZS5leHBvcnRzID0gY29udGVudC5sb2NhbHM7XG4vLyBIb3QgTW9kdWxlIFJlcGxhY2VtZW50XG5pZihtb2R1bGUuaG90KSB7XG5cdC8vIFdoZW4gdGhlIHN0eWxlcyBjaGFuZ2UsIHVwZGF0ZSB0aGUgPHN0eWxlPiB0YWdzXG5cdGlmKCFjb250ZW50LmxvY2Fscykge1xuXHRcdG1vZHVsZS5ob3QuYWNjZXB0KFwiISEuLi8uLi8uLi9ub2RlX21vZHVsZXMvY3NzLWxvYWRlci9pbmRleC5qcyEuL2Jvb3RzdHJhcC1kYXRldGltZXBpY2tlci5jc3NcIiwgZnVuY3Rpb24oKSB7XG5cdFx0XHR2YXIgbmV3Q29udGVudCA9IHJlcXVpcmUoXCIhIS4uLy4uLy4uL25vZGVfbW9kdWxlcy9jc3MtbG9hZGVyL2luZGV4LmpzIS4vYm9vdHN0cmFwLWRhdGV0aW1lcGlja2VyLmNzc1wiKTtcblx0XHRcdGlmKHR5cGVvZiBuZXdDb250ZW50ID09PSAnc3RyaW5nJykgbmV3Q29udGVudCA9IFtbbW9kdWxlLmlkLCBuZXdDb250ZW50LCAnJ11dO1xuXHRcdFx0dXBkYXRlKG5ld0NvbnRlbnQpO1xuXHRcdH0pO1xuXHR9XG5cdC8vIFdoZW4gdGhlIG1vZHVsZSBpcyBkaXNwb3NlZCwgcmVtb3ZlIHRoZSA8c3R5bGU+IHRhZ3Ncblx0bW9kdWxlLmhvdC5kaXNwb3NlKGZ1bmN0aW9uKCkgeyB1cGRhdGUoKTsgfSk7XG59IiwiLyoqXG4gKiBDcmVhdGVkIGJ5IEphY2t5LkdhbyBvbiAyMDE3LTAzLTE3LlxuICovXG5pbXBvcnQgJy4vZm9ybS9leHRlcm5hbC9ib290c3RyYXAtZGF0ZXRpbWVwaWNrZXIuY3NzJztcbmltcG9ydCB7XG4gICAgcG9pbnRUb01NLFxuICAgIHNob3dMb2FkaW5nLFxuICAgIGhpZGVMb2FkaW5nLFxuICAgIGdldFVybFBhcmFtXG59IGZyb20gJy4vVXRpbHMuanMnO1xuaW1wb3J0IHtcbiAgICBhbGVydFxufSBmcm9tICcuL01zZ0JveC5qcyc7XG5pbXBvcnQgUERGUHJpbnREaWFsb2cgZnJvbSAnLi9kaWFsb2cvUERGUHJpbnREaWFsb2cuanMnO1xuaW1wb3J0IGRlZmF1bHRJMThuSnNvbkRhdGEgZnJvbSAnLi9pMThuL3ByZXZpZXcuanNvbic7XG5pbXBvcnQgZW4xOG5Kc29uRGF0YSBmcm9tICcuL2kxOG4vcHJldmlld19lbi5qc29uJztcbihmdW5jdGlvbiAoJCkge1xuICAgICQuZm4uZGF0ZXRpbWVwaWNrZXIuZGF0ZXNbJ3poLUNOJ10gPSB7XG4gICAgICAgIGRheXM6IFtcIuaYn+acn+aXpVwiLCBcIuaYn+acn+S4gFwiLCBcIuaYn+acn+S6jFwiLCBcIuaYn+acn+S4iVwiLCBcIuaYn+acn+Wbm1wiLCBcIuaYn+acn+S6lFwiLCBcIuaYn+acn+WFrVwiLCBcIuaYn+acn+aXpVwiXSxcbiAgICAgICAgZGF5c1Nob3J0OiBbXCLlkajml6VcIiwgXCLlkajkuIBcIiwgXCLlkajkuoxcIiwgXCLlkajkuIlcIiwgXCLlkajlm5tcIiwgXCLlkajkupRcIiwgXCLlkajlha1cIiwgXCLlkajml6VcIl0sXG4gICAgICAgIGRheXNNaW46IFtcIuaXpVwiLCBcIuS4gFwiLCBcIuS6jFwiLCBcIuS4iVwiLCBcIuWbm1wiLCBcIuS6lFwiLCBcIuWFrVwiLCBcIuaXpVwiXSxcbiAgICAgICAgbW9udGhzOiBbXCLkuIDmnIhcIiwgXCLkuozmnIhcIiwgXCLkuInmnIhcIiwgXCLlm5vmnIhcIiwgXCLkupTmnIhcIiwgXCLlha3mnIhcIiwgXCLkuIPmnIhcIiwgXCLlhavmnIhcIiwgXCLkuZ3mnIhcIiwgXCLljYHmnIhcIiwgXCLljYHkuIDmnIhcIiwgXCLljYHkuozmnIhcIl0sXG4gICAgICAgIG1vbnRoc1Nob3J0OiBbXCLkuIDmnIhcIiwgXCLkuozmnIhcIiwgXCLkuInmnIhcIiwgXCLlm5vmnIhcIiwgXCLkupTmnIhcIiwgXCLlha3mnIhcIiwgXCLkuIPmnIhcIiwgXCLlhavmnIhcIiwgXCLkuZ3mnIhcIiwgXCLljYHmnIhcIiwgXCLljYHkuIDmnIhcIiwgXCLljYHkuozmnIhcIl0sXG4gICAgICAgIHRvZGF5OiBcIuS7iuWkqVwiLFxuICAgICAgICBzdWZmaXg6IFtdLFxuICAgICAgICBtZXJpZGllbTogW1wi5LiK5Y2IXCIsIFwi5LiL5Y2IXCJdXG4gICAgfTtcbn0oalF1ZXJ5KSk7XG5cbmNvbnN0IHByZXZpZXdJZCA9IGdldFVybFBhcmFtKCdpZCcpXG5jb25zdCBwYWdlTm8gPSBnZXRVcmxQYXJhbSgncGFnZScpIHx8IDBcbmNvbnN0IHRva2VuID0gZ2V0VXJsUGFyYW0oJ3Rva2VuJylcblxuXG4kKGRvY3VtZW50KS5yZWFkeShmdW5jdGlvbiAoKSB7XG5cbiAgICBsZXQganNQYWdlSW5kZXggPSAwXG4gICAgbGV0IGpzVG90YWxQYWdlID0gMVxuXG4gICAgLy8g5aSE55CG6aKE6KeI5YWz6Zet5oyJ6ZKuXG4gICAgaWYgKHByZXZpZXdJZCA9PT0gJ3ByZXZpZXcnKSB7XG4gICAgICAgICQoJy5wcmV2aWV3LWJ0bicpLmhpZGUoKVxuICAgIH0gZWxzZSB7XG4gICAgICAgICQoJy5wcmV2aWV3LWJ0bicpLnNob3coKVxuICAgIH1cblxuICAgIC8vIOmhtemdouWIneWni+WMllxuICAgIHBhZ2VJbml0KHBhZ2VObylcblxuICAgIGZ1bmN0aW9uIHBhZ2VJbml0KHBhZ2VObykge1xuICAgICAgICBzaG93TG9hZGluZygpO1xuICAgICAgICBjb25zb2xlLmxvZyh0b2tlbilcbiAgICAgICAgLy8g5Yid5aeL5YyW6aKE6KeI5oql6KGo5pWw5o2uXG4gICAgICAgICQuYWpheCh7XG4gICAgICAgICAgICB1cmw6IGAke3dpbmRvdy5fc2VydmVyfS9EYXRhUmVwb3J0L3ByZXZpZXc/aWQ9JHtwcmV2aWV3SWR9JnBhZ2U9JHtwYWdlTm99YCxcbiAgICAgICAgICAgIHR5cGU6ICdHRVQnLFxuICAgICAgICAgICAgaGVhZGVyczoge1xuICAgICAgICAgICAgICAgICdBdXRob3JpemF0aW9uJzogdG9rZW5cbiAgICAgICAgICAgIH0sXG4gICAgICAgICAgICBzdWNjZXNzOiAocmVzKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgZGF0YSA9IHJlcy5kYXRhXG4gICAgICAgICAgICAgICAgaWYgKHJlcy5jb2RlICE9PSAyMDApIHtcbiAgICAgICAgICAgICAgICAgICAgYWxlcnQocmVzLm1zZylcbiAgICAgICAgICAgICAgICB9IGVsc2Uge1xuICAgICAgICAgICAgICAgICAgICAvLyDpobXpnaLmlbDmja7muLLmn5NcbiAgICAgICAgICAgICAgICAgICAgbGV0IHN0eWxlcyA9IGA8c3R5bGUgdHlwZT1cInRleHQvY3NzXCI+YFxuICAgICAgICAgICAgICAgICAgICBzdHlsZXMgKz0gZGF0YS5zdHlsZTtcbiAgICAgICAgICAgICAgICAgICAgc3R5bGVzICs9IGA8L3N0eWxlPmA7XG4gICAgICAgICAgICAgICAgICAgICQoJyNfdXJlcG9ydF90YWJsZScpLmh0bWwoc3R5bGVzICsgZGF0YS5jb250ZW50KVxuICAgICAgICAgICAgICAgICAgICAkKCcjX3VyZXBvcnRfdGFibGVfc3R5bGUnKS5odG1sKGRhdGEuc3R5bGUpXG5cbiAgICAgICAgICAgICAgICAgICAganNQYWdlSW5kZXggPSBkYXRhLnBhZ2VJbmRleFxuICAgICAgICAgICAgICAgICAgICBqc1RvdGFsUGFnZSA9IGRhdGEudG90YWxQYWdlXG5cbiAgICAgICAgICAgICAgICAgICAgaWYgKGRhdGEucGFnZUluZGV4ID09PSAwKSB7XG4gICAgICAgICAgICAgICAgICAgICAgICAvLyDkuI3liIbpobVcbiAgICAgICAgICAgICAgICAgICAgICAgICQoJyNwYWdlTGlua0NvbnRhaW5lcicpLnNob3coKVxuICAgICAgICAgICAgICAgICAgICAgICAgJCgnLnBhZ2VOdW0nKS5odG1sKCcxLzEnKVxuICAgICAgICAgICAgICAgICAgICB9IGVsc2Uge1xuICAgICAgICAgICAgICAgICAgICAgICAgJCgnI3BhZ2VMaW5rQ29udGFpbmVyJykuc2hvdygpXG4gICAgICAgICAgICAgICAgICAgICAgICAkKCcucGFnZU51bScpLmh0bWwoYCR7anNQYWdlSW5kZXh9LyR7anNUb3RhbFBhZ2V9YClcbiAgICAgICAgICAgICAgICAgICAgfVxuXG4gICAgICAgICAgICAgICAgICAgIC8vIOiHquWKqOWIt+aWsOWkhOeQhlxuICAgICAgICAgICAgICAgICAgICBpZiAoZGF0YS5odG1sSW50ZXJ2YWxSZWZyZXNoVmFsdWUgPiAwKSBfaW50ZXJ2YWxSZWZyZXNoKGRhdGEuaHRtbEludGVydmFsUmVmcmVzaFZhbHVlLCBkYXRhLnRvdGFsUGFnZVdpdGhDb2wpO1xuXG4gICAgICAgICAgICAgICAgICAgIC8vIOWbvuihqOaVsOaNrlxuICAgICAgICAgICAgICAgICAgICBfYnVpbGRDaGFydERhdGFzKGRhdGEuY2hhcnREYXRhcyk7XG5cbiAgICAgICAgICAgICAgICAgICAgaGlkZUxvYWRpbmcoKTtcbiAgICAgICAgICAgICAgICAgICAgJCgnI191cmVwb3J0X3RhYmxlJykuc2hvdygpXG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgfSxcbiAgICAgICAgICAgIGVycm9yOiAocmVzcG9uc2UpID0+IHtcbiAgICAgICAgICAgICAgICBoaWRlTG9hZGluZygpO1xuICAgICAgICAgICAgICAgIGlmIChyZXNwb25zZSAmJiByZXNwb25zZS5yZXNwb25zZVRleHQpIHtcbiAgICAgICAgICAgICAgICAgICAgYWxlcnQoXCLmnI3liqHnq6/plJnor6/vvJpcIiArIHJlc3BvbnNlLnJlc3BvbnNlVGV4dCArIFwiXCIpO1xuICAgICAgICAgICAgICAgIH0gZWxzZSB7XG4gICAgICAgICAgICAgICAgICAgIGFsZXJ0KFwi5pyN5Yqh56uv5Ye66ZSZ77yBXCIpO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgIH1cbiAgICAgICAgfSlcbiAgICB9XG5cbiAgICAvLyDnrKzkuIDpobVcbiAgICAkKCcucGFnZUluZGV4Jykub24oJ2NsaWNrJywgKCkgPT4ge1xuICAgICAgICBpZiAoanNQYWdlSW5kZXggIT09IDApIHtcbiAgICAgICAgICAgIGlmIChqc1BhZ2VJbmRleCA9PT0gMSkgcmV0dXJuXG4gICAgICAgICAgICBwYWdlSW5pdCgxKVxuICAgICAgICB9XG4gICAgfSlcblxuICAgIC8vIOS4iuS4gOmhtVxuICAgICQoJy5wYWdlUHJlJykub24oJ2NsaWNrJywgKCkgPT4ge1xuICAgICAgICBpZiAoanNQYWdlSW5kZXggIT09IDApIHtcbiAgICAgICAgICAgIGlmIChqc1BhZ2VJbmRleCA9PT0gMSkgcmV0dXJuXG4gICAgICAgICAgICBwYWdlSW5pdChqc1BhZ2VJbmRleCAtIDEpXG4gICAgICAgIH1cbiAgICB9KVxuXG4gICAgLy8g5LiL5LiA6aG1XG4gICAgJCgnLnBhZ2VOZXh0Jykub24oJ2NsaWNrJywgKCkgPT4ge1xuICAgICAgICBpZiAoanNQYWdlSW5kZXggIT09IDApIHtcbiAgICAgICAgICAgIGlmIChqc1BhZ2VJbmRleCA9PT0ganNUb3RhbFBhZ2UpIHJldHVyblxuICAgICAgICAgICAgcGFnZUluaXQoanNQYWdlSW5kZXggKyAxKVxuICAgICAgICB9XG4gICAgfSlcblxuICAgIC8vIOacgOWQjuS4gOmhtVxuICAgICQoJy5wYWdlTGFzdCcpLm9uKCdjbGljaycsICgpID0+IHtcbiAgICAgICAgaWYgKGpzUGFnZUluZGV4ICE9PSAwKSB7XG4gICAgICAgICAgICBpZiAoanNQYWdlSW5kZXggPT09IGpzVG90YWxQYWdlKSByZXR1cm5cbiAgICAgICAgICAgIHBhZ2VJbml0KGpzVG90YWxQYWdlKVxuICAgICAgICB9XG4gICAgfSlcblxuXG4gICAgbGV0IGxhbmd1YWdlID0gd2luZG93Lm5hdmlnYXRvci5sYW5ndWFnZSB8fCB3aW5kb3cubmF2aWdhdG9yLmJyb3dzZXJMYW5ndWFnZTtcbiAgICBpZiAoIWxhbmd1YWdlKSB7XG4gICAgICAgIGxhbmd1YWdlID0gJ3poLWNuJztcbiAgICB9XG4gICAgbGFuZ3VhZ2UgPSBsYW5ndWFnZS50b0xvd2VyQ2FzZSgpO1xuICAgIHdpbmRvdy5pMThuID0gZGVmYXVsdEkxOG5Kc29uRGF0YTtcbiAgICBpZiAobGFuZ3VhZ2UgIT09ICd6aC1jbicpIHtcbiAgICAgICAgd2luZG93LmkxOG4gPSBlbjE4bkpzb25EYXRhO1xuICAgIH1cblxuICAgIGxldCBkaXJlY3RQcmludFBkZiA9IGZhbHNlLFxuICAgICAgICBpbmRleCA9IDA7XG4gICAgY29uc3QgcGRmUHJpbnREaWFsb2cgPSBuZXcgUERGUHJpbnREaWFsb2coKTtcblxuICAgIC8vIOWFs+mXrVxuICAgICQoXCIucHJldmlldy1idG5cIikub24oJ2NsaWNrJywgKCkgPT4ge1xuICAgICAgICB3aW5kb3cucGFyZW50LnBvc3RNZXNzYWdlKCdjbG9zZURpYWxvZycsICcqJylcbiAgICB9KVxuXG4gICAgLy8g5Zyo57q/5omT5Y2wXG4gICAgJCgnLnVyZXBvcnQtcHJpbnQnKS5vbignY2xpY2snLCAoKSA9PiB7XG4gICAgICAgIGNvbnN0IHVybFBhcmFtZXRlcnMgPSBidWlsZExvY2F0aW9uU2VhcmNoUGFyYW1ldGVycygpO1xuICAgICAgICBjb25zdCB1cmwgPSB3aW5kb3cuX3NlcnZlciArICcvcHJldmlldy9sb2FkUHJpbnRQYWdlcycgKyB1cmxQYXJhbWV0ZXJzO1xuICAgICAgICBzaG93TG9hZGluZygpO1xuICAgICAgICAkLmFqYXgoe1xuICAgICAgICAgICAgdXJsLFxuICAgICAgICAgICAgdHlwZTogJ1BPU1QnLFxuICAgICAgICAgICAgaGVhZGVyczoge1xuICAgICAgICAgICAgICAgICdBdXRob3JpemF0aW9uJzogdG9rZW5cbiAgICAgICAgICAgIH0sXG4gICAgICAgICAgICBzdWNjZXNzOiAocmVzdWx0KSA9PiB7XG4gICAgICAgICAgICAgICAgJC5nZXQod2luZG93Ll9zZXJ2ZXIgKyAnL3ByZXZpZXcvbG9hZFBhZ2VQYXBlcicgKyB1cmxQYXJhbWV0ZXJzLCBmdW5jdGlvbiAocGFwZXIpIHtcbiAgICAgICAgICAgICAgICAgICAgaGlkZUxvYWRpbmcoKTtcbiAgICAgICAgICAgICAgICAgICAgY29uc3QgaHRtbCA9IHJlc3VsdC5kYXRhO1xuICAgICAgICAgICAgICAgICAgICBjb25zdCBpRnJhbWUgPSB3aW5kb3cuZnJhbWVzWydfcHJpbnRfZnJhbWUnXTtcbiAgICAgICAgICAgICAgICAgICAgbGV0IHN0eWxlcyA9IGA8c3R5bGUgdHlwZT1cInRleHQvY3NzXCI+YDtcbiAgICAgICAgICAgICAgICAgICAgc3R5bGVzICs9IGJ1aWxkUHJpbnRTdHlsZShwYXBlcik7XG4gICAgICAgICAgICAgICAgICAgIHN0eWxlcyArPSAkKCcjX3VyZXBvcnRfdGFibGVfc3R5bGUnKS5odG1sKCk7XG4gICAgICAgICAgICAgICAgICAgIHN0eWxlcyArPSBgPC9zdHlsZT5gO1xuICAgICAgICAgICAgICAgICAgICAkKGlGcmFtZS5kb2N1bWVudC5ib2R5KS5odG1sKHN0eWxlcyArIGh0bWwpO1xuICAgICAgICAgICAgICAgICAgICBpRnJhbWUud2luZG93LmZvY3VzKCk7XG4gICAgICAgICAgICAgICAgICAgIGlGcmFtZS53aW5kb3cucHJpbnQoKTtcbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgIH0sXG4gICAgICAgICAgICBlcnJvcjogKHJlc3BvbnNlKSA9PiB7XG4gICAgICAgICAgICAgICAgaGlkZUxvYWRpbmcoKTtcbiAgICAgICAgICAgICAgICBpZiAocmVzcG9uc2UgJiYgcmVzcG9uc2UucmVzcG9uc2VUZXh0KSB7XG4gICAgICAgICAgICAgICAgICAgIGFsZXJ0KFwi5pyN5Yqh56uv6ZSZ6K+v77yaXCIgKyByZXNwb25zZS5yZXNwb25zZVRleHQgKyBcIlwiKTtcbiAgICAgICAgICAgICAgICB9IGVsc2Uge1xuICAgICAgICAgICAgICAgICAgICBhbGVydChcIuacjeWKoeerr+WHuumUme+8gVwiKTtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuICAgIH0pO1xuXG4gICAgLy8gUERG5Zyo57q/6aKE6KeI5omT5Y2wXG4gICAgJCgnLnVyZXBvcnQtcGRmLXByaW50Jykub24oJ2NsaWNrJywgKCkgPT4ge1xuICAgICAgICBjb25zdCB1cmxQYXJhbWV0ZXJzID0gYnVpbGRMb2NhdGlvblNlYXJjaFBhcmFtZXRlcnMoKTtcbiAgICAgICAgJC5nZXQod2luZG93Ll9zZXJ2ZXIgKyAnL3ByZXZpZXcvbG9hZFBhZ2VQYXBlcicgKyB1cmxQYXJhbWV0ZXJzLCBmdW5jdGlvbiAocGFwZXIpIHtcbiAgICAgICAgICAgIHBkZlByaW50RGlhbG9nLnNob3cocGFwZXIpO1xuICAgICAgICB9KTtcbiAgICB9KVxuXG4gICAgLy8g5Yi35pawXG4gICAgJCgnLnVyZXBvcnQtcmVmcmVzaCcpLm9uKCdjbGljaycsICgpID0+IHtcbiAgICAgICAgbG9jYXRpb24ucmVsb2FkKClcbiAgICB9KVxuXG5cbiAgICAvLyBQREblnKjnur/miZPljbBcbiAgICAkKCcudXJlcG9ydC1wZGYtZGlyZWN0LXByaW50Jykub24oJ2NsaWNrJywgKCkgPT4ge1xuXG4gICAgICAgIHNob3dMb2FkaW5nKCk7XG4gICAgICAgIGNvbnN0IHVybFBhcmFtZXRlcnMgPSBidWlsZExvY2F0aW9uU2VhcmNoUGFyYW1ldGVycygpO1xuICAgICAgICBjb25zdCB1cmwgPSB3aW5kb3cuX3NlcnZlciArICcvcGRmL3Nob3cnICsgdXJsUGFyYW1ldGVycyArIGAmX2k9JHtpbmRleCsrfWA7XG4gICAgICAgIGNvbnN0IGlmcmFtZSA9IHdpbmRvdy5mcmFtZXNbJ19wcmludF9wZGZfZnJhbWUnXTtcbiAgICAgICAgaWYgKCFkaXJlY3RQcmludFBkZikge1xuICAgICAgICAgICAgZGlyZWN0UHJpbnRQZGYgPSB0cnVlO1xuICAgICAgICAgICAgJChcImlmcmFtZVtuYW1lPSdfcHJpbnRfcGRmX2ZyYW1lJ11cIikub24oXCJsb2FkXCIsIGZ1bmN0aW9uICgpIHtcbiAgICAgICAgICAgICAgICBoaWRlTG9hZGluZygpO1xuICAgICAgICAgICAgICAgIGlmcmFtZS53aW5kb3cuZm9jdXMoKTtcbiAgICAgICAgICAgICAgICBpZnJhbWUud2luZG93LnByaW50KCk7XG4gICAgICAgICAgICB9KTtcbiAgICAgICAgfVxuICAgICAgICBpZnJhbWUud2luZG93LmZvY3VzKCk7XG4gICAgICAgIGlmcmFtZS5sb2NhdGlvbi5ocmVmID0gdXJsO1xuXG4gICAgICAgIC8vIHNob3dMb2FkaW5nKCk7XG4gICAgICAgIC8vIGNvbnN0IHVybFBhcmFtZXRlcnMgPSBidWlsZExvY2F0aW9uU2VhcmNoUGFyYW1ldGVycygpO1xuICAgICAgICAvLyBjb25zdCB1cmwgPSB3aW5kb3cuX3NlcnZlciArICcvcGRmL3Nob3cnICsgdXJsUGFyYW1ldGVycyArIGAmX2k9JHtpbmRleCsrfWA7XG4gICAgICAgIC8vIGNvbnN0IGlGcmFtZSA9IHdpbmRvdy5mcmFtZXNbJ19wcmludF9wZGZfZnJhbWUnXTtcbiAgICAgICAgLy8gaWYgKCFkaXJlY3RQcmludFBkZikge1xuICAgICAgICAvLyAgICAgZGlyZWN0UHJpbnRQZGYgPSB0cnVlO1xuICAgICAgICAvLyAgICAgJChcImlmcmFtZVtuYW1lPSdfcHJpbnRfcGRmX2ZyYW1lJ11cIikub24oXCJsb2FkXCIsIGZ1bmN0aW9uICgpIHtcbiAgICAgICAgLy8gICAgICAgICBoaWRlTG9hZGluZygpO1xuICAgICAgICAvLyAgICAgICAgIGlGcmFtZS53aW5kb3cuZm9jdXMoKTtcbiAgICAgICAgLy8gICAgICAgICBpRnJhbWUud2luZG93LnByaW50KCk7XG4gICAgICAgIC8vICAgICB9KTtcbiAgICAgICAgLy8gfVxuICAgICAgICAvLyBpRnJhbWUud2luZG93LmZvY3VzKCk7XG4gICAgICAgIC8vIGlGcmFtZS5sb2NhdGlvbi5ocmVmID0gdXJsO1xuICAgIH0pXG5cbiAgICAvLyDlr7zlh7pQREZcbiAgICAkKCcudXJlcG9ydC1leHBvcnQtcGRmJykub24oJ2NsaWNrJywgKCkgPT4ge1xuICAgICAgICBjb25zdCB1cmxQYXJhbWV0ZXJzID0gYnVpbGRMb2NhdGlvblNlYXJjaFBhcmFtZXRlcnMoKTtcbiAgICAgICAgY29uc3QgdXJsID0gd2luZG93Ll9zZXJ2ZXIgKyAnL3BkZicgKyB1cmxQYXJhbWV0ZXJzO1xuICAgICAgICB3aW5kb3cub3Blbih1cmwsICdfYmxhbmsnKTtcbiAgICB9KVxuXG4gICAgLy8g5a+85Ye6V09SRFxuICAgICQoYC51cmVwb3J0LWV4cG9ydC13b3JkYCkub24oJ2NsaWNrJywgKCkgPT4ge1xuICAgICAgICBjb25zdCB1cmxQYXJhbWV0ZXJzID0gYnVpbGRMb2NhdGlvblNlYXJjaFBhcmFtZXRlcnMoKTtcbiAgICAgICAgY29uc3QgdXJsID0gd2luZG93Ll9zZXJ2ZXIgKyAnL3dvcmQnICsgdXJsUGFyYW1ldGVycztcbiAgICAgICAgd2luZG93Lm9wZW4odXJsLCAnX2JsYW5rJyk7XG4gICAgfSlcblxuICAgIC8vIOWvvOWHukV4Y2VsXG4gICAgJChgLnVyZXBvcnQtZXhwb3J0LWV4Y2VsYCkub24oJ2NsaWNrJywgKCkgPT4ge1xuICAgICAgICBjb25zdCB1cmxQYXJhbWV0ZXJzID0gYnVpbGRMb2NhdGlvblNlYXJjaFBhcmFtZXRlcnMoKTtcbiAgICAgICAgY29uc3QgdXJsID0gd2luZG93Ll9zZXJ2ZXIgKyAnL2V4Y2VsJyArIHVybFBhcmFtZXRlcnM7XG4gICAgICAgIHdpbmRvdy5vcGVuKHVybCwgJ19ibGFuaycpO1xuICAgIH0pO1xuXG4gICAgLy8g5YiG6aG15a+85Ye6RVhDRUxcbiAgICAkKGAudXJlcG9ydC1leHBvcnQtZXhjZWwtcGFnaW5nYCkub24oJ2NsaWNrJywgKCkgPT4ge1xuICAgICAgICBjb25zdCB1cmxQYXJhbWV0ZXJzID0gYnVpbGRMb2NhdGlvblNlYXJjaFBhcmFtZXRlcnMoKTtcbiAgICAgICAgY29uc3QgdXJsID0gd2luZG93Ll9zZXJ2ZXIgKyAnL2V4Y2VsL3BhZ2luZycgKyB1cmxQYXJhbWV0ZXJzO1xuICAgICAgICB3aW5kb3cub3Blbih1cmwsICdfYmxhbmsnKTtcbiAgICB9KTtcblxuICAgIC8vIOWIhumhteWIhlNoZWV05a+85Ye6RVhDRUxcbiAgICAkKGAudXJlcG9ydC1leHBvcnQtZXhjZWwtcGFnaW5nLXNoZWV0YCkub24oJ2NsaWNrJywgKCkgPT4ge1xuICAgICAgICBjb25zdCB1cmxQYXJhbWV0ZXJzID0gYnVpbGRMb2NhdGlvblNlYXJjaFBhcmFtZXRlcnMoKTtcbiAgICAgICAgY29uc3QgdXJsID0gd2luZG93Ll9zZXJ2ZXIgKyAnL2V4Y2VsL3NoZWV0JyArIHVybFBhcmFtZXRlcnM7XG4gICAgICAgIHdpbmRvdy5vcGVuKHVybCwgJ19ibGFuaycpO1xuICAgIH0pO1xufSk7XG5cbndpbmRvdy5fY3VycmVudFBhZ2VJbmRleCA9IG51bGw7XG53aW5kb3cuX3RvdGFsUGFnZSA9IG51bGw7XG5cbndpbmRvdy5idWlsZExvY2F0aW9uU2VhcmNoUGFyYW1ldGVycyA9IGZ1bmN0aW9uIChleGNsdWRlKSB7XG5cbiAgICBsZXQgdXJsUGFyYW1ldGVycyA9IGAke3dpbmRvdy5sb2NhdGlvbi5zZWFyY2h9JnRva2VuPSR7dG9rZW59YDtcbiAgICBpZiAodXJsUGFyYW1ldGVycy5sZW5ndGggPiAwKSB7XG4gICAgICAgIHVybFBhcmFtZXRlcnMgPSB1cmxQYXJhbWV0ZXJzLnN1YnN0cmluZygxLCB1cmxQYXJhbWV0ZXJzLmxlbmd0aCk7XG4gICAgfVxuICAgIGxldCBwYXJhbWV0ZXJzID0ge307XG4gICAgY29uc3QgcGFpcnMgPSB1cmxQYXJhbWV0ZXJzLnNwbGl0KCcmJyk7XG4gICAgZm9yIChsZXQgaSA9IDA7IGkgPCBwYWlycy5sZW5ndGg7IGkrKykge1xuICAgICAgICBjb25zdCBpdGVtID0gcGFpcnNbaV07XG4gICAgICAgIGlmIChpdGVtID09PSAnJykge1xuICAgICAgICAgICAgY29udGludWU7XG4gICAgICAgIH1cbiAgICAgICAgY29uc3QgcGFyYW0gPSBpdGVtLnNwbGl0KCc9Jyk7XG4gICAgICAgIGxldCBrZXkgPSBwYXJhbVswXTtcbiAgICAgICAgaWYgKGV4Y2x1ZGUgJiYga2V5ID09PSBleGNsdWRlKSB7XG4gICAgICAgICAgICBjb250aW51ZTtcbiAgICAgICAgfVxuICAgICAgICBsZXQgdmFsdWUgPSBwYXJhbVsxXTtcbiAgICAgICAgcGFyYW1ldGVyc1trZXldID0gdmFsdWU7XG4gICAgfVxuICAgIGlmICh3aW5kb3cuc2VhcmNoRm9ybVBhcmFtZXRlcnMpIHtcbiAgICAgICAgZm9yIChsZXQga2V5IGluIHdpbmRvdy5zZWFyY2hGb3JtUGFyYW1ldGVycykge1xuICAgICAgICAgICAgaWYgKGtleSA9PT0gZXhjbHVkZSkge1xuICAgICAgICAgICAgICAgIGNvbnRpbnVlO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgY29uc3QgdmFsdWUgPSB3aW5kb3cuc2VhcmNoRm9ybVBhcmFtZXRlcnNba2V5XTtcbiAgICAgICAgICAgIGlmICh2YWx1ZSkge1xuICAgICAgICAgICAgICAgIHBhcmFtZXRlcnNba2V5XSA9IHZhbHVlO1xuICAgICAgICAgICAgfVxuICAgICAgICB9XG4gICAgfVxuICAgIGxldCBwID0gJz8nO1xuICAgIGZvciAobGV0IGtleSBpbiBwYXJhbWV0ZXJzKSB7XG4gICAgICAgIGlmIChwID09PSAnPycpIHtcbiAgICAgICAgICAgIHAgKz0ga2V5ICsgJz0nICsgcGFyYW1ldGVyc1trZXldO1xuICAgICAgICB9IGVsc2Uge1xuICAgICAgICAgICAgcCArPSAnJicgKyBrZXkgKyAnPScgKyBwYXJhbWV0ZXJzW2tleV07XG4gICAgICAgIH1cbiAgICB9XG4gICAgcmV0dXJuIHA7XG59O1xuXG5mdW5jdGlvbiBidWlsZFByaW50U3R5bGUocGFwZXIpIHtcbiAgICBjb25zdCBtYXJnaW5MZWZ0ID0gcG9pbnRUb01NKHBhcGVyLmxlZnRNYXJnaW4pO1xuICAgIGNvbnN0IG1hcmdpblRvcCA9IHBvaW50VG9NTShwYXBlci50b3BNYXJnaW4pO1xuICAgIGNvbnN0IG1hcmdpblJpZ2h0ID0gcG9pbnRUb01NKHBhcGVyLnJpZ2h0TWFyZ2luKTtcbiAgICBjb25zdCBtYXJnaW5Cb3R0b20gPSBwb2ludFRvTU0ocGFwZXIuYm90dG9tTWFyZ2luKTtcbiAgICBjb25zdCBwYXBlclR5cGUgPSBwYXBlci5wYXBlclR5cGU7XG4gICAgbGV0IHBhZ2UgPSBwYXBlclR5cGU7XG4gICAgaWYgKHBhcGVyVHlwZSA9PT0gJ0NVU1RPTScpIHtcbiAgICAgICAgcGFnZSA9IHBvaW50VG9NTShwYXBlci53aWR0aCkgKyAnbW0gJyArIHBvaW50VG9NTShwYXBlci5oZWlnaHQpICsgJ21tJztcbiAgICB9XG4gICAgY29uc3Qgc3R5bGUgPSBgXG4gICAgICAgIEBtZWRpYSBwcmludCB7XG4gICAgICAgICAgICAucGFnZS1icmVha3tcbiAgICAgICAgICAgICAgICBkaXNwbGF5OiBibG9jaztcbiAgICAgICAgICAgICAgICBwYWdlLWJyZWFrLWJlZm9yZTogYWx3YXlzO1xuICAgICAgICAgICAgfVxuICAgICAgICB9XG4gICAgICAgIEBwYWdlIHtcbiAgICAgICAgICBzaXplOiAke3BhZ2V9ICR7cGFwZXIub3JpZW50YXRpb259O1xuICAgICAgICAgIG1hcmdpbi1sZWZ0OiAke21hcmdpbkxlZnR9bW07XG4gICAgICAgICAgbWFyZ2luLXRvcDogJHttYXJnaW5Ub3B9bW07XG4gICAgICAgICAgbWFyZ2luLXJpZ2h0OiR7bWFyZ2luUmlnaHR9bW07XG4gICAgICAgICAgbWFyZ2luLWJvdHRvbToke21hcmdpbkJvdHRvbX1tbTtcbiAgICAgICAgfVxuICAgIGA7XG4gICAgcmV0dXJuIHN0eWxlO1xufTtcblxuXG53aW5kb3cuX2ludGVydmFsUmVmcmVzaCA9IGZ1bmN0aW9uICh2YWx1ZSwgdG90YWxQYWdlKSB7XG4gICAgaWYgKCF2YWx1ZSkge1xuICAgICAgICByZXR1cm47XG4gICAgfVxuICAgIHdpbmRvdy5fdG90YWxQYWdlID0gdG90YWxQYWdlO1xuICAgIGNvbnN0IHNlY29uZCA9IHZhbHVlICogMTAwMDtcbiAgICBzZXRUaW1lb3V0KGZ1bmN0aW9uICgpIHtcbiAgICAgICAgX3JlZnJlc2hEYXRhKHNlY29uZCk7XG4gICAgfSwgc2Vjb25kKTtcbn07XG5cbmZ1bmN0aW9uIF9yZWZyZXNoRGF0YShzZWNvbmQpIHtcbiAgICBjb25zdCBwYXJhbXMgPSBidWlsZExvY2F0aW9uU2VhcmNoUGFyYW1ldGVycygnX2knKTtcbiAgICBsZXQgdXJsID0gd2luZG93Ll9zZXJ2ZXIgKyBgL3ByZXZpZXcvbG9hZERhdGEke3BhcmFtc31gO1xuICAgIGNvbnN0IHRvdGFsUGFnZSA9IHdpbmRvdy5fdG90YWxQYWdlO1xuICAgIGlmICh0b3RhbFBhZ2UgPiAwKSB7XG4gICAgICAgIGlmICh3aW5kb3cuX2N1cnJlbnRQYWdlSW5kZXgpIHtcbiAgICAgICAgICAgIGlmICh3aW5kb3cuX2N1cnJlbnRQYWdlSW5kZXggPiB0b3RhbFBhZ2UpIHtcbiAgICAgICAgICAgICAgICB3aW5kb3cuX2N1cnJlbnRQYWdlSW5kZXggPSAxO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgdXJsICs9IFwiJl9pPVwiICsgd2luZG93Ll9jdXJyZW50UGFnZUluZGV4ICsgXCJcIjtcbiAgICAgICAgfVxuICAgICAgICAkKFwiI3BhZ2VTZWxlY3RvclwiKS52YWwod2luZG93Ll9jdXJyZW50UGFnZUluZGV4KTtcbiAgICB9XG4gICAgJC5hamF4KHtcbiAgICAgICAgdXJsLFxuICAgICAgICB0eXBlOiAnR0VUJyxcbiAgICAgICAgaGVhZGVyczoge1xuICAgICAgICAgICAgJ0F1dGhvcml6YXRpb24nOiB0b2tlblxuICAgICAgICB9LFxuICAgICAgICBzdWNjZXNzOiAocmVwb3J0KSA9PiB7XG4gICAgICAgICAgICBjb25zdCB0YWJsZUNvbnRhaW5lciA9ICQoYCNfdXJlcG9ydF90YWJsZWApO1xuICAgICAgICAgICAgdGFibGVDb250YWluZXIuZW1wdHkoKTtcbiAgICAgICAgICAgIHdpbmRvdy5fdG90YWxQYWdlID0gcmVwb3J0LnRvdGFsUGFnZVdpdGhDb2w7XG4gICAgICAgICAgICB0YWJsZUNvbnRhaW5lci5hcHBlbmQocmVwb3J0LmNvbnRlbnQpO1xuICAgICAgICAgICAgX2J1aWxkQ2hhcnREYXRhcyhyZXBvcnQuY2hhcnREYXRhcyk7XG4gICAgICAgICAgICBidWlsZFBhZ2luZyh3aW5kb3cuX2N1cnJlbnRQYWdlSW5kZXgsIHdpbmRvdy5fdG90YWxQYWdlKTtcbiAgICAgICAgICAgIGlmICh3aW5kb3cuX2N1cnJlbnRQYWdlSW5kZXgpIHtcbiAgICAgICAgICAgICAgICB3aW5kb3cuX2N1cnJlbnRQYWdlSW5kZXgrKztcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIHNldFRpbWVvdXQoZnVuY3Rpb24gKCkge1xuICAgICAgICAgICAgICAgIF9yZWZyZXNoRGF0YShzZWNvbmQpO1xuICAgICAgICAgICAgfSwgc2Vjb25kKTtcbiAgICAgICAgfSxcbiAgICAgICAgZXJyb3I6IGZ1bmN0aW9uIChyZXNwb25zZSkge1xuICAgICAgICAgICAgY29uc3QgdGFibGVDb250YWluZXIgPSAkKGAjX3VyZXBvcnRfdGFibGVgKTtcbiAgICAgICAgICAgIHRhYmxlQ29udGFpbmVyLmVtcHR5KCk7XG4gICAgICAgICAgICBpZiAocmVzcG9uc2UgJiYgcmVzcG9uc2UucmVzcG9uc2VUZXh0KSB7XG4gICAgICAgICAgICAgICAgdGFibGVDb250YWluZXIuYXBwZW5kKFwiPGgzIHN0eWxlPSdjb2xvcjogI2QzMGUwMDsnPuacjeWKoeerr+mUmeivr++8mlwiICsgcmVzcG9uc2UucmVzcG9uc2VUZXh0ICsgXCI8L2gzPlwiKTtcbiAgICAgICAgICAgIH0gZWxzZSB7XG4gICAgICAgICAgICAgICAgdGFibGVDb250YWluZXIuYXBwZW5kKFwiPGgzIHN0eWxlPSdjb2xvcjogI2QzMGUwMDsnPuWKoOi9veaVsOaNruWksei0pTwvaDM+XCIpO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgc2V0VGltZW91dChmdW5jdGlvbiAoKSB7XG4gICAgICAgICAgICAgICAgX3JlZnJlc2hEYXRhKHNlY29uZCk7XG4gICAgICAgICAgICB9LCBzZWNvbmQpO1xuICAgICAgICB9XG4gICAgfSk7XG59O1xuXG53aW5kb3cuX2J1aWxkQ2hhcnREYXRhcyA9IGZ1bmN0aW9uIChjaGFydERhdGEpIHtcbiAgICBpZiAoIWNoYXJ0RGF0YSkge1xuICAgICAgICByZXR1cm47XG4gICAgfVxuICAgIGZvciAobGV0IGQgb2YgY2hhcnREYXRhKSB7XG4gICAgICAgIGxldCBqc29uID0gZC5qc29uO1xuICAgICAgICBqc29uID0gSlNPTi5wYXJzZShqc29uLCBmdW5jdGlvbiAoaywgdikge1xuICAgICAgICAgICAgaWYgKHYuaW5kZXhPZiAmJiB2LmluZGV4T2YoJ2Z1bmN0aW9uJykgPiAtMSkge1xuICAgICAgICAgICAgICAgIHJldHVybiBldmFsKFwiKGZ1bmN0aW9uKCl7cmV0dXJuIFwiICsgdiArIFwiIH0pKClcIilcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIHJldHVybiB2O1xuICAgICAgICB9KTtcbiAgICAgICAgX2J1aWxkQ2hhcnQoZC5pZCwganNvbik7XG4gICAgfVxufTtcbndpbmRvdy5fYnVpbGRDaGFydCA9IGZ1bmN0aW9uIChjYW52YXNJZCwgY2hhcnRKc29uKSB7XG4gICAgY29uc3QgY3R4ID0gZG9jdW1lbnQuZ2V0RWxlbWVudEJ5SWQoY2FudmFzSWQpO1xuICAgIGlmICghY3R4KSB7XG4gICAgICAgIHJldHVybjtcbiAgICB9XG4gICAgbGV0IG9wdGlvbnMgPSBjaGFydEpzb24ub3B0aW9ucztcbiAgICBpZiAoIW9wdGlvbnMpIHtcbiAgICAgICAgb3B0aW9ucyA9IHt9O1xuICAgICAgICBjaGFydEpzb24ub3B0aW9ucyA9IG9wdGlvbnM7XG4gICAgfVxuICAgIGxldCBhbmltYXRpb24gPSBvcHRpb25zLmFuaW1hdGlvbjtcbiAgICBpZiAoIWFuaW1hdGlvbikge1xuICAgICAgICBhbmltYXRpb24gPSB7fTtcbiAgICAgICAgb3B0aW9ucy5hbmltYXRpb24gPSBhbmltYXRpb247XG4gICAgfVxuICAgIGFuaW1hdGlvbi5vbkNvbXBsZXRlID0gZnVuY3Rpb24gKGV2ZW50KSB7XG4gICAgICAgIGNvbnN0IGNoYXJ0ID0gZXZlbnQuY2hhcnQ7XG4gICAgICAgIGNvbnN0IGJhc2U2NEltYWdlID0gY2hhcnQudG9CYXNlNjRJbWFnZSgpO1xuICAgICAgICBjb25zdCB1cmxQYXJhbWV0ZXJzID0gd2luZG93LmxvY2F0aW9uLnNlYXJjaDtcbiAgICAgICAgY29uc3QgdXJsID0gd2luZG93Ll9zZXJ2ZXIgKyAnL2NoYXJ0L3N0b3JlRGF0YScgKyB1cmxQYXJhbWV0ZXJzO1xuICAgICAgICBjb25zdCBjYW52YXMgPSAkKFwiI1wiICsgY2FudmFzSWQpO1xuICAgICAgICBjb25zdCB3aWR0aCA9IHBhcnNlSW50KGNhbnZhcy5jc3MoJ3dpZHRoJykpO1xuICAgICAgICBjb25zdCBoZWlnaHQgPSBwYXJzZUludChjYW52YXMuY3NzKCdoZWlnaHQnKSk7XG4gICAgICAgICQuYWpheCh7XG4gICAgICAgICAgICB0eXBlOiAnUE9TVCcsXG4gICAgICAgICAgICBoZWFkZXJzOiB7XG4gICAgICAgICAgICAgICAgJ0F1dGhvcml6YXRpb24nOiB0b2tlblxuICAgICAgICAgICAgfSxcbiAgICAgICAgICAgIGRhdGE6IHtcbiAgICAgICAgICAgICAgICBfYmFzZTY0RGF0YTogYmFzZTY0SW1hZ2UsXG4gICAgICAgICAgICAgICAgX2NoYXJ0SWQ6IGNhbnZhc0lkLFxuICAgICAgICAgICAgICAgIF93aWR0aDogd2lkdGgsXG4gICAgICAgICAgICAgICAgX2hlaWdodDogaGVpZ2h0XG4gICAgICAgICAgICB9LFxuICAgICAgICAgICAgdXJsXG4gICAgICAgIH0pO1xuICAgIH07XG4gICAgY29uc3QgY2hhcnQgPSBuZXcgQ2hhcnQoY3R4LCBjaGFydEpzb24pO1xufTtcblxud2luZG93LnN1Ym1pdFNlYXJjaEZvcm0gPSBmdW5jdGlvbiAoZmlsZSwgY3VzdG9tUGFyYW1ldGVycykge1xuICAgIHdpbmRvdy5zZWFyY2hGb3JtUGFyYW1ldGVycyA9IHt9O1xuICAgIGZvciAobGV0IGZ1biBvZiB3aW5kb3cuZm9ybUVsZW1lbnRzKSB7XG4gICAgICAgIGNvbnN0IGpzb24gPSBmdW4uY2FsbCh0aGlzKTtcbiAgICAgICAgZm9yIChsZXQga2V5IGluIGpzb24pIHtcbiAgICAgICAgICAgIGxldCB2YWx1ZSA9IGpzb25ba2V5XTtcbiAgICAgICAgICAgIHZhbHVlID0gZW5jb2RlVVJJKHZhbHVlKTtcbiAgICAgICAgICAgIHZhbHVlID0gZW5jb2RlVVJJKHZhbHVlKTtcbiAgICAgICAgICAgIHdpbmRvdy5zZWFyY2hGb3JtUGFyYW1ldGVyc1trZXldID0gdmFsdWU7XG4gICAgICAgIH1cbiAgICB9XG4gICAgY29uc3QgcGFyYW1ldGVycyA9IHdpbmRvdy5idWlsZExvY2F0aW9uU2VhcmNoUGFyYW1ldGVycygnX2knKTtcbiAgICBsZXQgdXJsID0gd2luZG93Ll9zZXJ2ZXIgKyBcIi9wcmV2aWV3L2xvYWREYXRhXCIgKyBwYXJhbWV0ZXJzO1xuICAgIGNvbnN0IHBhZ2VTZWxlY3RvciA9ICQoYCNwYWdlU2VsZWN0b3JgKTtcbiAgICBpZiAocGFnZVNlbGVjdG9yLmxlbmd0aCA+IDApIHtcbiAgICAgICAgdXJsICs9ICcmX2k9MSc7XG4gICAgfVxuICAgICQuYWpheCh7XG4gICAgICAgIHVybCxcbiAgICAgICAgdHlwZTogJ1BPU1QnLFxuICAgICAgICBoZWFkZXJzOiB7XG4gICAgICAgICAgICAnQXV0aG9yaXphdGlvbic6IHRva2VuXG4gICAgICAgIH0sXG4gICAgICAgIHN1Y2Nlc3M6IChyZXBvcnQpID0+IHtcbiAgICAgICAgICAgIHdpbmRvdy5fY3VycmVudFBhZ2VJbmRleCA9IDE7XG4gICAgICAgICAgICBjb25zdCB0YWJsZUNvbnRhaW5lciA9ICQoYCNfdXJlcG9ydF90YWJsZWApO1xuICAgICAgICAgICAgdGFibGVDb250YWluZXIuZW1wdHkoKTtcbiAgICAgICAgICAgIHRhYmxlQ29udGFpbmVyLmFwcGVuZChyZXBvcnQuY29udGVudCk7XG4gICAgICAgICAgICBfYnVpbGRDaGFydERhdGFzKHJlcG9ydC5jaGFydERhdGFzKTtcbiAgICAgICAgICAgIGNvbnN0IHRvdGFsUGFnZSA9IHJlcG9ydC50b3RhbFBhZ2U7XG4gICAgICAgICAgICB3aW5kb3cuX3RvdGFsUGFnZSA9IHRvdGFsUGFnZTtcbiAgICAgICAgICAgIGlmIChwYWdlU2VsZWN0b3IubGVuZ3RoID4gMCkge1xuICAgICAgICAgICAgICAgIHBhZ2VTZWxlY3Rvci5lbXB0eSgpO1xuICAgICAgICAgICAgICAgIGZvciAobGV0IGkgPSAxOyBpIDw9IHRvdGFsUGFnZTsgaSsrKSB7XG4gICAgICAgICAgICAgICAgICAgIHBhZ2VTZWxlY3Rvci5hcHBlbmQoYDxvcHRpb24+JHtpfTwvb3B0aW9uPmApO1xuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICBjb25zdCBwYWdlSW5kZXggPSByZXBvcnQucGFnZUluZGV4IHx8IDE7XG4gICAgICAgICAgICAgICAgcGFnZVNlbGVjdG9yLnZhbChwYWdlSW5kZXgpO1xuICAgICAgICAgICAgICAgICQoJyN0b3RhbFBhZ2VMYWJlbCcpLmh0bWwodG90YWxQYWdlKTtcbiAgICAgICAgICAgICAgICBidWlsZFBhZ2luZyhwYWdlSW5kZXgsIHRvdGFsUGFnZSk7XG4gICAgICAgICAgICB9XG4gICAgICAgIH0sXG4gICAgICAgIGVycm9yOiByZXNwb25zZSA9PiB7XG4gICAgICAgICAgICBpZiAocmVzcG9uc2UgJiYgcmVzcG9uc2UucmVzcG9uc2VUZXh0KSB7XG4gICAgICAgICAgICAgICAgYWxlcnQoXCLmnI3liqHnq6/plJnor6/vvJpcIiArIHJlc3BvbnNlLnJlc3BvbnNlVGV4dCArIFwiXCIpO1xuICAgICAgICAgICAgfSBlbHNlIHtcbiAgICAgICAgICAgICAgICBhbGVydCgn5p+l6K+i5pON5L2c5aSx6LSl77yBJyk7XG4gICAgICAgICAgICB9XG4gICAgICAgIH1cbiAgICB9KTtcbn07Il0sInNvdXJjZVJvb3QiOiIifQ==