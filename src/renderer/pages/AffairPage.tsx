import React from 'react';
import { Box, Heading } from '@chakra-ui/react';
import { labels } from '../arabic.labels';

const AffairPage: React.FC = () => {
  return (
    <Box>
      <Heading>{labels.affairs} </Heading>
    </Box>
  );
};

export default AffairPage;
