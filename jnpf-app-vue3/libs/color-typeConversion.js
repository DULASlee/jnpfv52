const conversion = {
	colorList: [{
		r: 244,
		g: 67,
		b: 54,
		a: 1
	}, {
		r: 233,
		g: 30,
		b: 99,
		a: 1
	}, {
		r: 156,
		g: 39,
		b: 176,
		a: 1
	}, {
		r: 103,
		g: 58,
		b: 183,
		a: 1
	}, {
		r: 63,
		g: 81,
		b: 181,
		a: 1
	}, {
		r: 33,
		g: 150,
		b: 243,
		a: 1
	}, {
		r: 3,
		g: 169,
		b: 244,
		a: 1
	}, {
		r: 0,
		g: 188,
		b: 212,
		a: 1
	}, {
		r: 0,
		g: 150,
		b: 136,
		a: 1
	}, {
		r: 76,
		g: 175,
		b: 80,
		a: 1
	}, {
		r: 139,
		g: 195,
		b: 74,
		a: 1
	}, {
		r: 205,
		g: 220,
		b: 57,
		a: 1
	}, {
		r: 255,
		g: 235,
		b: 59,
		a: 1
	}, {
		r: 255,
		g: 193,
		b: 7,
		a: 1
	}, {
		r: 255,
		g: 152,
		b: 0,
		a: 1
	}, {
		r: 255,
		g: 87,
		b: 34,
		a: 1
	}, {
		r: 121,
		g: 85,
		b: 72,
		a: 1
	}, {
		r: 158,
		g: 158,
		b: 158,
		a: 1
	}, {
		r: 0,
		g: 0,
		b: 0,
		a: 0.5
	}, {
		r: 0,
		g: 0,
		b: 0,
		a: 0
	}, ],
	hsv2rgb(h, s, v) {
		h = this.bound01(h, 360) * 6;
		s = this.bound01(s, 100);
		v = this.bound01(v, 100);
		const i = Math.floor(h);
		const f = h - i;
		const p = v * (1 - s);
		const q = v * (1 - f * s);
		const t = v * (1 - (1 - f) * s);
		const mod = i % 6;
		const r = [v, q, p, p, t, v][mod];
		const g = [t, v, v, q, p, p][mod];
		const b = [p, p, t, v, v, q][mod];
		return {
			r: Math.round(r * 255),
			g: Math.round(g * 255),
			b: Math.round(b * 255),
			a: 1
		};
	},
	bound01(value, max) {
		if (this.isOnePointZero(value)) value = '100%';
		const processPercent = this.isPercentage(value);
		value = Math.min(max, Math.max(0, parseFloat(value)));
		if (processPercent) {
			value = parseInt(value * max, 10) / 100;
		}
		if ((Math.abs(value - max) < 0.000001)) {
			return 1;
		}
		return (value % max) / parseFloat(max);
	},
	isPercentage(n) {
		return typeof n === 'string' && n.indexOf('%') !== -1;
	},
	isOnePointZero(n) {
		return typeof n === 'string' && n.indexOf('.') !== -1 && parseFloat(n) === 1;
	},
	rgb2hsl(r, g, b) {
		r = r / 255;
		g = g / 255;
		b = b / 255;
		var min = Math.min(r, g, b);
		var max = Math.max(r, g, b);
		var l = (min + max) / 2;
		var difference = max - min;
		var h, s, l;
		if (max == min) {
			h = 0;
			s = 0;
		} else {
			s = l > 0.5 ? difference / (2.0 - max - min) : difference / (max + min);
			switch (max) {
				case r:
					h = (g - b) / difference + (g < b ? 6 : 0);
					break;
				case g:
					h = 2.0 + (b - r) / difference;
					break;
				case b:
					h = 4.0 + (r - g) / difference;
					break;
			}
			h = Math.round(h * 60);
		}
		s = Math.round(s * 100); //转换成百分比的形式
		l = Math.round(l * 100);

		return {
			h: h,
			s: s + '%',
			l: l + '%'
		};
	},
	rgb2hsv(r, g, b) {
		r = this.bound01(r, 255);
		g = this.bound01(g, 255);
		b = this.bound01(b, 255);
		const max = Math.max(r, g, b);
		const min = Math.min(r, g, b);
		let h, s;
		let v = max;
		const d = max - min;
		s = max === 0 ? 0 : d / max;
		if (max === min) {
			h = 0; // achromatic
		} else {
			switch (max) {
				case r:
					h = (g - b) / d + (g < b ? 6 : 0);
					break;
				case g:
					h = (b - r) / d + 2;
					break;
				case b:
					h = (r - g) / d + 4;
					break;
			}
			h /= 6;
		}
		return {
			h: parseInt(h * 360),
			s: parseInt(s * 100),
			v: parseInt(v * 100)
		};
	},
	/**
	 * rgb 转 二进制 hex
	 * @param {Object} rgb
	 */
	rgbToHex(rgb) {
		let hex = [rgb.r.toString(16), rgb.g.toString(16), rgb.b.toString(16)];
		hex.map(function(str, i) {
			if (str.length == 1) {
				hex[i] = '0' + str;
			}
		});
		return hex.join('');
	},
	hex2rgba(color) {
		var reg = /^#([0-9a-fA-f]{3}|[0-9a-fA-f]{6})$/;
		var sColor = color.toLowerCase();
		let rgbaObj = {}
		let rgba = 'rgba'
		let k = ['r', 'g', 'b']
		if (sColor && reg.test(sColor)) {
			if (sColor.length === 4) {
				var sColorNew = "#";
				for (var i = 1; i < 4; i += 1) {
					sColorNew += sColor.slice(i, i + 1).concat(sColor.slice(i, i + 1));
				}
				sColor = sColorNew;
			}
			//处理六位的颜色值
			var sColorChange = [];
			for (var i = 1; i < 7; i += 2) {
				sColorChange.push(parseInt("0x" + sColor.slice(i, i + 2)));
			}
			sColorChange.push(1)
			sColorChange.forEach((o, i) => {
				if (k[i]) {
					rgbaObj[k[i]] = o
				}
			})
			return rgbaObj
		} else {
			return sColor;
		}
	},
	hsv2rgb(h, s, v) {
		h = this.bound01(h, 360) * 6;
		s = this.bound01(s, 100);
		v = this.bound01(v, 100);

		const i = Math.floor(h);
		const f = h - i;
		const p = v * (1 - s);
		const q = v * (1 - f * s);
		const t = v * (1 - (1 - f) * s);
		const mod = i % 6;
		const r = [v, q, p, p, t, v][mod];
		const g = [t, v, v, q, p, p][mod];
		const b = [p, p, t, v, v, q][mod];

		return {
			r: Math.round(r * 255),
			g: Math.round(g * 255),
			b: Math.round(b * 255),
			a: 1
		};
	},
	hsl2rgb(h, s, l) {
		h = h / 360;
		s = s / 100;
		l = l / 100;
		let rgb = [];
		let rgbList = ['r', 'g', 'b'];
		let rgbObj = {
			r: 0,
			g: 0,
			b: 0,
			a: 1
		}
		if (s == 0) {
			rgb = [Math.round(l * 255), Math.round(l * 255), Math.round(l * 255)];
		} else {
			var q = l >= 0.5 ? (l + s - l * s) : (l * (1 + s));
			var p = 2 * l - q;
			var tr = rgb[0] = h + 1 / 3;
			var tg = rgb[1] = h;
			var tb = rgb[2] = h - 1 / 3;
			for (var i = 0; i < rgb.length; i++) {
				var tc = rgb[i];
				if (tc < 0) {
					tc = tc + 1;
				} else if (tc > 1) {
					tc = tc - 1;
				}
				switch (true) {
					case (tc < (1 / 6)):
						tc = p + (q - p) * 6 * tc;
						break;
					case ((1 / 6) <= tc && tc < 0.5):
						tc = q;
						break;
					case (0.5 <= tc && tc < (2 / 3)):
						tc = p + (q - p) * (4 - 6 * tc);
						break;
					default:
						tc = p;
						break;
				}
				rgb[i] = Math.round(tc * 255);
				rgbObj[rgbList[i]] = rgb[i]
			}
		}
		return rgbObj;
	},
	rgbToHsb(rgb) {
		let hsb = {
			h: 0,
			s: 0,
			b: 0
		};
		let min = Math.min(rgb.r, rgb.g, rgb.b);
		let max = Math.max(rgb.r, rgb.g, rgb.b);
		let delta = max - min;
		hsb.b = max;
		hsb.s = max != 0 ? 255 * delta / max : 0;
		if (hsb.s != 0) {
			if (rgb.r == max) hsb.h = (rgb.g - rgb.b) / delta;
			else if (rgb.g == max) hsb.h = 2 + (rgb.b - rgb.r) / delta;
			else hsb.h = 4 + (rgb.r - rgb.g) / delta;
		} else hsb.h = -1;
		hsb.h *= 60;
		if (hsb.h < 0) hsb.h = 0;
		hsb.s *= 100 / 255;
		hsb.b *= 100 / 255;
		return hsb;
	},
	/**
	 * hsb 转 rgb
	 * @param {Object} 颜色模式  H(hues)表示色相，S(saturation)表示饱和度，B（brightness）表示亮度
	 */
	HSBToRGB(hsb) {
		let rgb = {};
		let h = Math.round(hsb.h);
		let s = Math.round((hsb.s * 255) / 100);
		let v = Math.round((hsb.b * 255) / 100);
		if (s == 0) {
			rgb.r = rgb.g = rgb.b = v;
		} else {
			let t1 = v;
			let t2 = ((255 - s) * v) / 255;
			let t3 = ((t1 - t2) * (h % 60)) / 60;
			if (h == 360) h = 0;
			if (h < 60) {
				rgb.r = t1;
				rgb.b = t2;
				rgb.g = t2 + t3;
			} else if (h < 120) {
				rgb.g = t1;
				rgb.b = t2;
				rgb.r = t1 - t3;
			} else if (h < 180) {
				rgb.g = t1;
				rgb.r = t2;
				rgb.b = t2 + t3;
			} else if (h < 240) {
				rgb.b = t1;
				rgb.r = t2;
				rgb.g = t1 - t3;
			} else if (h < 300) {
				rgb.b = t1;
				rgb.g = t2;
				rgb.r = t2 + t3;
			} else if (h < 360) {
				rgb.r = t1;
				rgb.g = t2;
				rgb.b = t1 - t3;
			} else {
				rgb.r = 0;
				rgb.g = 0;
				rgb.b = 0;
			}
		}
		return {
			r: Math.round(rgb.r),
			g: Math.round(rgb.g),
			b: Math.round(rgb.b)
		};
	},
}
export default conversion
