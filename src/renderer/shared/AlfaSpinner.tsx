import React from 'react'
import {CircularProgress, Box} from '@chakra-ui/react'

function AlfaSpinner() {
  return (
    <Box w="100vw" h="100vh" display="flex" justifyContent="center" alignItems="center">
        <CircularProgress color="purple" isIndeterminate />
    </Box>
  )
}

export default AlfaSpinner