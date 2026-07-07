/**
 * Webpack 4 — 生产环境配置 (jnpf-web-datareport)
 *
 * 用法: npm run build (或 webpack --config webpack.config.prod.js)
 * 输出: dist/ (designer.bundle.js + searchform.bundle.js + preview.bundle.js + common.bundle.js)
 *
 * 优化要点:
 *   - mode: production → 自动 Terser 压缩 + tree-shaking
 *   - sourcemap: false (生产不暴露源码)
 *   - splitChunks: React/Handsontable/Codemirror/Chart.js/Bootstrap 独立 vendor chunk
 *   - url-loader limit: 8KB (小资源 inline，大资源独立 file)
 *   - performance hints: 500KB 警告阈值
 */
const path = require('path');
const TerserPlugin = require('terser-webpack-plugin');

module.exports = {
    mode: 'production',
    entry: {
        designer: './src/index.js',
        searchform: './src/form/index.js',
        preview: './src/preview.js'
    },
    output: {
        path: path.resolve('dist'),
        filename: '[name].[contenthash:8].bundle.js',
        publicPath: './'
    },
    optimization: {
        minimizer: [
            new TerserPlugin({
                terserOptions: {
                    compress: {
                        drop_console: true,
                        drop_debugger: true,
                    },
                    output: { comments: false },
                },
                // 多线程压缩（Webpack 4 的 TerserPlugin 默认开启）
                parallel: true,
            }),
        ],
        splitChunks: {
            cacheGroups: {
                vendor: {
                    test: /[\\/]node_modules[\\/](handsontable|codemirror|chart\.js|react|react-dom|jquery|bootstrap|raphael)[\\/]/,
                    chunks: 'initial',
                    name: 'common',
                    priority: 10,
                },
            },
        },
    },
    module: {
        rules: [
            {
                test: /\.js$/,
                exclude: /node_modules/,
                loader: 'babel-loader',
            },
            {
                test: /\.css$/,
                use: [{ loader: 'style-loader' }, { loader: 'css-loader' }],
            },
            {
                test: /\.(eot|woff|woff2|ttf|svg|png|jpg|gif)$/,
                use: [
                    {
                        loader: 'url-loader',
                        options: {
                            limit: 8192,
                            name: 'assets/[name].[hash:8].[ext]',
                        },
                    },
                ],
            },
        ],
    },
    performance: {
        hints: 'warning',
        maxEntrypointSize: 512000,
        maxAssetSize: 512000,
    },
    devtool: false,
};
