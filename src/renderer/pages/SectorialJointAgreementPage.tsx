import React from 'react'
import { Box, Heading } from '@chakra-ui/react';
import { labels } from '../arabic.labels';
import SectorialAgreementList from '../components/sectorialJointAgreement/SectorialJointAgreementList';

const SectorialJointAgreementPage:React.FC =()=> {
  
  return (
    <Box>
      <Heading>{labels.sectorialJointAgreement}</Heading>
      <SectorialAgreementList />
    </Box>
  )
}

export default SectorialJointAgreementPage