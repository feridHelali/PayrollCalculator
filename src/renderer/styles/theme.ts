import { extendTheme } from '@chakra-ui/react';

const theme = extendTheme({
  direction: 'rtl', // Right-to-left direction for Arabic
  fonts: {
    heading: 'Arial, sans-serif',
    body: 'Arial, sans-serif',
  },
});

export default theme;
