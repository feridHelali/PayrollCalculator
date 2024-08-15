import React, { useEffect } from 'react';
import {
  Box,
  Button,
  HStack,
  Heading,
  Text,
  Table,
  Thead,
  Tbody,
  Tr,
  Th,
  Td,
} from '@chakra-ui/react';
import { useAppDispatch, useAppSelector } from '../../redux/redux.hooks';
import { useNavigate } from 'react-router-dom';
import { fetchAffairs, deleteAffair } from '../../redux/affair/affairSlice';
import AlfaSpinner from '../../shared/AlfaSpinner';
import { labels } from '../../arabic.labels';

const AffairList: React.FC = () => {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const affairs = useAppSelector((state) => state.affairs.affairs);
  const affairsStatus = useAppSelector((state) => state.affairs.status);
  const error = useAppSelector((state) => state.affairs.error);

  useEffect(() => {
    if (affairsStatus === 'idle') {
      dispatch(fetchAffairs());
    }
  }, [affairsStatus, dispatch]);

  const handleEdit = (affairId: any) => {
    navigate(`/affair-form/${affairId}`);
  };

  const handleDelete = (affairId: any) => {
    dispatch(deleteAffair(affairId));
  };

  return (
    <Box p={5} m={5} bgColor={'gray.100'} borderRadius={5}>
      <Heading mb={5} alignContent={'center'} fontSize={'2xl'}>
        {labels.affairList}
      </Heading>
      {error && <Text colorScheme='red'>Error: {error}</Text>}
      {affairsStatus === 'loading' && <AlfaSpinner />}
      <Table variant="striped">
        <Thead>
          <Tr>
            <Th>{labels.affairNumber}</Th>
            <Th>{labels.affairTitle}</Th>
            <Th>{labels.claimant}</Th>
            <Th>{labels.startDateOfWork}</Th>
            <Th>{labels.endDateOfWork}</Th>
            <Th>{labels.action}</Th>
          </Tr>
        </Thead>
        <Tbody>
          {affairs.map((affair) => (
            <Tr key={affair.affairId}>
              <Td>{affair.affairNumber}</Td>
              <Td>{affair.title}</Td>
              <Td>{affair.claimant}</Td>
              <Td>{new Date(affair.startDate).toLocaleDateString()}</Td>
              <Td>{affair.endDate ? new Date(affair.endDate).toLocaleDateString() : 'N/A'}</Td>
              <Td>
                <HStack spacing={4}>
                  <Button
                    onClick={() => handleEdit(affair.affairId)}
                    colorScheme='teal'
                    shadow={'md'}
                  >
                    {labels.update}
                  </Button>
                  <Button
                    onClick={() => handleDelete(affair.affairId)}
                    colorScheme='red'
                    shadow={'md'}
                  >
                    {labels.delete}
                  </Button>
                </HStack>
              </Td>
            </Tr>
          ))}
        </Tbody>
      </Table>
    </Box>
  );
};

export default AffairList;
