import React, { useState } from 'react';
import { Button, Input, Box, Heading, HStack, VStack } from '@chakra-ui/react';
import { labels } from '../../arabic.labels';
import { ProfessionalCategory, ProfessionalDegree, SalaryTableProps } from '../../../types/salaryTableProps';

interface SalaryTableStructureProps {
  salaryTable: Partial<SalaryTableProps>,
  headers: any,
  categories: any,
  handleAddCategory: any,
  handleAddDegreeAndAgeOfWork: any,
  handleRemoveCategory: any,
  handleRemoveDegreeAndAgeOfWork: any,
  handleChangeCategoryLabel: any,
  handleChangeDegreeAndAgeOfWork: any
}

const SalaryTableStructure = ({
  salaryTable,
  headers,
  categories,
  handleAddCategory,
  handleAddDegreeAndAgeOfWork,
  handleRemoveCategory,
  handleRemoveDegreeAndAgeOfWork,
  handleChangeCategoryLabel,
  handleChangeDegreeAndAgeOfWork }: SalaryTableStructureProps): React.JSX.Element => {


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
            <hr />
            <HStack>
              <Button onClick={handleAddDegreeAndAgeOfWork} colorScheme="green">{labels.addDegree}</Button>
              <Button onClick={handleRemoveDegreeAndAgeOfWork} colorScheme="red">{labels.removeDegree}</Button>
            </HStack>
          </VStack>
        </>

      )}

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