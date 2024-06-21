import React, { useEffect, useState } from 'react';
import { Box, Button, FormControl, FormLabel, Heading, Input, VStack, HStack, Text } from '@chakra-ui/react';
import { labels } from '../../arabic.labels';
import { sectorialJointAgreementProps } from '../../../types/sectorialAgreementProps';
import { useAppDispatch, useAppSelector } from '../../redux/redux.hooks';
import { AppDispatch, RootState } from '../../redux/store';
import { createAgreement, fetchAgreementById, updateAgreement } from '../../redux/sectorialJointAgreement/sectorialJointAgreementSlice';
import { useNavigate, useParams } from 'react-router-dom';
import AlfaSpinner from '../../shared/AlfaSpinner';
import SalaryTableForm from './SalaryTableForm';


const initialAgreement: sectorialJointAgreementProps = {
    sectorialJointAgreementId: '',
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
    const mode =useAppSelector((state: RootState) => state.agreements.mode);
    const [newAgreement, setNewAgreement] = useState<sectorialJointAgreementProps>(initialAgreement);


    useEffect(() => {
        if (sectorialJointAgreementId) {
            dispatch(fetchAgreementById(sectorialJointAgreementId!))
            
        } else {
            setNewAgreement(initialAgreement);
        }


    }, [sectorialJointAgreementId, dispatch]);

    useEffect(() => {
        if (currentAgreement) {
            setNewAgreement(currentAgreement);
        }
    }, [currentAgreement]);

    const handleCreate = () => {
        if (newAgreement.agreementName.trim()) {
            dispatch(createAgreement({ sectorialJointAgreementId: '', agreementName: newAgreement.agreementName, description: newAgreement.description, agreementApplicationTables: [] }))
                .then(() => navigate('/agreements'));

        }
    };

    const handleUpdate = () => {
        if (currentAgreement && newAgreement.agreementName.trim()) {
            dispatch(updateAgreement({ ...newAgreement, agreementName: newAgreement.agreementName, description: newAgreement.description}))
            .then(() => navigate('/agreements'));
            
        }
    };



    return (
        <Box>
            {/* <pre><code>{JSON.stringify(newAgreement, null, 2)}</code></pre>
            <pre><code>{JSON.stringify(currentAgreement, null, 2)}</code></pre> */}
            <VStack spacing={4}>
                <Heading>{labels.sectorialJointAgreement}</Heading>
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
                    {currentAgreement ? labels.update : labels.create}
                </Button>
                {currentAgreement && (
                    <Button onClick={() => {
                        setNewAgreement(initialAgreement);
                    }}>
                        {labels.cancel}
                    </Button>
                )}
            </HStack>
            {agreementStatus === 'loading' && <AlfaSpinner />}
            {error && <Text colorScheme='red'>Error: {error}</Text>}

           <SalaryTableForm sectorialJointAgreementId={sectorialJointAgreementId}/>        
        </Box >

    );
}

export default SectorialJointAgreementForm;