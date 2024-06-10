// ConventionComponent.jsx
import React from 'react';
import { Box, Heading, Text } from '@chakra-ui/react';

interface ConventionCollectiveProps {
  id: number,
  name: string,
  description: string 
}


const ConventionCollectiveComponent = ( ): React.JSX.Element => {
  const  conventionsCollectives=[
    {id: 1, name: 'Convention 1', description: 'Description 1'}, 
    {id: 2, name: 'Convention 2', description: 'Description 2'}, 
    {id: 3, name: 'Convention 3', description: 'Description 3'}]
    
  return (
    <Box>
      <Heading as="h2" size="lg" mb={4}>Conventions</Heading>
      {conventionsCollectives?.map((convention:{ id:number, name:string, description:string }) => (
        <Box key={convention.id} border="1px" borderColor="gray.200" p={4} mb={4}>
          <Heading as="h3" size="md">{convention.name}</Heading>
          <Text>{convention.description}</Text>
        </Box>
      ))}
    </Box>
  );
};

export default ConventionCollectiveComponent;
