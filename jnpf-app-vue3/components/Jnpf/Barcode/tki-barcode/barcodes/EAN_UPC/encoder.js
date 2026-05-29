'use strict';

import _constants from './constants'

// Encode data string
var encode = function encode(data, structure, separator) {
	var encoded = data.split('').map(function(val, idx) {
		return _constants.BINARIES[structure[idx]];
	}).map(function(val, idx) {
		return val ? val[data[idx]] : '';
	});

	if (separator) {
		var last = data.length - 1;
		encoded = encoded.map(function(val, idx) {
			return idx < last ? val + separator : val;
		});
	}

	return encoded.join('');
};

export default encode