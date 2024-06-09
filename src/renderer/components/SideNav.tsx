import React from 'react';
import { Box, VStack, Link } from '@chakra-ui/react';
import { Link as RouterLink } from 'react-router-dom';

const SideNav: React.FC = () => {
  return (
    <Box w="200px" p={4} bg="gray.800" color="white" h="100vh">
      <VStack spacing={4} align="stretch">
        <Link as={RouterLink} to="/">Home</Link>
        <Link as={RouterLink} to="/payroll">Payroll</Link>
      </VStack>
    </Box>
  );
};

export default SideNav;
