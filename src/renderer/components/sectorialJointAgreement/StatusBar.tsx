// src/components/StatusBar.tsx
import React from 'react';
import { Box, Text } from '@chakra-ui/react';
import { useSelector } from 'react-redux';
import { RootState } from '../../redux/store';

const StatusBar: React.FC = () => {
  const statusMessage = useSelector((state: RootState) => state.status.message);

  return (
    <Box
      position="fixed"
      bottom="0"
      width="100%"
      bg="gray.700"
      color="white"
      textAlign="center"
      p={2}
      zIndex={1}
    >
      <Text>{statusMessage}</Text>
    </Box>
  );
};

export default StatusBar;
