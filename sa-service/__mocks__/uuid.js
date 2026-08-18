let counter = 0; module.exports = { v4: () => 'test-uuid-' + (++counter).toString().padStart(4, '0') };
