import React, { useState, useEffect } from 'react';
import {
  Button, Input, Box, Heading, Text, FormControl, FormLabel, Select, VStack,
} from '@chakra-ui/react';
import { createSalaryTable, fetchSalaryTableById, updateSalaryTable } from '../../redux/sectorialJointAgreement/salaryTableSlice';
import { AppDispatch, RootState } from '../../redux/store';
import { useAppDispatch, useAppSelector } from '../../redux/redux.hooks';
import AlfaSpinner from '../../shared/AlfaSpinner';
import { SalaryTableProps } from '../../../types/salaryTableProps';
import { useNavigate, useParams } from 'react-router-dom';
import { labels } from '../../arabic.labels';
import { fetchAgreements, switchToCreateMode, switchToUpdateMode } from '../../redux/sectorialJointAgreement/sectorialJointAgreementSlice';
import { salaryTypes } from '../../../types/salaryTypes';
import { clearStatus, setStatus } from '../../redux/common/statusSlice';
import SalaryTableInput from './SalaryTableInput';

const SalaryTableForm = () => {
  const navigate = useNavigate();
  const { salaryTableId } = useParams<{ salaryTableId: string }>();
  const dispatch: AppDispatch = useAppDispatch();
  const salaryTableStatus = useAppSelector((state: RootState) => state.salaryTables.status);
  const error = useAppSelector((state: RootState) => state.salaryTables.error);
  const agreements = useAppSelector((state: RootState) => state.agreements.agreements);
  const mode = useAppSelector((state: RootState) => state.salaryTables.mode);
  const currentSalaryTable = useAppSelector((state: RootState) => state.salaryTables.currentSalaryTable);

  const [degrees, setDegrees] = useState<number[]>([1]); // Default initial degree
  const [categories, setCategories] = useState<string[]>(['Category 1']); // Default initial category
  const [workingAges, setWorkingAges] = useState<number[]>([1]); // Default initial working age
  const [headers, setHeaders] = useState<{ degree: number, ageOfWork: number }[]>([{ degree: 1, ageOfWork: 1 }]); // Default initial header

  const initialSalaryTable: SalaryTableProps = {
    agreementId: -1,
    numeroTable: '',
    type: '',
    consernedEmployee: '',
    beginningDateOfApplication: '',
    endDateOfApplication: '',
    degrees: [],
    workingAges: [],
    categories: []
  };

  const [newSalaryTable, setNewSalaryTable]: [Partial<SalaryTableProps>, any] = useState(initialSalaryTable);

  useEffect(() => {
    dispatch(setStatus(labels.idle));
    if (salaryTableStatus === 'idle') {
      dispatch(fetchAgreements());
    }
  }, [salaryTableStatus, dispatch]);

  useEffect(() => {
    if (salaryTableId) {
      dispatch(switchToUpdateMode())
      dispatch(fetchSalaryTableById(parseInt(salaryTableId)));
    } else {
      dispatch(switchToCreateMode())
      setNewSalaryTable(initialSalaryTable); // Reset new salary table on mode switch
    }
  }, [salaryTableId, dispatch, initialSalaryTable]);

  useEffect(() => {
    if (currentSalaryTable) {
      setNewSalaryTable(
        {
          ...currentSalaryTable,
          beginningDateOfApplication: new Date(currentSalaryTable.beginningDateOfApplication),
          endDateOfApplication: currentSalaryTable.endDateOfApplication ? new Date(currentSalaryTable.endDateOfApplication) : ''
        }
      );
    }
  }, [currentSalaryTable]);

  const handleCreate = () => {
    if (newSalaryTable.agreementId && newSalaryTable.type && newSalaryTable.consernedEmployee) {
      dispatch(createSalaryTable(newSalaryTable))
        .then(() => {
          if (salaryTableStatus === 'succeeded')
            dispatch(setStatus(`${labels.salaryTableForm} - ${labels.created} - ${labels.successfully}`));
          if (salaryTableStatus === 'failed')
            dispatch(setStatus(`${labels.salaryTableForm}  - ${labels.created} - ${labels.failed}`));
          setTimeout(() => {
            dispatch(clearStatus());
          }, 9000);
        })
        .then(() => {
          navigate('/salary-tables')
        });
    }
  };

  const handleUpdate = () => {
    if (currentSalaryTable && newSalaryTable.agreementId && newSalaryTable.type && newSalaryTable.consernedEmployee) {
      dispatch(updateSalaryTable({ ...newSalaryTable, salaryTableId: currentSalaryTable.salaryTableId }))
        .then(() => {
          if (salaryTableStatus === 'succeeded') {
            dispatch(setStatus(`${labels.salaryTableForm} - ${labels.updated} - ${labels.successfully}`));
            navigate('/salary-tables')
            setTimeout(() => {
              dispatch(clearStatus());
            }, 10000);
          }
          if (salaryTableStatus === 'failed')
            dispatch(setStatus(`${labels.salaryTableForm}  - ${labels.updated} - ${labels.failed}`));

        })

    }
  }

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

  const handleAddDegreeAndAgeOfWork = () => {
    const newDegree = degrees.length + 1;
    const newAgeOfWork = workingAges.length + 1;
    setDegrees([...degrees, newDegree]);
    setWorkingAges([...workingAges, newAgeOfWork]);
    setHeaders([...headers, { degree: newDegree, ageOfWork: newAgeOfWork }]);
  };

  const handleAddCategory = () => {
    const newCategory = `Category ${categories.length + 1}`;
    setCategories([...categories, newCategory]);
  };

  return (
    <>
      <Box m={4} p={4} bgColor={'gray.100'} borderRadius={5}>
        <Heading mb={5} alignContent={'center'} fontSize={'2xl'}>{labels.salaryTableForm} {mode === 'create' ? labels.create : labels.update}</Heading>
        {salaryTableStatus === 'loading' && <AlfaSpinner />}
        {error && <Text colorScheme="red">{labels.error}: {error}</Text>}
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
              onChange={(e) => setNewSalaryTable({ ...newSalaryTable, numeroTable: e.target.value })}
            />
          </FormControl>
          <FormControl>
            <FormLabel>{labels.salaryType}</FormLabel>
            <Select
              placeholder={`${labels.selectSalaryType}`}
              value={newSalaryTable.type}
              onChange={(e) => setNewSalaryTable({ ...newSalaryTable, type: e.target.value })}
            >
              {salaryTypes.map((salaryType) => (
                <option key={salaryType} value={salaryType}>
                  {salaryType}
                </option>
              ))}
            </Select>
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
              value={newSalaryTable.beginningDateOfApplication ? new Date(newSalaryTable.beginningDateOfApplication).toISOString().split('T')[0] : ''}
              onChange={(e) => setNewSalaryTable({ ...newSalaryTable, beginningDateOfApplication: e.target.value })}
            />
          </FormControl>
          <FormControl>
            <FormLabel>{labels.endDateOfApplication}</FormLabel>
            <Input
              w={"60%"}
              alignContent={"right"}
              type="date"
              value={newSalaryTable.endDateOfApplication ? new Date(newSalaryTable.endDateOfApplication).toISOString().split('T')[0] : ''}
              onChange={(e) => setNewSalaryTable({ ...newSalaryTable, endDateOfApplication: e.target.value })}
            />
          </FormControl>
          <Button onClick={handleAddCategory} colorScheme="green">{labels.addCategory}</Button>
          <Button onClick={handleAddDegreeAndAgeOfWork} colorScheme="green">{labels.addDegree}</Button>
          <Button colorScheme="blue" shadow="md" onClick={mode === 'create' ? handleCreate : handleUpdate} isDisabled={!isSalaryTableFormValid(newSalaryTable)}>
            {labels.create}
          </Button>
        </VStack>
      </Box>
      {headers.length > 0 && categories.length > 0 && (
        <SalaryTableInput
          headers={headers}
          categories={categories}
          onSave={handleSave}
        />
      )}
      {error && <Text colorScheme='red'>Error: {error}</Text>}
    </>
  );
};

export default SalaryTableForm;

function isSalaryTableFormValid(salaryTable: any): boolean {
  if (!salaryTable) {
    return false;
  }
  if (!salaryTable.numeroTable || !salaryTable.type || !salaryTable.consernedEmployee
    || !salaryTable.beginningDateOfApplication || !salaryTable.degrees
    || !salaryTable.categories || !salaryTable.workingAges) {
    return false;
  }

  return true;
}
