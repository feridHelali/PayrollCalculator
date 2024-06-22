import React, { useState, useEffect } from 'react';
import { Table, Thead, Tbody, Tr, Th, Td, Button, Input, Box, Heading, Text } from '@chakra-ui/react';

import { createSalaryTable, fetchSalaryTables } from '../../redux/sectorialJointAgreement/salaryTableSlice';
import { FaSave } from 'react-icons/fa';
import { AppDispatch, RootState } from '../../redux/store';
import { useAppDispatch, useAppSelector } from '../../redux/redux.hooks';

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

const SalaryTableForm = ({sectorialJointAgreementId}: {sectorialJointAgreementId: number}) => {
   
    const dispatch: AppDispatch = useAppDispatch();
    const salaryTableStatus = useAppSelector((state: RootState) => state.salaryTables.status);
    const error = useAppSelector((state: RootState) => state.salaryTables.error);
    const currentAgreement = useAppSelector((state: RootState) => state.agreements.currentAgreement);
    const degrees = [1, 2, 3, 4, 5, 5, 6, 7, 8, 9, 10, 11, 12, 13];
    const categories = ['I', 'II', 'III', 'IV', 'V'];
    const workingAges = [1, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4];

    useEffect(() => {
        if (salaryTableStatus === 'idle') {
            dispatch(fetchSalaryTables(sectorialJointAgreementId));
        }
    }, [salaryTableStatus, dispatch]);

    const handleSave = async (salaries: { [s: string]: unknown; } | ArrayLike<unknown>) => {
        const salaryTable = {
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

        await dispatch(createSalaryTable(salaryTable));
    };


    return (
        <>
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
