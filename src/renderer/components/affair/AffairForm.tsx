//src\renderer\components\affair\AffairForm.tsx
import React, { useEffect, useState } from 'react';
import { Box, Button, FormControl, FormLabel, Heading, Input, VStack, HStack, Text } from '@chakra-ui/react';
import { labels } from '../../arabic.labels';
import { useAppDispatch, useAppSelector } from '../../redux/redux.hooks';
import { AppDispatch, RootState } from '../../redux/store';
import { createAffair, fetchAffairById, switchToUpdateMode, switchToCreateMode, updateAffair } from '../../redux/affair/affairSlice';
import { useNavigate, useParams } from 'react-router-dom';
import AlfaSpinner from '../../shared/AlfaSpinner';
import GenericLookupDialog, { Entity } from '../../shared/GenericLookupDialog';
import { fetchAgreements } from '../../redux/sectorialJointAgreement/sectorialJointAgreementSlice';
import { fetchSalaryTablesByAgreementId } from '../../redux/sectorialJointAgreement/salaryTablesSlice';

interface affairState {
    affairId?: number;
    affairNumber: string;
    title: string;
    claimant: string;
    startDateOfWork: string;
    endDateOfWork: string;
    professionalCategoryAtBegining: string;
    professionalDegreeAtBegining: string | number;
    sectorialJointAgreement: { sectorialJointAgreementId: string, name: string };
    numeroTable: { numeroTableId: string, name: string };
    mode: 'create' | 'update';
}

