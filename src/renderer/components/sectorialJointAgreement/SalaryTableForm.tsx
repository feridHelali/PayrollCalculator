import React, { useState, useEffect } from 'react';
import { Table, Thead, Tbody, Tr, Th, Td, Button, Input, Box, Heading, Text } from '@chakra-ui/react';
import { createSalaryTable, fetchSalaryTableById } from '../../redux/sectorialJointAgreement/salaryTableSlice';
import { FaSave } from 'react-icons/fa';
import { AppDispatch, RootState } from '../../redux/store';
import { useAppDispatch, useAppSelector } from '../../redux/redux.hooks';
import { FormControl, FormLabel, Select, VStack, } from '@chakra-ui/react';




import AlfaSpinner from '../../shared/AlfaSpinner';
import { SalaryTableProps } from '../../../types/salaryTableProps';
import { useNavigate, useParams } from 'react-router-dom';
import { labels } from '../../arabic.labels';
import { fetchAgreements } from '../../redux/sectorialJointAgreement/sectorialJointAgreementSlice';

const SalaryTableInput = ({ degrees, categories, workingAges, onSave }: any): React.JSX.Element => {
    const [salaries, setSalaries] = useState(
        categories.reduce((acc: { [x: string]: any; }, category: string | number) => {
            acc[category] = degrees.reduce((acc: { [x: string]: string; }, degree: any) => {
                workingAges.forEach((age: any) => {
                    acc[`${degree}-${age}`] = '';
                });
                return acc;
            }, {});
            return acc;
        }, {})
    );

    const handleChange = (category: string | number, degree: any, age: any, value: string) => {
        setSalaries((prev: { [x: string]: any; }) => ({
            ...prev,
            [category]: {
                ...prev[category],
                [`${degree}-${age}`]: value,
            },
        }));
    };

    const handleSave = () => {
        onSave(salaries);
    };

    return (
        <Box>
            <Heading mb={4}>Input Salary Table</Heading>
            <Table variant="simple">
                <Thead>
                    <Tr>
                        <Th>Professional Category</Th>
                        {degrees.map((degree: number) => (
                            <Th key={degree}>Degree {degree}</Th>
                        ))}
                    </Tr>
                </Thead>
                <Tbody>
                    {categories.map((category: string) => (
                        <Tr key={category}>
                            <Td>{category}</Td>
                            {degrees.map((degree: React.Key | null | undefined) => (
                                <Td key={degree}>
                                    {workingAges.map((age: number) => (
                                        <Box key={age} mb={2}>
                                            <label>Age {age}</label>
                                            <Input
                                                type="number"
                                                value={salaries[category][`${degree}-${age}`]}
                                                onChange={(e) =>
                                                    handleChange(category, degree, age, e.target.value)
                                                }
                                            />
                                        </Box>
                                    ))}
                                </Td>
                            ))}
                        </Tr>
                    ))}
                </Tbody>
            </Table>

            <Button leftIcon={<FaSave />} onClick={handleSave} colorScheme="blue" mt={4}>
                Save
            </Button>
        </Box>
    );
};

