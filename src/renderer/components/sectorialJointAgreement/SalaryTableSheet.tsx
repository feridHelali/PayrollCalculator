import React, { FC, useState } from 'react';
import { Box, Button, Table, Tbody, Td, Th, Thead, Tr, VStack } from '@chakra-ui/react';
import { FaSave } from 'react-icons/fa';
import { ProfessionalCategory, ProfessionalDegree } from '../../../types/salaryTableProps';
import { labels } from '../../arabic.labels';

interface SalaryTableSheetProps {
  headers: ProfessionalDegree[];
  categories: ProfessionalCategory[];
  salaryTableCells: Record<string, Record<string, number>>;
  onSave: (salaryTableCells: Record<string, Record<string, number>>) => void;
}

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

const SalaryTableSheet: FC<SalaryTableSheetProps> = ({ headers, categories, salaryTableCells: intialSalaryTableCells, onSave }) => {
  console.dir(intialSalaryTableCells, 'SalaryTableSheet');
  const [salaryTableCells, setSalaryTableCells] = useState(intialSalaryTableCells);
 console.log(salaryTableCells, 'SalaryTableSheet');
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
    <Box 
    mt={4}
    p={4}
    fontSize={'xs'}
    bgColor={'gray.100'}
    borderRadius={5}
    width={'calc(100vw - 220px)'}
    height={'60vh'}
    overflow={'auto'}
    ml="200px"  // Adjust this if the width of your sidenav changes
    dir='rtl'>
      <Table variant="simple">
        <Thead>
          <Tr fontSize={'xs'}>
            <Th h={"70px"}>{labels.category}</Th>
            {headers.map((header) => (
              <Th key={header.key}>
                {labels.degree} {header.degree} <br />{labels.ageOfWork}: {header.ageOfWork}
              </Th>
            ))}
          </Tr>
        </Thead>
        <Tbody>
          {categories.map((category) => (
            <Tr key={category.key}>
              <Td w={"70px"}>{category.label}</Td>
              {headers.map((header) => (
                <Td key={header.key}>
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
        <Button leftIcon={<FaSave />} colorScheme="blue" onClick={() => onSave(salaryTableCells)}>
          {labels.save}
        </Button>
      </VStack>
    </Box>
  );
};

export default SalaryTableSheet;
