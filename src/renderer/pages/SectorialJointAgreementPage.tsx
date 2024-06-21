import React from 'react'
import { Box, Heading } from '@chakra-ui/react';
import { labels } from '../arabic.labels';
import ConventionCollectiveComponent from '../components/sectorialJointAgreement/SectorialJointAgreementList';

const ConventionCollectivePage:React.FC =()=> {
  
  return (
    <Box>
      <Heading>{labels.sectorialJointAgreement}</Heading>
      <ConventionCollectiveComponent />
    </Box>
  )
}

export default ConventionCollectivePage