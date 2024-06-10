import React from 'react'
import { Box, Heading } from '@chakra-ui/react';
import { labels } from '../arabic.labels';
import ConventionCollectiveComponent from '../components/conventionCollective/ConventionCollectiveComponent';

const ConventionCollectivePage:React.FC =()=> {
  
  return (
    <Box>
      <Heading>{labels.conventionCollective}</Heading>
      <ConventionCollectiveComponent />
    </Box>
  )
}

export default ConventionCollectivePage