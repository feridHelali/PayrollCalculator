
import React, { useEffect } from 'react';
import {
  fetchAgreements,
  deleteAgreement,
} from '../../redux/sectorialJointAgreement/sectorialJointAgreementSlice';
import {
  Box,
  Button,
  HStack,
  Text
} from '@chakra-ui/react';
import type { RootState, AppDispatch } from '../../redux/store';
import { sectorialJointAgreementProps } from '../../../types/sectorialAgreementProps';
import { labels } from '../../arabic.labels';

import { Table, Thead, Tbody, Tr, Th, Td } from '@chakra-ui/react';
import { useAppDispatch,useAppSelector } from '../../redux/redux.hooks';
import { useNavigate } from 'react-router-dom';



const SectorialJointAgreementList: React.FC = () => {
  const navigate = useNavigate();
  const dispatch: AppDispatch = useAppDispatch();
  const agreements: sectorialJointAgreementProps[] = useAppSelector((state: RootState) => state.agreements.agreements);
  const agreementStatus = useAppSelector((state: RootState) => state.agreements.status);
  const error = useAppSelector((state: RootState) => state.agreements.error);


  useEffect(() => {
    if (agreementStatus === 'idle') {
      dispatch(fetchAgreements());
    }
  }, [agreementStatus, dispatch]);

  const handleEdit = (sectorialJointAgreementId: any) => {
    navigate(`/agreement-form/${sectorialJointAgreementId}`);
};

  const handleDelete = (id: any) => {
    dispatch(deleteAgreement(id));
  };

  return (
    <Box p={5}>
          <Table variant="simple">
            <Thead>
              <Tr>
                <Th>{labels.sectorialJointAgreementName}</Th>
                <Th>{labels.descriptionSectorialJointAgreement}</Th>
                <Th>{labels.action}</Th>
              </Tr>
            </Thead>
            <Tbody>
              {agreements.map((agreement) => (
                <Tr key={agreement.sectorialJointAgreementId}>
                  <Td>{agreement.agreementName}</Td>
                  <Td>{agreement.description}</Td>
                  <Td>
                    <HStack spacing={4}>
                      <Button onClick={() => handleEdit(agreement.sectorialJointAgreementId)}>{labels.update}</Button>
                      <Button onClick={() => handleDelete(agreement.sectorialJointAgreementId)}>{labels.delete}</Button>
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

export default SectorialJointAgreementList;
