import React, { FC, useState } from 'react';
import { Box, Button, Table, Tbody, Td, Th, Thead, Tr, VStack } from '@chakra-ui/react';
//import { FaSave } from 'react-icons/fa';
import { ProfessionalCategory, ProfessionalDegree } from '../../../types/salaryTableProps';
import { labels } from '../../arabic.labels';
import { useSalaryTableContext } from './contexts/SalaryTableContext';
import  './SalaryTableSheet.module.css'


interface Header {
  key: string;
  degree: number;
  ageOfWork: number;
}

interface Category {
  key: string;
  label: string;
}


type RowData = {
  category: string;
  [key: string]: number | string;
};

const SalaryTableSheet: FC = () => {
  
  const { categories, headers, salaryTableCells, setSalaryTableCells } = useSalaryTableContext();
  const handleSalaryChange = (categoryKey: string, degreeKey: string, value: string) => {
    setSalaryTableCells((prevSalaryTableCells) => ({
      ...prevSalaryTableCells,
      [categoryKey]: {
        ...prevSalaryTableCells[categoryKey],
        [degreeKey]: parseFloat(value) || 0,
      },
    }));
  };

  return (
    <Box className='salary-table-sheet__container'>
      <Table  className='salary-table-sheet__table'>
        <Thead className='salary-table-sheet__header'>
          <Tr>
            <Th className='salary-table-sheet__header-cell'>{labels.category}</Th>
            {headers.map((header) => (
              <Th key={header.key}  className='salary-table-sheet__header-cell'>
                {labels.degree} {header.degree} <br />{labels.ageOfWork}: {header.ageOfWork}
              </Th>
            ))}
          </Tr>
        </Thead>
        <Tbody className='salary-table-sheet__body'>
          {categories.map((category) => (
            <Tr key={category.key} className='salary-table-sheet__body-row'>
              <Td className='salary-table-sheet__body-row-cell'>{category.label}</Td>
              {headers.map((header) => (
                <Td key={header.key} className='salary-table-sheet__body-row-cell'>
                  <input
                    type="number"
                    value={salaryTableCells[category.key]?.[header.key] || ''}
                    onChange={(e) => handleSalaryChange(category.key, header.key, e.target.value)}
                    style={{ width: '80%' }}
                  />
                </Td>
              ))}
            </Tr>
          ))}
        </Tbody>
      </Table>
      <VStack mt={4}>
        {/* <Button leftIcon={<FaSave />} colorScheme="blue" onClick={() => onSave(salaryTableCells)}>
          {labels.save}
        </Button> */}
      </VStack>
    </Box>
  );
};

export default SalaryTableSheet;
