import React, { useEffect, useState } from 'react';
import { Modal, ModalOverlay, ModalContent, ModalHeader, ModalFooter, ModalBody, ModalCloseButton, Button, Input, Table, Tbody, Tr, Td, Th, Thead } from '@chakra-ui/react';
import { labels } from '../arabic.labels';

export interface Entity {
  id: string | number;
  label: string;
}

interface GenericLookupDialogProps {
  isOpen: boolean;
  onClose: () => void;
  entities: Entity[]; // This will hold the list of entities like agreements
  onSelect: (entity: Entity) => void; // Callback to send the selected entity back to the form
  title: string; // Dialog title, e.g., "Select Agreement"
}

const GenericLookupDialog: React.FC<GenericLookupDialogProps> = ({ isOpen, onClose, entities, onSelect, title }) => {
  const [filteredEntities, setFilteredEntities] = useState<Entity[]>(entities);
  const [search, setSearch] = useState<string>('');

  useEffect(() => {
    setFilteredEntities(
      entities.filter((entity) =>
        entity.label.toLowerCase().includes(search.toLowerCase())
      )
    );
  }, [search, entities]);

  const handleSelect = (entity: Entity) => {
    onSelect(entity);
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose}>
      <ModalOverlay />
      <ModalContent>
        <ModalHeader>{title}</ModalHeader>
        <ModalCloseButton />
        <ModalBody>
          <Input
            placeholder={labels.search}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            mb={4}
          />
          <Table variant="simple">
            <Thead>
              <Tr>
                <Th>ID</Th>
                <Th>Label</Th>
              </Tr>
            </Thead>
            <Tbody>
              {filteredEntities.map((entity) => (
                <Tr key={entity.id} onClick={() => handleSelect(entity)} cursor="pointer" _hover={{ bg: 'gray.100' }}>
                  <Td>{entity.id}</Td>
                  <Td>{entity.label}</Td>
                </Tr>
              ))}
            </Tbody>
          </Table>
        </ModalBody>
        <ModalFooter>
          <Button colorScheme="blue" mr={3} onClick={onClose}>
            {labels.close}
          </Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
};

export default GenericLookupDialog;
