import React from 'react';
import * as ReactDOM from 'react-dom';
import { ChakraProvider } from '@chakra-ui/react';
import App from './App';
import theme from './styles/theme';
import { Provider } from 'react-redux';
import store from './redux/store';


ReactDOM.render(
  <Provider store={store}>
    <ChakraProvider theme={theme}>
      <App />
    </ChakraProvider>
  </Provider>,
  document.getElementById('root')
);
