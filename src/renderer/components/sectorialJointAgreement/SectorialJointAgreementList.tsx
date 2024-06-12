
import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
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

const initialAgreement: sectorialJointAgreementProps = {
  sectorialJointAgreementId: 0,
  agreementName: '',
  description: '',
  agreementApplicationPeriods: [],
}

const SectorialJointAgreementList: React.FC = () => {
  console.log(window.electronAPI)
  const dispatch: AppDispatch = useDispatch();
  const agreements = useSelector((state: RootState) => state.agreements.agreements);
  const agreementStatus = useSelector((state: RootState) => state.agreements.status);
  const error = useSelector((state: RootState) => state.agreements.error);

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
    setNewAgreement({ ...agreement, agreementName:  agreement.agreementName, description: agreement.description, agreementApplicationPeriods: [] });
  };

  const handleDelete = (id: number) => {
    dispatch(deleteAgreement(id));
  };

  return (
    <Box p={5}>
      <VStack spacing={4}>
        <FormControl>
          <FormLabel>{labels.sectorialJointAgreementName}</FormLabel>
          <Input
            value={newAgreement.agreementName}
            onChange={(e) => setNewAgreement(prev=>{return {...prev, agreementName: e.target.value}})} />
        </FormControl>
        <HStack spacing={4}>
          <Button onClick={currentAgreement ? handleUpdate : handleCreate}>
            {currentAgreement ? 'Update Agreement' : 'Create Agreement'}
          </Button>
          {currentAgreement && (
            <Button onClick={() => {
              setCurrentAgreement(null);
              setNewAgreement(initialAgreement);
            }}>
              Cancel
            </Button>
          )}
        </HStack>
        {agreementStatus === 'loading' && <Text>Loading...</Text>}
        {error && <Text>Error: {error}</Text>}
        <VStack spacing={2} align="stretch">
          {agreements.map((agreement) => (
            <HStack key={agreement.sectorialJointAgreementId} spacing={4}>
              <Text>{agreement.agreementName}</Text>
              <Button onClick={() => handleEdit(agreement)}>Edit</Button>
              <Button onClick={() => handleDelete(agreement.sectorialJointAgreementId)}>Delete</Button>
            </HStack>
          ))}
        </VStack>
      </VStack>
    </Box>
  );
};

export default SectorialJointAgreementList;
