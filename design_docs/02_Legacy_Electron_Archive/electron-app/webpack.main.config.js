// webpack.main.config.js
const path = require('path');

module.exports = {
  mode: 'development',
  target: 'electron-main',
  entry: {
    main: './src/main/main.ts',
    preload: './src/main/preload.ts'
  },
  output: {
    path: path.resolve(__dirname, 'dist'),
    filename: '[name].js'
  },
  resolve: {
    extensions: ['.ts', '.js'],
  },
  module: {
    rules: [
      {
        test: /\.ts$/,
        use: 'ts-loader',
        exclude: /node_modules/
      },
      {
        test: /\.node$/,
        use: 'node-loader'
      }
    ]
  },
  externals: {
    'better-sqlite3': 'commonjs better-sqlite3'
  },
  node: {
    __dirname: false,
    __filename: false
  }
};
