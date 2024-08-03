import React, { useEffect, useState } from 'react';
import { Box, Button, FormControl, FormLabel, Heading, Input, VStack, HStack, Text } from '@chakra-ui/react';
import { labels } from '../../arabic.labels';
import { sectorialJointAgreementProps } from '../../../types/sectorialAgreementProps';
import { useAppDispatch, useAppSelector } from '../../redux/redux.hooks';
import { AppDispatch, RootState } from '../../redux/store';
import { createAgreement, fetchAgreementById, switchToUpdateMode,switchToCreateMode, updateAgreement } from '../../redux/sectorialJointAgreement/sectorialJointAgreementSlice';
import { useNavigate, useParams } from 'react-router-dom';
import AlfaSpinner from '../../shared/AlfaSpinner';


const initialAgreement: sectorialJointAgreementProps = {
    sectorialJointAgreementId: 0,
    agreementName: '',
    description: '',
    agreementApplicationTables: [],
}
const SectorialJointAgreementForm: React.FC = () => {
    const navigate = useNavigate();
    const { sectorialJointAgreementId } = useParams<{ sectorialJointAgreementId: string }>();
    const dispatch: AppDispatch = useAppDispatch();
    const agreementStatus = useAppSelector((state: RootState) => state.agreements.status);
    const currentAgreement = useAppSelector((state: RootState) => state.agreements.currentAgreement);
    const error = useAppSelector((state: RootState) => state.agreements.error);
    const mode = useAppSelector((state: RootState) => state.agreements.mode);
    const [newAgreement, setNewAgreement] = useState<sectorialJointAgreementProps>(initialAgreement);


    useEffect(() => {
        if (sectorialJointAgreementId) {
            dispatch(switchToUpdateMode())
            dispatch(fetchAgreementById(sectorialJointAgreementId!))

        } else {
            dispatch(switchToCreateMode())
            setNewAgreement(initialAgreement);
        }


    }, [sectorialJointAgreementId, dispatch]);

    useEffect(() => {
        if (currentAgreement) {
            dispatch(switchToUpdateMode())
            setNewAgreement(currentAgreement);
        }
    }, [currentAgreement]);

    const handleCreate = () => {
        if (newAgreement.agreementName.trim() && mode === 'create') {
            dispatch(createAgreement({ agreementName: newAgreement.agreementName, description: newAgreement.description }))
                .then(() => navigate('/agreements'));

        }
    };

    const handleUpdate = () => {
        if (currentAgreement && newAgreement.agreementName.trim()) {
            dispatch(updateAgreement({ ...newAgreement, agreementName: newAgreement.agreementName, description: newAgreement.description }))
                .then(() => navigate('/agreements'));

        }
    };



    return (
        <Box p={5} m={5} bgColor={'gray.100'} borderRadius={5}>
              {agreementStatus === 'loading' && <AlfaSpinner />}
              {error && <Text colorScheme='red'>Error: {error}</Text>}
            <VStack spacing={4}>
                <Heading> {mode === 'update' ? labels.update : labels.create} {labels.sectorialJointAgreement}</Heading>
                <FormControl>
                    {mode === 'update' && <FormLabel>#{currentAgreement?.sectorialJointAgreementId}</FormLabel>}
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
            <HStack spacing={4} mt={4} justifyContent='flex-end' p={4}>
                <Button onClick={ mode === 'update' ? handleUpdate : handleCreate} colorScheme='blue' shadow='md' isDisabled={!isNewAgreementValid(newAgreement)}>
                    {mode === 'update' ? labels.update : labels.save}
                </Button>
               
            </HStack>
          
        </Box >

    );
}

export default SectorialJointAgreementForm;

function isNewAgreementValid(newAgreement: sectorialJointAgreementProps): boolean {
    if(!newAgreement) {
        return false;
    }
    if(!newAgreement.agreementName.trim()) {
        return false;
    }
    return true;    
}


