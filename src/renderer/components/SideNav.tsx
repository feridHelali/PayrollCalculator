import React from 'react';
import { Box, VStack, Link, StepSeparator } from '@chakra-ui/react';
import { Link as RouterLink } from 'react-router-dom';
import { labels } from '../arabic.labels';
import { FaHome, FaBriefcase, FaFileContract, FaPlusSquare } from 'react-icons/fa';
const SideNav: React.FC = () => {
  return (
    <Box w="200px" p={4} bg="gray.800" color="white" h="100vh" fontSize={"xl"}>
      <VStack spacing={4} align="stretch">
        <Link as={RouterLink} to="/" display={"flex"} alignItems={"flex-start"}>
          <FaHome style={{ marginLeft: '10px' }} />{labels.home}
        </Link>
        <hr/>
        <Link as={RouterLink} to="/affairs" display={"flex"} alignItems={"flex-start"}>
          <FaBriefcase style={{ marginLeft: '10px' }} />{labels.affairList}
        </Link>
        <Link as={RouterLink} to="/affair-form" display={"flex"} alignItems={"flex-start"}>
          <FaPlusSquare style={{ marginLeft: '10px' }} /> {labels.addAffair}
        </Link>
        <hr/>
        <Link as={RouterLink} to="/agreements" display={"flex"} alignItems={"flex-start"}>
          <FaFileContract style={{ marginLeft: '10px' }} />{labels.sectorialJointAgreement}
        </Link>
        <Link as={RouterLink} to="/agreement-form" display={"flex"} alignItems={"flex-start"}>
          <FaPlusSquare style={{ marginLeft: '10px' }} /> {labels.addSectorialJointAgreement}
        </Link>
        <hr/>
        
        <Link as={RouterLink} to="/salary-tables" display={"flex"} alignItems={"flex-start"}>
          <FaBriefcase style={{ marginLeft: '10px' }} /> {labels.salaryTableList}
        </Link>
        <Link as={RouterLink} to="/salary-table-form" display={"flex"} alignItems={"flex-start"}>
          <FaPlusSquare style={{ marginLeft: '10px' }} /> {labels.addSalaryTable}
        </Link>

      </VStack>
    </Box>
  );
};

export default SideNav;