const SalaryTableForm = () => {
    const navigate = useNavigate();
    const { salaryTableId } = useParams();
    const dispatch: AppDispatch = useAppDispatch();
    const salaryTableStatus = useAppSelector((state: RootState) => state.salaryTables.status);
    const error = useAppSelector((state: RootState) => state.salaryTables.error);
    const agreements = useAppSelector((state: RootState) => state.agreements.agreements);
    const status = useAppSelector((state: RootState) => state.salaryTables.status);
    const mode = useAppSelector((state: RootState) => state.salaryTables.mode);
    const currentSalaryTable = useAppSelector((state: RootState) => state.salaryTables.currentSalaryTable);



    const degrees = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    const categories = ['I', 'II', 'III', 'IV', 'V'];
    const workingAges = [1, 2, 2, 2, 3, 3, 3, 3, 4, 4];

    const [newSalaryTable, setNewSalaryTable]: [Partial<SalaryTableProps>, any] = useState({
        agreementId: -1,
        numeroTable:'',
        type: '',
        consernedEmployee: '',
        beginningDateOfApplication: new Date(),
        endDateOfApplication: new Date(),
        degrees: [],
        workingAges: [],
        categories: []
    });

    useEffect(() => {
        if (status === 'idle') {
            dispatch(fetchAgreements());
        }
    }, [status, dispatch]);

    useEffect(() => {
        if (salaryTableStatus === 'idle' && salaryTableId) {
            dispatch(fetchSalaryTableById(parseInt(salaryTableId)));
        }
    }, [salaryTableStatus, dispatch]);

    const handleCreate = () => {
        if (newSalaryTable.agreementId && newSalaryTable.type && newSalaryTable.consernedEmployee) {
            dispatch(createSalaryTable(newSalaryTable))
                .then(() => {
                    navigate('/salary-tables')
                });
        }
    };


    const handleSave = async (salaries: { [s: string]: unknown; } | ArrayLike<unknown>) => {
        const salaryTable = {
            ...newSalaryTable,
            degrees,
            workingAges,
            categories,
            salaries: Object.entries(salaries).flatMap(([category, degreeAges]) =>
                Object.entries(degreeAges as object).map(([degreeAge, salary]) => {
                    const [degree, age] = degreeAge.split('-').map(Number);
                    return {
                        professionalCategory: category,
                        professionalDegree: degree,
                        workingAge: age,
                        salary: parseFloat(salary as string),
                    };
                })
            ),
        };
        
        await dispatch(createSalaryTable(salaryTable)).then(() => {
            navigate('/salary-tables')
        });
    };


    return (
        <>
            <Box m={4} p={4} bgColor={'gray.100'} borderRadius={5}>
                <Heading mb={5} alignContent={'center'} fontSize={'2xl'}>{labels.salaryTableForm} {mode==='create' ? labels.create : labels.update}</Heading>
                {status === 'loading' && <AlfaSpinner />}
                {error && <Text color="red">{labels.error}: {error}</Text>}
                <VStack spacing={4} w={"80%"} m={4} p={4}>
                    <FormControl>
                        <FormLabel>{labels.selectAgreement}</FormLabel>
                        <Select
                            placeholder={`${labels.selectAgreement}`}
                            value={newSalaryTable.agreementId}
                            onChange={(e) => setNewSalaryTable({ ...newSalaryTable, agreementId: Number(e.target.value) })}
                        >
                            {agreements.map((agreement) => (
                                <option key={agreement.sectorialJointAgreementId} value={agreement.sectorialJointAgreementId}>
                                    {agreement.agreementName}
                                </option>
                            ))}
                        </Select>
                    </FormControl>
                    <FormControl>
                        <FormLabel>{labels.salaryTableNumber}</FormLabel>
                        <Input
                            type="text"
                            value={newSalaryTable.numeroTable}
                            onChange={(e) => setNewSalaryTable({ ...newSalaryTable, numeroTable: Number(e.target.value) })}
                        />
                    </FormControl>
                    <FormControl>
                        <FormLabel>{labels.salaryType}</FormLabel>
                        <Input
                            value={newSalaryTable.type}
                            onChange={(e) => setNewSalaryTable({ ...newSalaryTable, type: e.target.value })}
                        />
                    </FormControl>
                    <FormControl>
                        <FormLabel>{labels.concernedEmployee}</FormLabel>
                        <Input
                            value={newSalaryTable.consernedEmployee}
                            onChange={(e) => setNewSalaryTable({ ...newSalaryTable, consernedEmployee: e.target.value })}
                        />
                    </FormControl>
                    <FormControl>
                        <FormLabel>{labels.beginningDateOfApplication}</FormLabel>
                        <Input
                            w={"60%"}
                            alignContent={"right"}
                            type="date"
                            value={newSalaryTable.beginningDateOfApplication?.toISOString().split('T')[0]}
                            onChange={(e) => setNewSalaryTable({ ...newSalaryTable, beginningDateOfApplication: new Date(e.target.value) })}
                        />
                    </FormControl>
                    <FormControl>
                        <FormLabel>{labels.endDateOfApplication}</FormLabel>
                        <Input
                            w={"60%"}
                            alignContent={"right"}
                            type="date"
                            value={newSalaryTable.endDateOfApplication?.toISOString().split('T')[0] || ''}
                            onChange={(e) => setNewSalaryTable({ ...newSalaryTable, endDateOfApplication: e.target.value ? new Date(e.target.value) : null })}
                        />
                    </FormControl>
                    {/* Add inputs for degrees, workingAges, categories as needed */}
                    <Button colorScheme="blue" shadow="md" onClick={handleCreate} isDisabled={!isSalaryTableFormValid(newSalaryTable)}>
                        {labels.create}
                    </Button>
                </VStack>
               
            </Box>
            <SalaryTableInput
                degrees={degrees}
                categories={categories}
                workingAges={workingAges}
                onSave={handleSave}
            />
            {error && <Text colorScheme='red'>Error: {error}</Text>}
        </>
    );
};

export default SalaryTableForm;


function isSalaryTableFormValid(salaryTable: any): boolean {
    if (!salaryTable) {
        return false;
    }
    if(!salaryTable.numeroTable || !salaryTable.type || !salaryTable.consernedEmployee || !salaryTable.beginningDateOfApplication || !salaryTable.endDateOfApplication) {
        return false;
    }

    return true;
}
