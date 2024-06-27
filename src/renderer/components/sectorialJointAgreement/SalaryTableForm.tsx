import React, { useState, useEffect } from 'react';
import {
  Button, Input, Box, Heading, Text, FormControl, FormLabel, Select, VStack,
} from '@chakra-ui/react';
import { FaSave } from 'react-icons/fa';
import {
  createSalaryTable, fetchSalaryTableById, updateSalaryTable, switchToCreateMode, switchToUpdateMode
} from '../../redux/sectorialJointAgreement/salaryTableSlice';
import { AppDispatch, RootState } from '../../redux/store';
import { useAppDispatch, useAppSelector } from '../../redux/redux.hooks';
import AlfaSpinner from '../../shared/AlfaSpinner';
import { ProfessionalCategory, ProfessionalDegree, SalaryTableProps } from '../../../types/salaryTableProps';
import { useNavigate, useParams } from 'react-router-dom';
import { labels } from '../../arabic.labels';
import { fetchAgreements } from '../../redux/sectorialJointAgreement/sectorialJointAgreementSlice';
import { salaryTypes } from '../../../types/salaryTypes';
import { clearStatus, setStatus } from '../../redux/common/statusSlice';
import SalaryTableStructure from './SalaryTableStructure';
import { v4 as uuid } from 'uuid';
import SalaryTableSheet from './SalaryTableSheet';

