/**
 * Webpack 4 — 开发环境配置
 * 优化: eval-cheap-module-source-map 替代 inline-source-map（快 3-5×）
 * 优化: url-loader limit 从 10MB 降到 8KB（避免 JS 膨胀）
 */
const path = require('path');
module.exports = {
    mode: 'development',
    entry: {
        designer: './src/index.js',
        searchform: './src/form/index.js',
        preview: './src/preview.js'
    },
    output: {
        path: path.resolve('html/js'),
        filename: '[name].bundle.js'
    },
    optimization: {
        splitChunks: {
            cacheGroups: {
                vendor: {
                    test: /[\\/]node_modules[\\/](handsontable|codemirror|chart\.js|react|react-dom|jquery|bootstrap|raphael)[\\/]/,
                    chunks: 'initial',
                    name: 'common',
                    priority: 10
                }
            }
        }
    },
    module: {
        rules: [{
                test: /\.js$/,
                exclude: /node_modules/,
                loader: "babel-loader"
            },
            {
                test: /\.css$/,
                use: [{ loader: 'style-loader' }, { loader: 'css-loader' }]
            },
            {
                test: /\.(eot|woff|woff2|ttf|svg|png|jpg)$/,
                use: [{
                    loader: 'url-loader',
                    options: { limit: 8192 } // 8KB，超过则走 file-loader 输出独立文件
                }]
            }
        ]
    },
    devtool: 'eval-cheap-module-source-map'
};