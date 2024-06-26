import React, { useState } from 'react';
import { Button, Input, Box, Heading, HStack, VStack } from '@chakra-ui/react';
import { FaSave } from 'react-icons/fa';
import { labels } from '../../arabic.labels';
import { ProfessionalCategory, ProfessionalDegree, SalaryTableProps } from '../../../types/salaryTableProps';
import { v4 as uuid } from 'uuid';

const SalaryTableStructure = ({ salaryTable }: { salaryTable: Partial<SalaryTableProps> }): React.JSX.Element => {
  const [categories, setCategories] = useState<ProfessionalCategory[]>([{ key: uuid().toString(), label: `1 - ${labels.category}` }]); // Default initial category
  const [headers, setHeaders] = useState<ProfessionalDegree[]>([{ key: uuid().toString(), degree: 1, ageOfWork: 1 }]);
  const handleAddDegreeAndAgeOfWork = () => {
    const newHeadersLength = headers.length + 1;
    const newDegree = newHeadersLength + 1;
    const newAgeOfWork = newHeadersLength + 1;
    setHeaders([...headers, { key: uuid().toString(), degree: newDegree, ageOfWork: newAgeOfWork }]);
  };

  const handleAddCategory = () => {
    const newCategory = { key: uuid().toString(), label: `${categories.length + 1} - ${labels.category}` };
    setCategories([...categories, newCategory]);
  }; // Default initial header

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


  return (
    <Box>
      <Heading mb={4}>{labels.salaryTableForm}</Heading>
      <HStack>
        <Box>{labels.ageOfWork}</Box>
        {headers.map((header: any) => (
          <Box key={header.key}>
            <Box>{labels.degree} - {header.degree}</Box>
            <Input
              type="number"
              value={header.ageOfWork}
              onChange={(e) =>
                handleChangeDegreeAndAgeOfWork(header.key, e.target.value)
              }
            />
          </Box>
        ))}
      </HStack>
      <hr />
      <HStack>
        <Box>{labels.category}</Box>
        {categories.map((category: ProfessionalCategory) => (
          <Box key={category.key}>
            <Input
              type="text"
              value={category.label}
              onChange={(e) =>
                handleChangeCategoryLabel(category.key, e.target.value)
              }
            />
          </Box>
        ))}
      </HStack>
      {isNewSalaryTableValid(salaryTable) && (
        <>
          <VStack mt={4}>
            <HStack>
              <Button onClick={handleAddCategory} colorScheme="green" >{labels.addCategory}</Button>
              <Button onClick={handleRemoveCategory} colorScheme="red" >{labels.removeCategory}</Button>
            </HStack>
            <HStack>
              <Button onClick={handleAddDegreeAndAgeOfWork} colorScheme="green">{labels.addDegree}</Button>
              <Button onClick={handleRemoveDegreeAndAgeOfWork} colorScheme="red">{labels.removeDegree}</Button>
            </HStack>
          </VStack>


          <Button leftIcon={<FaSave />} onClick={() => { }} colorScheme="blue" mt={4}>
            {labels.save}
          </Button>

        </>

      )}

      <code>{JSON.stringify(categories, null, 2)}</code>
      <hr />
      <code>{JSON.stringify(headers, null, 2)}</code>
    </Box>
  );
};

export default SalaryTableStructure;

function isNewSalaryTableValid(salaryTable: Partial<SalaryTableProps>): boolean {

  return !!salaryTable.agreementId &&
    !!salaryTable.numeroTable &&
    !!salaryTable.type &&
    !!salaryTable.consernedEmployee &&
    !!salaryTable.beginningDateOfApplication;
}