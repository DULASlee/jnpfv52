/** Minimal resolve config so dependency-cruiser understands Vite aliases. */
const path = require('path');

module.exports = {
  resolve: {
    alias: {
      '/@': path.resolve(__dirname, 'src'),
      '/#': path.resolve(__dirname, 'types'),
    },
    extensions: ['.ts', '.tsx', '.vue', '.js', '.jsx', '.json', '.mjs', '.cjs'],
  },
};