const initialAffair: affairState = {
    affairId: undefined,
    affairNumber: '',
    title: '',
    claimant: '',
    startDateOfWork: '',
    endDateOfWork: '',
    professionalCategoryAtBegining: '',
    professionalDegreeAtBegining: '',
    sectorialJointAgreement: { sectorialJointAgreementId: '', name: '' },
    numeroTable: { numeroTableId: '', name: '' },
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
    const [isAggrementLookupWindowOpen, setIsAgreementLookupWindowOpen] = useState(false); // Dialog state for agreement Lookup
    const [isSalaryTableLookupWindowOpen, setIsSalaryTableLookupWindowOpen] = useState(false); // Dialog state for salary table Lookup
    const [agreementsForLookup, setAgreementsForLookup] = useState<Entity[]>([]); // Agreements for lookup
    const [salaryTablesForLookup, setSalaryTablesForLookup] = useState<Entity[]>([]); // Salary tables for lookup
    const [newAffair, setNewAffair] = useState<affairState>(initialAffair);



    useEffect(() => {
        if (affairId) {
            dispatch(switchToUpdateMode());
            dispatch(fetchAffairById(affairId));
        } else {
            dispatch(switchToCreateMode());
        }
    }, [affairId, dispatch]);

    useEffect(() => {
        dispatch(fetchAgreements()).then((agreements) => {
            setAgreementsForLookup(mapAgreementsToEntity(agreements["payload"]));
        })
    }, []);

    useEffect(() => {
        dispatch(fetchSalaryTablesByAgreementId(+newAffair.sectorialJointAgreement.sectorialJointAgreementId)).then((salaryTables) => {
            setSalaryTablesForLookup(mapSalryTablesToEntity(salaryTables["payload"]));
        })
    }, [newAffair.sectorialJointAgreement.sectorialJointAgreementId]);




    useEffect(() => {
        if (currentAffair && mode === 'update') {
            setNewAffair({
                ...currentAffair,
                sectorialJointAgreement: {
                    sectorialJointAgreementId: currentAffair.agreement.sectorialJointAgreementId ? currentAffair.agreement.sectorialJointAgreementId.toString() : '',
                    name: currentAffair.agreement.agreementName
                },

            })
        }
    }, [currentAffair, mode]);

    useEffect(() => {
        if (mode === 'create') {
            setNewAffair(initialAffair);
        }
    }, [mode]);

    const handleAgreementSelect = (agreement: Entity) => {
        setNewAffair((prev) => ({
            ...prev,
            sectorialJointAgreement: { sectorialJointAgreementId: agreement.id.toString(), name: agreement.label },
        }));
    };

    const handleNumeroTableSelect = (salaryTable: Entity) => {
        setNewAffair((prev) => ({
            ...prev,
            numeroTable: { numeroTableId: salaryTable.id.toString(), name: salaryTable.label },
        }));
    };

    const handleCreate = () => {
        const affairDTO = {
            affairNumber: newAffair.affairNumber,
            title: newAffair.title,
            claimant: newAffair.claimant,
            startDateOfWork: newAffair.startDateOfWork,
            endDateOfWork: newAffair.endDateOfWork,
            professionalCategoryAtBegining: newAffair.professionalCategoryAtBegining,
            professionalDegreeAtBegining: newAffair.professionalDegreeAtBegining,
            agreement: newAffair.sectorialJointAgreement.sectorialJointAgreementId,
            numeroTable: newAffair.numeroTable
        }


        if (isAffairValid(newAffair) && mode === 'create') {
            dispatch(createAffair(affairDTO))
                .then(() => navigate('/affairs'));
        }
    };

    const handleUpdate = () => {
        if (currentAffair && isAffairValid(newAffair)) {
            const updatedAffairDTO = {
                affairId: currentAffair.affairId,
                affairNumber: newAffair.affairNumber,
                title: newAffair.title,
                claimant: newAffair.claimant,
                startDateOfWork: newAffair.startDateOfWork,
                endDateOfWork: newAffair.endDateOfWork,
                professionalCategoryAtBegining: newAffair.professionalCategoryAtBegining,
                professionalDegreeAtBegining: newAffair.professionalDegreeAtBegining,
                agreement: newAffair.sectorialJointAgreement.sectorialJointAgreementId,
                numeroTable: newAffair.numeroTable
            };
            dispatch(updateAffair(updatedAffairDTO))
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
                        required
                        placeholder={labels.claimant}
                    />
                </FormControl>
                {/* ----------------------------------- Sectorial Joint Agreement ---------------------------------------------------------- */}
                <FormControl>
                    <FormLabel>{labels.sectorialJointAgreementId}</FormLabel>
                    <HStack>
                        <Input
                            type="text"
                            value={newAffair.sectorialJointAgreement.name}
                            placeholder={labels.sectorialJointAgreementId}
                            readOnly
                        />
                        <Button onClick={() => {
                            setIsAgreementLookupWindowOpen(true)
                        }}>{labels.selectAgreement}</Button>
                    </HStack>
                </FormControl>
                {/* Lookup Dialog for agreements*/}
                <GenericLookupDialog
                    isOpen={isAggrementLookupWindowOpen}
                    onClose={() => setIsAgreementLookupWindowOpen(false)}
                    entities={agreementsForLookup}
                    onSelect={handleAgreementSelect}
                    title={labels.selectAgreement}
                />
                {/* ------------------------------- numeroTable ------------------------------------------------------------------ */}
                <FormControl>
                    <FormLabel>{labels.numeroTable}</FormLabel>
                    <HStack>
                        <Input
                            type="number"
                            value={newAffair.numeroTable.name}
                            placeholder={labels.numeroTable}
                            readOnly
                        />
                        <Button onClick={() => {
                            setIsSalaryTableLookupWindowOpen(true)
                        }}>{labels.selectNumeroTable}</Button>
                    </HStack>
                </FormControl>
                {/* Lookup Dialog for salary tables*/}
                <GenericLookupDialog
                    isOpen={isSalaryTableLookupWindowOpen}
                    onClose={() => setIsSalaryTableLookupWindowOpen(false)}
                    entities={salaryTablesForLookup}
                    onSelect={handleNumeroTableSelect}
                    title={labels.selectNumeroTable}
                />
                {JSON.stringify(salaryTablesForLookup,null,2)}
                {/* ----------------------------------------------------------------------------------------- */}

                <FormControl>
                    <FormLabel>{labels.startDateOfWork}</FormLabel>
                    <Input
                        type="date"
                        value={newAffair.startDateOfWork}
                        onChange={(e) => setNewAffair((prev) => ({ ...prev, startDateOfWork: e.target.value }))}
                        required
                        placeholder={labels.startDateOfWork}
                    />
                </FormControl>
                <FormControl>
                    <FormLabel>{labels.endDateOfWork}</FormLabel>
                    <Input
                        type="date"
                        value={newAffair.endDateOfWork}
                        onChange={(e) => setNewAffair((prev) => ({ ...prev, endDateOfWork: e.target.value }))}
                        required
                        placeholder={labels.endDateOfWork}
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
                        placeholder={labels.professionalDegreeAtBegining}
                    />
                </FormControl>


            </VStack>
            <HStack spacing={4} mt={4} justifyContent='flex-end' p={4}>
                <Button onClick={mode === 'update' ? handleUpdate : handleCreate} colorScheme='blue' shadow='md' isDisabled={!isAffairValid(newAffair)}>
                    {mode === 'update' ? labels.update : labels.save}
                </Button>
            </HStack>
        </Box>
    );
};

export default AffairForm;

function isAffairValid(_affair: affairState): boolean {
    return !!(_affair &&
        _affair.title.trim() &&
        _affair.affairNumber.trim() &&
        _affair.claimant.trim() &&
        _affair.sectorialJointAgreement.sectorialJointAgreementId.trim() &&
        _affair.startDateOfWork.trim() &&
        _affair.endDateOfWork.trim() &&
        _affair.professionalCategoryAtBegining.trim() &&
        (typeof (_affair.professionalDegreeAtBegining) === 'number' ||
            typeof (_affair.professionalDegreeAtBegining) === "string"
            && _affair.professionalDegreeAtBegining.trim())
    );
}
function mapAgreementsToEntity(agreements: any) {
    return agreements.map((agreement: any) => ({ id: agreement.sectorialJointAgreementId, label: agreement.agreementName }))
}

function mapSalryTablesToEntity(salaryTables: any) {
    return salaryTables.map((salaryTable: any) => ({ id: salaryTable.numeroTable, label: ` ${salaryTable.consernedEmployee} {${salaryTable.type}} ${salaryTable.numeroTable}` }))
}