const SalaryTableForm = () => {
  const navigate = useNavigate();
  const { salaryTableId } = useParams<{ salaryTableId: string }>();
  const dispatch: AppDispatch = useAppDispatch();
  const salaryTableStatus = useAppSelector((state: RootState) => state.salaryTables.status) || 'idle';
  const error = useAppSelector((state: RootState) => state.salaryTables.error);
  const agreements = useAppSelector((state: RootState) => state.agreements.agreements);
  const mode = useAppSelector((state: RootState) => state.salaryTables.mode);
  const currentSalaryTable = useAppSelector((state: RootState) => state.salaryTables.currentSalaryTable);


  const [categories, setCategories] = useState<ProfessionalCategory[]>([{ key: uuid().toString(), label: `1 - ${labels.category}` }]); // Default initial category
  const [headers, setHeaders] = useState<ProfessionalDegree[]>([{ key: uuid().toString(), degree: 1, ageOfWork: 1 }]);
  const [salaries, setSalaries] = useState<Record<string, Record<string, number>>>({});

  useEffect(() => {
    dispatch(setStatus(salaryTableStatus));
    if (agreements.length <= 0) {
      dispatch(fetchAgreements());
    }
  }, [salaryTableStatus, dispatch, agreements.length]);

  useEffect(() => {
    if (salaryTableId) {
      dispatch(switchToUpdateMode());
      dispatch(fetchSalaryTableById(parseInt(salaryTableId)));
    } else {
      dispatch(switchToCreateMode());
      setNewSalaryTable(initialSalaryTable); // Reset new salary table on mode switch
    }
  }, [salaryTableId, dispatch]);

  useEffect(() => {
    if (currentSalaryTable) {
      setNewSalaryTable({
        ...currentSalaryTable,
        beginningDateOfApplication: new Date(currentSalaryTable.beginningDateOfApplication),
        endDateOfApplication: currentSalaryTable.endDateOfApplication ? new Date(currentSalaryTable.endDateOfApplication) : ''
      });
      setCategories(currentSalaryTable.categories);
      setHeaders(currentSalaryTable.degrees);
      // Populate salaries from the current salary table cells
      
      const salaryData: Record<string, Record<string, number>> = {};

      currentSalaryTable.salaryTableCells?.forEach(cell => {
        if (!salaryData[cell.professionalCategory]) {
          salaryData[cell.professionalCategory] = {};
        }
        salaryData[cell.professionalCategory][`${cell.professionalDegree}-${cell.ageOfWork}`] = cell.salary;
      });
      setSalaries(salaryData);

    }
  }, [currentSalaryTable]);
  const handleAddDegreeAndAgeOfWork = () => {
    const newDegree = headers.length + 1;
    const newAgeOfWork = 0;
    setHeaders([...headers, { key: uuid().toString(), degree: newDegree, ageOfWork: newAgeOfWork }]);
  };

  const handleAddCategory = () => {
    const newCategory = { key: uuid().toString(), label: `${categories.length + 1} - ${labels.category}` };
    setCategories([...categories, newCategory]);
  };

  const handleRemoveDegreeAndAgeOfWork = () => {
    if (headers.length > 1) {
      const newHeaders = [...headers];
      newHeaders.pop();
      setHeaders(newHeaders);
    }
  }

  const handleRemoveCategory = () => {
    if (categories.length > 1) {
      const newCategories = [...categories];
      newCategories.pop();
      setCategories(newCategories);
    }
  }

  const handleChangeDegreeAndAgeOfWork = (key: any, ageOfWork: any) => {
    const newHeaders = headers.map((header: any) => {
      if (header.key === key) {
        header.ageOfWork = ageOfWork;
      }
      return header;
    });
    setHeaders(newHeaders);
  };

  const handleChangeCategoryLabel = (categoryKey: any, label: any) => {

    const newCategories = categories.map((category: ProfessionalCategory) => {
      if (category.key === categoryKey) {
        category.label = label;
      }
      return category;
    });

    setCategories(newCategories);
  };



  const initialSalaryTable: SalaryTableProps = {
    agreementId: -1,
    numeroTable: '',
    type: '',
    consernedEmployee: '',
    beginningDateOfApplication: '',
    endDateOfApplication: '',
    degrees: [{ key: uuid(), degree: 1, ageOfWork: 1 }],
    categories: [{ key: uuid(), label: `1 - ${labels.category}` }],
  };

  const [newSalaryTable, setNewSalaryTable]: [Partial<SalaryTableProps>, any] = useState(initialSalaryTable);



  const handleCreateOrUpdate = () => {
    if (mode === 'create') {
      handleCreate();
    } else if (mode === 'update') {
      handleUpdate();
    }
  };

  const handleCreate = () => {
    if (isSalaryTableStructureReadyToBeSaved(newSalaryTable)) {
      dispatch(createSalaryTable({ ...newSalaryTable, degrees: headers, categories: categories }))
        .then(() => {
          if (salaryTableStatus === 'succeeded') {
            dispatch(setStatus(`${labels.salaryTableForm} - ${labels.created} - ${labels.successfully}`))
            navigate('/salary-tables');
          }
          if (salaryTableStatus === 'failed') {
            dispatch(setStatus(`${labels.salaryTableForm} - ${labels.created} - ${labels.failed}`));
          }
          setTimeout(() => {
            dispatch(clearStatus());
          }, 9000);
        });
    }
  };

  const handleUpdate = () => {
    if (currentSalaryTable && isSalaryTableStructureReadyToBeSaved(newSalaryTable)) {
      dispatch(updateSalaryTable({ ...newSalaryTable, salaryTableId: currentSalaryTable.salaryTableId }))
        .then(() => {
          if (salaryTableStatus === 'succeeded') {
            dispatch(setStatus(`${labels.salaryTableForm} - ${labels.updated} - ${labels.successfully}`));
            navigate('/salary-tables');
          }
          if (salaryTableStatus === 'failed') {
            dispatch(setStatus(`${labels.salaryTableForm} - ${labels.updated} - ${labels.failed}`));
          }
          setTimeout(() => {
            dispatch(clearStatus());
          }, 9000);
        });
    }
  };

  const handleSaveSalaries = (salaries: Record<string, Record<string, number>>) => {
    // Update the salary table with the new salary values
    handleSave(salaries);
  };
  const handleSave = async (salaries: Record<string, Record<string, number>>) => {
    const salaryTable = {
      ...newSalaryTable,
      salaries: Object.entries(salaries).flatMap(([category, degreeAges]) =>
        Object.entries(degreeAges).map(([degreeAge, salary]) => {
          const [degree, age] = degreeAge.split('-').map(Number);
          return {
            professionalCategory: category,
            professionalDegree: { degree, ageOfWork: age },
            salary: salary,
          };
        })
      ),
    };

    await Promise.all([
      dispatch(updateSalaryTable(salaryTable)).then(() => {
        navigate('/salary-tables')
      }),
      dispatch(setStatus(`${labels.salaryTableForm} - ${labels.updated} - ${labels.successfully}`)),
      setTimeout(() => {
        dispatch(clearStatus());
      }, 9000)
    ]);
  };





  return (
    <>
      <Box dir='rtl' marginLeft={"210px"} p={4} display="flex" flexDirection="column" alignItems="center">
        <pre><code>{JSON.stringify(salaries, null, 2)}</code></pre>
        <Heading mb={5} alignContent={'center'} fontSize={'2xl'} >{labels.salaryTableForm} {mode === 'create' ? labels.create : labels.update}</Heading>
        {salaryTableStatus === 'loading' && <AlfaSpinner />}
        {error && <Text color="red.500">{labels.error}: {error}</Text>}
        <VStack spacing={4} w={"100%"}>
          <FormControl id="agreementId" isRequired>
            <FormLabel>{labels.selectAgreement}</FormLabel>
            <Select
              name="agreementId"
              placeholder={`${labels.selectAgreement}`}
              value={newSalaryTable.agreementId}
              onChange={(e) => setNewSalaryTable({ ...newSalaryTable, agreementId: Number(e.target.value) })}
              isDisabled={mode === 'update'}
            >
              {agreements.map((agreement) => (
                <option key={agreement.sectorialJointAgreementId} value={agreement.sectorialJointAgreementId}>
                  {agreement.agreementName}
                </option>
              ))}
            </Select>
          </FormControl>
          <FormControl id="numeroTable" isRequired>
            <FormLabel>{labels.salaryTableNumber}</FormLabel>
            <Input
              name="numeroTable"
              type="text"
              value={newSalaryTable.numeroTable}
              onChange={(e) => setNewSalaryTable({ ...newSalaryTable, numeroTable: e.target.value })}
              isDisabled={mode === 'update'}
            />
          </FormControl>
          <FormControl id="type" isRequired>
            <FormLabel>{labels.salaryType}</FormLabel>
            <Select
              name="type"
              placeholder={`${labels.selectSalaryType}`}
              value={newSalaryTable.type}
              onChange={(e) => setNewSalaryTable({ ...newSalaryTable, type: e.target.value })}
              isDisabled={mode === 'update'}
            >
              {salaryTypes.map((salaryType) => (
                <option key={salaryType} value={salaryType}>
                  {salaryType}
                </option>
              ))}
            </Select>
          </FormControl>
          <FormControl id="consernedEmployee" isRequired>
            <FormLabel>{labels.concernedEmployee}</FormLabel>
            <Input
              name="consernedEmployee"
              value={newSalaryTable.consernedEmployee}
              onChange={(e) => setNewSalaryTable({ ...newSalaryTable, consernedEmployee: e.target.value })}
              isDisabled={mode === 'update'}
            />
          </FormControl>
          <FormControl id="beginningDateOfApplication" isRequired>
            <FormLabel>{labels.beginningDateOfApplication}</FormLabel>
            <Input
              name="beginningDateOfApplication"
              w={"60%"}
              alignContent={"right"}
              type="date"
              value={newSalaryTable.beginningDateOfApplication ? new Date(newSalaryTable.beginningDateOfApplication).toISOString().split('T')[0] : ''}
              onChange={(e) => setNewSalaryTable({ ...newSalaryTable, beginningDateOfApplication: e.target.value })}
              isDisabled={mode === 'update'}
            />
          </FormControl>
          <FormControl id="endDateOfApplication">
            <FormLabel>{labels.endDateOfApplication}</FormLabel>
            <Input
              name="endDateOfApplication"
              w={"60%"}
              type="date"
              value={newSalaryTable.endDateOfApplication ? new Date(newSalaryTable.endDateOfApplication).toISOString().split('T')[0] : ''}
              onChange={(e) => setNewSalaryTable({ ...newSalaryTable, endDateOfApplication: e.target.value })}

            />
          </FormControl>

          {mode === 'create' && (
            <>
              <SalaryTableStructure
                salaryTable={newSalaryTable}
                headers={headers}
                categories={categories}
                handleAddCategory={handleAddCategory}
                handleRemoveCategory={handleRemoveCategory}
                handleAddDegreeAndAgeOfWork={handleAddDegreeAndAgeOfWork}
                handleRemoveDegreeAndAgeOfWork={handleRemoveDegreeAndAgeOfWork}
                handleChangeCategoryLabel={handleChangeCategoryLabel}
                handleChangeDegreeAndAgeOfWork={handleChangeDegreeAndAgeOfWork}
              />
              <Button
                leftIcon={<FaSave />}
                colorScheme="teal"
                onClick={handleCreateOrUpdate}
              >
                {labels.create}
              </Button>
            </>
          )}


          {mode === 'update' && currentSalaryTable && (
            <>
              <Box mt={4}>
                <SalaryTableSheet
                  headers={headers}
                  categories={categories}
                  initialSalaries={salaries}
                  onSave={handleSaveSalaries}
                />
              </Box>
              <Button
                leftIcon={<FaSave />}
                colorScheme="blue"
                onClick={handleCreateOrUpdate}
              >
                {labels.update}
              </Button>
            </>
          )}
        </VStack>
      </Box>
    </>
  );
};

export default SalaryTableForm;

const isSalaryTableStructureReadyToBeSaved = (salaryTable: Partial<SalaryTableProps>): boolean => {
  return (
    !!salaryTable.agreementId &&
    !!salaryTable.numeroTable &&
    !!salaryTable.type &&
    !!salaryTable.consernedEmployee &&
    !!salaryTable.beginningDateOfApplication
  );
};