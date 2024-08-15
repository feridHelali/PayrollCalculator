import React, { useEffect, useState } from 'react';
import { Box, Button, FormControl, FormLabel, Heading, Input, VStack, HStack, Text } from '@chakra-ui/react';
import { labels } from '../../arabic.labels';
import { affairProps } from '../../../types/affairProps';
import { useAppDispatch, useAppSelector } from '../../redux/redux.hooks';
import { AppDispatch, RootState } from '../../redux/store';
import { createAffair, fetchAffairById, switchToUpdateMode, switchToCreateMode, updateAffair } from '../../redux/affair/affairSlice';
import { useNavigate, useParams } from 'react-router-dom';
import AlfaSpinner from '../../shared/AlfaSpinner';

interface affairState  {
    affairId: string;
    affairNumber: string;
    title: string;
    claimant: string;
    startDateOfWork: string;
    endDateDateOfWork: string;
    mode: 'create' | 'update';
}

const initialAffair: affairState = {
    affairId: '',
    affairNumber: '',
    title: '',
    claimant: '',
    startDateOfWork: '',
    endDateDateOfWork: '',
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

    const handleCreate = () => {
        if (newAffair.title.trim() && mode === 'create') {
            dispatch(createAffair(newAffair))
                .then(() => navigate('/affairs'));
        }
    };

    const handleUpdate = () => {
        if (currentAffair && newAffair.title.trim()) {
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
                    <FormLabel>{labels.claimant}</FormLabel>
                    <Input
                        value={newAffair.claimant}
                        onChange={(e) => setNewAffair((prev) => ({ ...prev, concernedDepartment: e.target.value }))}
                    />
                </FormControl>
                <FormControl>
                    <FormLabel>{labels.startDateOfWork}</FormLabel>
                    <Input
                        type="date"
                        value={newAffair.startDateOfWork}
                        onChange={(e) => setNewAffair((prev) => ({ ...prev, startDate: e.target.value }))}
                    />
                </FormControl>
                <FormControl>
                    <FormLabel>{labels.endDateOfWork}</FormLabel>
                    <Input
                        type="date"
                        value={newAffair.endDateDateOfWork}
                        onChange={(e) => setNewAffair((prev) => ({ ...prev, endDate: e.target.value }))}
                    />
                </FormControl>
            </VStack>
            <HStack spacing={4} mt={4} justifyContent='flex-end' p={4}>
                <Button onClick={mode === 'update' ? handleUpdate : handleCreate} colorScheme='blue' shadow='md' isDisabled={!isNewAffairValid(newAffair)}>
                    {mode === 'update' ? labels.update : labels.save}
                </Button>
            </HStack>
        </Box>
    );
};

export default AffairForm;

function isNewAffairValid(newAffair: affairState): boolean {
    return !!(newAffair && newAffair.title.trim());
}
