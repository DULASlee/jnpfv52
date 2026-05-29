'use strict';

import _CODE from './CODE39/'

import _CODE2 from './CODE128/'

import _EAN_UPC from './EAN_UPC/'

import _ITF from './ITF/'

import _MSI from './MSI/'

import _pharmacode from './pharmacode/'

import _codabar from './codabar'
export default {
	CODE128: _CODE2.CODE128,
	CODE128A: _CODE2.CODE128A,
	CODE128B: _CODE2.CODE128B,
	CODE128C: _CODE2.CODE128C,
	EAN13: _EAN_UPC.EAN13,
	EAN8: _EAN_UPC.EAN8,
	EAN5: _EAN_UPC.EAN5,
	EAN2: _EAN_UPC.EAN2,
	UPC: _EAN_UPC.UPCE,
	UPCE: _EAN_UPC.UPCE,
	ITF14: _ITF.ITF14,
	ITF: _ITF.ITF,
	MSI: _MSI.MSI,
	MSI10: _MSI.MSI10,
	MSI11: _MSI.MSI11,
	MSI1010: _MSI.MSI1010,
	MSI1110: _MSI.MSI1110,
	PHARMACODE: _pharmacode,
	CODABAR: _codabar,
	CODE39: _CODE,
}