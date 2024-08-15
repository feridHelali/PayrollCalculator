import React from 'react';
import { Box, Heading } from '@chakra-ui/react';
import { labels } from '../arabic.labels';
import AffairList from '../components/affair/AffairList';

const AffairPage: React.FC = () => {
  return (
    <Box>
      <Heading>{labels.affairs} </Heading>
      <AffairList />
    </Box>
  );
};

export default AffairPage;
