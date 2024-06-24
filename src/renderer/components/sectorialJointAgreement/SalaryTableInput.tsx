import React, { useState, useEffect } from 'react';
import {
  Table, Thead, Tbody, Tr, Th, Td, Button, Input, Box, Heading,
} from '@chakra-ui/react';
import { FaSave } from 'react-icons/fa';
import { labels } from '../../arabic.labels';

const SalaryTableInput = ({ headers, categories, onSave }: any): React.JSX.Element => {
  const [salaries, setSalaries] = useState(
    categories.reduce((acc: { [x: string]: any; }, category: string | number) => {
      acc[category] = headers.reduce((acc: { [x: string]: string; }, header: any) => {
        acc[`${header.degree}-${header.ageOfWork}`] = '';
        return acc;
      }, {});
      return acc;
    }, {})
  );

  useEffect(() => {
    setSalaries(
      categories.reduce((acc: { [x: string]: any; }, category: string | number) => {
        acc[category] = headers.reduce((acc: { [x: string]: string; }, header: any) => {
          acc[`${header.degree}-${header.ageOfWork}`] = '';
          return acc;
        }, {});
        return acc;
      }, {})
    );
  }, [headers, categories]);

  const handleChange = (category: string | number, degree: any, ageOfWork: any, value: string) => {
    setSalaries((prev: { [x: string]: any; }) => ({
      ...prev,
      [category]: {
        ...prev[category],
        [`${degree}-${ageOfWork}`]: value,
      },
    }));
  };

  const handleSave = () => {
    onSave(salaries);
  };

  return (
    <Box>
      <Heading mb={4}>{labels.salaryTableForm}</Heading>
      <Table variant="simple">
        <Thead>
          <Tr>
            <Th>{labels.degree}</Th>
            {headers.map((header: any, index: number) => (
              <Th key={index}>{`${labels.degree} ${header.degree} (${labels.workingAge} ${header.ageOfWork})`}</Th>
            ))}
          </Tr>
        </Thead>
        <Tbody>
          {categories.map((category: string) => (
            <Tr key={category}>
              <Td>{category}</Td>
              {headers.map((header: any) => (
                <Td key={`${header.degree}-${header.ageOfWork}`}>
                  <Input
                    type="number"
                    value={salaries[category]?.[`${header.degree}-${header.ageOfWork}`] || ''}
                    onChange={(e) =>
                      handleChange(category, header.degree, header.ageOfWork, e.target.value)
                    }
                  />
                </Td>
              ))}
            </Tr>
          ))}
        </Tbody>
      </Table>

      <Button leftIcon={<FaSave />} onClick={handleSave} colorScheme="blue" mt={4}>
        {labels.save}
      </Button>
    </Box>
  );
};

export default SalaryTableInput;
