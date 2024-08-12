//webpack.renderer.config.js

const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');

module.exports = {
  mode: 'development',
  entry: './src/renderer/index.tsx',
  target: 'web',
  module: {
    rules: [
      {
        test: /\.(js|ts|tsx)$/,
        include: /src/,
        use: 'babel-loader'
      },
      {
        test: /\.html$/,
        use: 'html-loader'
      },
      {
        test: /\.css$/,
        use: ['style-loader', 'css-loader']
      },
      {
        test: /\.scss$/,
        use: [
          'style-loader',
          {
            loader: 'css-loader',
            options: {
              modules: true, // Enables CSS modules
            },
          },
          'sass-loader',
        ],
        include: /\.scss$/, // Only apply to files ending with .module.scss
      }
    ]
  },
  resolve: {
    extensions: ['.js', '.ts', '.tsx', '.css', '.scss'],
  },
  output: {
    path: path.resolve(__dirname, 'dist'),
    filename: 'renderer.js'
  },
  plugins: [
    new HtmlWebpackPlugin({
      template: './src/renderer/index.html'
    })
  ],
  devServer: {
    static: {
      directory: path.join(__dirname, 'dist')
    },
    compress: true,
    port: 9000,
  }
};
