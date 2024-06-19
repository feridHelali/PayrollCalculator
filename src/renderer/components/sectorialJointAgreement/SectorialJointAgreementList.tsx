
import React, { useEffect, useState } from 'react';
import {
  fetchAgreements,
  createAgreement,
  updateAgreement,
  deleteAgreement,
} from '../../redux/sectorialJointAgreement/sectorialJointAgreementSlice';
import {
  Box,
  Button,
  FormControl,
  FormLabel,
  Input,
  VStack,
  HStack,
  Text,
} from '@chakra-ui/react';
import type { RootState, AppDispatch } from '../../redux/store';
import { sectorialJointAgreementProps } from '../../../types/sectorialAgreementProps';
import { labels } from '../../arabic.labels';
import AlfaSpinner from '../../shared/AlfaSpinner';
import { Table, Thead, Tbody, Tr, Th, Td } from '@chakra-ui/react';
import { useAppDispatch,useAppSelector } from '../../redux/redux.hooks';

const initialAgreement: sectorialJointAgreementProps = {
  sectorialJointAgreementId: 0,
  agreementName: '',
  description: '',
  agreementApplicationPeriods: [],
}

const SectorialJointAgreementList: React.FC = () => {
  const dispatch: AppDispatch = useAppDispatch();
  const agreements = useAppSelector((state: RootState) => state.agreements.agreements);
  const agreementStatus = useAppSelector((state: RootState) => state.agreements.status);
  const error = useAppSelector((state: RootState) => state.agreements.error);

  const [newAgreement, setNewAgreement] = useState<sectorialJointAgreementProps>(initialAgreement);
  const [currentAgreement, setCurrentAgreement] = useState<sectorialJointAgreementProps | null>(null);

  useEffect(() => {
    if (agreementStatus === 'idle') {
      dispatch(fetchAgreements());
    }
  }, [agreementStatus, dispatch]);

  const handleCreate = () => {
    if (newAgreement.agreementName.trim()) {
      dispatch(createAgreement({ sectorialJointAgreementId: 0, agreementName: newAgreement.agreementName, description: newAgreement.description, agreementApplicationPeriods: [] }));
      setNewAgreement(initialAgreement);
    }
  };

  const handleUpdate = () => {
    if (currentAgreement && newAgreement.agreementName.trim()) {
      dispatch(updateAgreement({ ...currentAgreement, agreementName: newAgreement.agreementName, description: newAgreement.description, agreementApplicationPeriods: [] }));
      setCurrentAgreement(null);
      setNewAgreement(initialAgreement);
    }
  };

  const handleEdit = (agreement: sectorialJointAgreementProps) => {
    setCurrentAgreement(agreement);
    setNewAgreement({ ...agreement, agreementName: agreement.agreementName, description: agreement.description, agreementApplicationPeriods: [] });
  };

  const handleDelete = (id: number) => {
    dispatch(deleteAgreement(id));
  };

  return (
    <Box p={5}>
      <VStack spacing={4}>
        <VStack spacing={4} align="stretch" gap={2}>
          <FormControl>
            <FormLabel>{labels.sectorialJointAgreementName}</FormLabel>
            <Input
              value={newAgreement.agreementName}
              onChange={(e) => setNewAgreement(prev => { return { ...prev, agreementName: e.target.value } })} />
          </FormControl>
          <FormControl>
            <FormLabel>{labels.descriptionSectorialJointAgreement}</FormLabel>
            <Input
              value={newAgreement.description}
              onChange={(e) => setNewAgreement(prev => { return { ...prev, description: e.target.value } })} />
          </FormControl>
        </VStack>
        <HStack spacing={4}>
          <Button onClick={currentAgreement ? handleUpdate : handleCreate}>
            {currentAgreement ? 'Update Agreement' : 'Create Agreement'}
          </Button>
          {currentAgreement && (
            <Button onClick={() => {
              setCurrentAgreement(null);
              setNewAgreement(initialAgreement);
            }}>
              {labels.cancel}
            </Button>
          )}
        </HStack>
        {agreementStatus === 'loading' && <AlfaSpinner />}
        {error && <Text>Error: {error}</Text>}
        <VStack spacing={2} align="stretch">
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
                      <Button onClick={() => handleEdit(agreement)}>{labels.update}</Button>
                      <Button onClick={() => handleDelete(agreement.sectorialJointAgreementId)}>{labels.delete}</Button>
                    </HStack>
                  </Td>
                </Tr>
              ))}
            </Tbody>
          </Table>
        </VStack>
      </VStack>
    </Box>
  );
};

export default SectorialJointAgreementList;
