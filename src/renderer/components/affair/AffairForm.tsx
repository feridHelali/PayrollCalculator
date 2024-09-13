//src\renderer\components\affair\AffairForm.tsx
import React, { useEffect, useState } from 'react';
import { Box, Button, FormControl, FormLabel, Heading, Input, VStack, HStack, Text } from '@chakra-ui/react';
import { labels } from '../../arabic.labels';
import { useAppDispatch, useAppSelector } from '../../redux/redux.hooks';
import { AppDispatch, RootState } from '../../redux/store';
import { createAffair, fetchAffairById, switchToUpdateMode, switchToCreateMode, updateAffair } from '../../redux/affair/affairSlice';
import { useNavigate, useParams } from 'react-router-dom';
import AlfaSpinner from '../../shared/AlfaSpinner';

interface affairState {
    affairId?: number;
    affairNumber: string;
    title: string;
    claimant: string;
    startDateOfWork: string;
    endDateOfWork: string;
    professionalCategoryAtBegining: string;
    professionalDegreeAtBegining: string;
    sectorialJointAgreement: { sectorialJointAgreementId: string, name: string };
    mode: 'create' | 'update';
}

const initialAffair: affairState = {
    affairId: -1,
    affairNumber: '',
    title: '',
    claimant: '',
    startDateOfWork: '',
    endDateOfWork: '',
    professionalCategoryAtBegining: '',
    professionalDegreeAtBegining: '',
    sectorialJointAgreement: { sectorialJointAgreementId: '', name: '' },
    mode: 'create',
};

const AffairForm: React.FC = () => {
    const navigate = useNavigate();
    const { affairId } = useParams<{ affairId: string }>();
    const dispatch: AppDispatch = useAppDispatch();
    const affairStatus = useAppSelector((state: RootState) => state.affairs.status);
    const currentAffair = useAppSelector((state: RootState) => state.affairs.currentAffair);
    const error = useAppSelector((state: RootState) => state.affairs.error);
    const mode = useAppSelector((state: RootState) => state.affairs.mode);
    const [newAffair, setNewAffair] = useState<affairState>(initialAffair);

    useEffect(() => {
        if (affairId) {
            dispatch(switchToUpdateMode());
            dispatch(fetchAffairById(affairId));
        } else {
            dispatch(switchToCreateMode());
            setNewAffair(initialAffair);
        }
    }, [affairId, dispatch]);

    useEffect(() => {
        if (currentAffair) {
            dispatch(switchToUpdateMode());
            setNewAffair(currentAffair);
        }
    }, [currentAffair, dispatch]);

    useEffect(() => {
        if (currentAffair && mode === 'update') {
            setNewAffair({
                ...currentAffair,
                startDateOfWork: new Date(currentAffair.startDateOfWork),
                endDateDateOfWork: new Date(currentAffair.endDateDateOfWork)
            })
        }
    }, [currentAffair, mode]);

    const handleCreate = () => {
        const {affairId,...affaireData}=newAffair;
        if (isAffairValid(newAffair) && mode === 'create') {
            dispatch(createAffair(affaireData))
                .then(() => navigate('/affairs'));
        }
    };

    const handleUpdate = () => {
        if (currentAffair && isAffairValid(newAffair)) {
            dispatch(updateAffair(newAffair))
                .then(() => navigate('/affairs'));
        }
    };

    return (
        <Box p={5} m={5} bgColor={'gray.100'} borderRadius={5}>
            {affairStatus === 'loading' && <AlfaSpinner />}
            {error && <Text colorScheme='red'>Error: {error}</Text>}
            <VStack spacing={4}>
                <Heading>{mode === 'update' ? labels.update : labels.create} {labels.affair}</Heading>
                <FormControl>
                    {mode === 'update' && <FormLabel>#{currentAffair?.affairId}</FormLabel>}
                    <FormLabel>{labels.affairTitle}</FormLabel>
                    <Input
                        value={newAffair.title}
                        onChange={(e) => setNewAffair((prev) => ({ ...prev, title: e.target.value }))}
                    />
                </FormControl>
                <FormControl>
                    <FormLabel>{labels.affairNumber}</FormLabel>
                    <Input
                        type="text"
                        value={newAffair.affairNumber}
                        onChange={(e) => setNewAffair((prev) => ({ ...prev, affairNumber: e.target.value }))}
                        required
                        placeholder={labels.affairNumber}
                    />
                </FormControl>

                <FormControl>
                    <FormLabel>{labels.claimant}</FormLabel>
                    <Input
                        value={newAffair.claimant}
                        onChange={(e) => setNewAffair((prev) => ({ ...prev, claimant: e.target.value }))}
                    />
                </FormControl>
                <FormControl>
                    <FormLabel>{labels.sectorialJointAgreementId}</FormLabel>
                    <Input
                        type="number"
                        value={newAffair.sectorialJointAgreement.sectorialJointAgreementId}
                        onChange={(e) => setNewAffair((prev) => (
                            {
                                ...prev,
                                sectorialJointAgreement: {
                                    ...prev.sectorialJointAgreement,
                                    sectorialJointAgreementId: e.target.value
                                }
                            }
                        ))}
                    />
                </FormControl>
                <FormControl>
                    <FormLabel>{labels.startDateOfWork}</FormLabel>
                    <Input
                        type="date"
                        value={newAffair.startDateOfWork}
                        onChange={(e) => setNewAffair((prev) => ({ ...prev, startDateOfWork: e.target.value }))}
                    />
                </FormControl>
                <FormControl>
                    <FormLabel>{labels.endDateOfWork}</FormLabel>
                    <Input
                        type="date"
                        value={newAffair.endDateOfWork}
                        onChange={(e) => setNewAffair((prev) => ({ ...prev, endDateOfWork: e.target.value }))}
                    />
                </FormControl>
                <FormControl>
                    <FormLabel>{labels.professionalCategoryAtBegining}</FormLabel>
                    <Input

                        value={newAffair.professionalCategoryAtBegining}
                        onChange={(e) => setNewAffair((prev) => ({ ...prev, professionalCategoryAtBegining: e.target.value }))}
                        required
                        placeholder={labels.professionalCategoryAtBegining}
                    />
                </FormControl>

                <FormControl>
                    <FormLabel>{labels.professionalDegreeAtBegining}</FormLabel>
                    <Input
                        type="number"
                        value={newAffair.professionalDegreeAtBegining}
                        onChange={(e) => setNewAffair((prev) => ({ ...prev, professionalDegreeAtBegining: e.target.value }))}
                        required
                        placeholder={labels.professionalCategoryAtBegining}
                    />
                </FormControl>


            </VStack>
            <HStack spacing={4} mt={4} justifyContent='flex-end' p={4}>
                <Button onClick={mode === 'update' ? handleUpdate : handleCreate} colorScheme='blue' shadow='md' isDisabled={!isAffairValid(newAffair)}>
                    {mode === 'update' ? labels.update : labels.save}
                </Button>
            </HStack>
            <pre><code>{JSON.stringify(newAffair)}</code></pre>
        </Box>
    );
};

export default AffairForm;

function isAffairValid(newAffair: affairState): boolean {
    return !!(newAffair &&
        newAffair.title.trim() &&
        newAffair.affairNumber.trim() &&
        newAffair.claimant.trim() &&
        newAffair.sectorialJointAgreement.sectorialJointAgreementId.trim() &&
        newAffair.startDateOfWork.trim() &&
        newAffair.endDateOfWork.trim() &&
        newAffair.professionalCategoryAtBegining.trim() &&
        newAffair.professionalDegreeAtBegining.trim());
}
