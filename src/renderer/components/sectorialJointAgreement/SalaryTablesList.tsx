
import React, { useEffect } from 'react';

import {
  Box,
  Button,
  HStack,
  Heading,
  Text
} from '@chakra-ui/react';
import type { RootState, AppDispatch } from '../../redux/store';
import { labels } from '../../arabic.labels';

import { Table, Thead, Tbody, Tr, Th, Td } from '@chakra-ui/react';
import { useAppDispatch, useAppSelector } from '../../redux/redux.hooks';
import { useNavigate } from 'react-router-dom';
import { SalaryTableProps } from '../../../types/salaryTableProps';
import { deleteSalaryTable, fetchAllSalaryTables } from '../../redux/sectorialJointAgreement/salaryTableSlice';
import { fetchAgreements } from '../../redux/sectorialJointAgreement/sectorialJointAgreementSlice';




const SalaryTablesList: React.FC = () => {
  const navigate = useNavigate();
  const dispatch: AppDispatch = useAppDispatch();
  const salaryTables: SalaryTableProps[] = useAppSelector((state: RootState) => state.salaryTables.salaryTables);
  const salaryTablesStatus = useAppSelector((state: RootState) => state.salaryTables.status);
  const error = useAppSelector((state: RootState) => state.salaryTables.error);
  

  
  useEffect(() => {
    if (salaryTablesStatus === 'idle') {
      dispatch(fetchAllSalaryTables());
    }
  }, [salaryTablesStatus, dispatch]);

  const handleEdit = (salaryTableId: any) => {
    navigate(`/salary-table-form/${salaryTableId}`);
  };

  const handleDelete = (salaryTableId: any) => {
    dispatch(deleteSalaryTable(salaryTableId));
  };

  return (
    <Box p={5} m={5} bgColor={'gray.100'} borderRadius={5}>
      <Heading mb={5} alignContent={'center'} fontSize={'2xl'}>{labels.salaryTableList}</Heading>
      <Table variant="striped">
        <Thead>
          <Tr>
            <Th>{labels.sectorialJointAgreement}</Th>
            <Th>{labels.salaryTableNumber}</Th>
            <Th>{labels.salaryType}</Th>
            <Th>{labels.concernedEmployee}</Th>
            <Th>{labels.beginningDateOfApplication}</Th>
            <Th>{labels.endDateOfApplication}</Th>
            <Th>{labels.action}</Th>
          </Tr>
        </Thead>
        <Tbody>
          {salaryTables.map((salaryTable) => (
            <Tr key={salaryTable.salaryTableId}>
              <Td>{salaryTable.agreement?.agreementName}</Td>
              <Td>{salaryTable.numeroTable}</Td>
              <Td>{salaryTable.type}</Td>
              <Td>{salaryTable.consernedEmployee}</Td>
              <Td>{new Date(salaryTable.beginningDateOfApplication).toLocaleDateString()}</Td>
              <Td>{new Date(salaryTable.endDateOfApplication ? salaryTable.endDateOfApplication : new Date('0000-00-00'))?.toLocaleDateString()}</Td>
              <Td>
                <HStack spacing={4}>
                  <Button onClick={() => handleEdit(salaryTable.salaryTableId)} colorScheme='teal' shadow={'md'}>{labels.update}</Button>
                  <Button onClick={() => handleDelete(salaryTable.salaryTableId)} colorScheme='red' shadow={'md'}>{labels.delete}</Button>
                </HStack>
              </Td>
            </Tr>
          ))}
        </Tbody>
      </Table>
      {error && <Text colorScheme='red'>Error: {error}</Text>}
    </Box>
  );
};

export default SalaryTablesList;
