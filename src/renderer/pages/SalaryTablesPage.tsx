import React from 'react'
import { Box, Heading } from '@chakra-ui/react';
import { labels } from '../arabic.labels';
import SalaryTablesList from '../components/sectorialJointAgreement/SalaryTablesList';

const SalaryTablesPage:React.FC =()=> {
  
  return (
    <Box>
      <Heading>{labels.salaryTableList}</Heading>
      <SalaryTablesList />
    </Box>
  )
}

export default SalaryTablesPage