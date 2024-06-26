import React, { FC, useState } from 'react';
import { Box, Button, Table, Tbody, Td, Th, Thead, Tr, VStack } from '@chakra-ui/react';
import { FaSave } from 'react-icons/fa';
import { ProfessionalCategory, ProfessionalDegree } from '../../../types/salaryTableProps';
import { labels } from '../../arabic.labels';

interface SalaryTableSheetProps {
  headers: ProfessionalDegree[];
  categories: ProfessionalCategory[];
  initialSalaries: Record<string, Record<string, number>>;
  onSave: (salaries: Record<string, Record<string, number>>) => void;
}

const SalaryTableSheet: FC<SalaryTableSheetProps> = ({ headers, categories, initialSalaries, onSave }) => {
  const [salaries, setSalaries] = useState(initialSalaries);

  const handleSalaryChange = (categoryKey: string, degreeKey: string, value: string) => {
    setSalaries((prevSalaries) => ({
      ...prevSalaries,
      [categoryKey]: {
        ...prevSalaries[categoryKey],
        [degreeKey]: parseFloat(value) || 0,
      },
    }));
  };

  return (
    <Box mt={4} p={4} fontSize={'xs'} bgColor={'gray.100'} borderRadius={5} w={'100vw'} h={'60vh'} overflow={'scroll'}>
      <Table variant="simple">
        <Thead>
          <Tr>
            <Th>{labels.category}</Th>

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
              <Td>{category.label}</Td>
              {headers.map((header) => (
                <Td key={header.key}>
                  <input
                    type="number"
                    value={salaries[category.key]?.[header.key] || ''}
                    onChange={(e) => handleSalaryChange(category.key, header.key, e.target.value)}
                  />
                </Td>
              ))}
            </Tr>
          ))}
        </Tbody>
      </Table>
      <VStack mt={4}>
        <Button leftIcon={<FaSave />} colorScheme="blue" onClick={() => onSave(salaries)}>
          {labels.save}
        </Button>
      </VStack>
    </Box>
  );
};

export default SalaryTableSheet;
