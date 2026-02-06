import React, { useEffect, useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  TextField,
  InputAdornment,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  CircularProgress,
  Chip,
} from '@mui/material';
import {
  Search as SearchIcon,
  Close as CloseIcon,
  ContentCopy as CopyIcon,
} from '@mui/icons-material';
import apiService from '../services/api';
import { DicomTag } from '../types';

interface DicomTagsViewerProps {
  instanceId: number;
  onClose: () => void;
}

const DicomTagsViewer: React.FC<DicomTagsViewerProps> = ({ instanceId, onClose }) => {
  const [tags, setTags] = useState<DicomTag[]>([]);
  const [filteredTags, setFilteredTags] = useState<DicomTag[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadTags = async () => {
      setLoading(true);
      setError(null);
      try {
        const result = await apiService.getDicomTags(instanceId);
        setTags(result.tags);
        setFilteredTags(result.tags);
      } catch (err: any) {
        setError(err.message || 'Failed to load DICOM tags');
      } finally {
        setLoading(false);
      }
    };

    loadTags();
  }, [instanceId]);

  useEffect(() => {
    if (!searchTerm) {
      setFilteredTags(tags);
    } else {
      const term = searchTerm.toLowerCase();
      setFilteredTags(
        tags.filter(
          (tag) =>
            tag.tag.toLowerCase().includes(term) ||
            tag.name.toLowerCase().includes(term) ||
            tag.value.toLowerCase().includes(term)
        )
      );
    }
  }, [searchTerm, tags]);

  const handleCopyValue = (value: string) => {
    navigator.clipboard.writeText(value);
  };

  const getVrColor = (vr: string): string => {
    const colors: Record<string, string> = {
      PN: '#81c784', // Person Name
      DA: '#4fc3f7', // Date
      TM: '#4fc3f7', // Time
      UI: '#ffb74d', // UID
      LO: '#ce93d8', // Long String
      SH: '#ce93d8', // Short String
      CS: '#90a4ae', // Code String
      IS: '#f48fb1', // Integer String
      DS: '#f48fb1', // Decimal String
      US: '#80cbc4', // Unsigned Short
      UL: '#80cbc4', // Unsigned Long
      SQ: '#ffcc80', // Sequence
    };
    return colors[vr] || '#9e9e9e';
  };

  return (
    <Paper
      sx={{
        width: 450,
        display: 'flex',
        flexDirection: 'column',
        borderLeft: '1px solid #333',
        backgroundColor: '#151515',
      }}
    >
      {/* Header */}
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          p: 1.5,
          borderBottom: '1px solid #333',
        }}
      >
        <Typography variant="subtitle1" sx={{ fontWeight: 600, flex: 1 }}>
          DICOM Tags
        </Typography>
        <IconButton size="small" onClick={onClose}>
          <CloseIcon />
        </IconButton>
      </Box>

      {/* Search */}
      <Box sx={{ p: 1.5, borderBottom: '1px solid #333' }}>
        <TextField
          size="small"
          fullWidth
          placeholder="Search tags..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon sx={{ color: 'text.secondary' }} />
              </InputAdornment>
            ),
          }}
        />
        <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, display: 'block' }}>
          {filteredTags.length} of {tags.length} tags
        </Typography>
      </Box>

      {/* Tags Table */}
      <TableContainer sx={{ flex: 1, overflow: 'auto' }}>
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
            <CircularProgress />
          </Box>
        ) : error ? (
          <Box sx={{ p: 2, textAlign: 'center' }}>
            <Typography color="error">{error}</Typography>
          </Box>
        ) : (
          <Table size="small" stickyHeader>
            <TableHead>
              <TableRow>
                <TableCell sx={{ backgroundColor: '#1a1a1a', fontWeight: 600 }}>Tag</TableCell>
                <TableCell sx={{ backgroundColor: '#1a1a1a', fontWeight: 600 }}>VR</TableCell>
                <TableCell sx={{ backgroundColor: '#1a1a1a', fontWeight: 600 }}>Name</TableCell>
                <TableCell sx={{ backgroundColor: '#1a1a1a', fontWeight: 600 }}>Value</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredTags.map((tag, index) => (
                <TableRow
                  key={`${tag.tag}-${index}`}
                  hover
                  sx={{
                    '&:hover': {
                      backgroundColor: 'rgba(79, 195, 247, 0.08)',
                    },
                  }}
                >
                  <TableCell sx={{ fontFamily: 'monospace', fontSize: '11px' }}>
                    {tag.tag}
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={tag.vr}
                      size="small"
                      sx={{
                        backgroundColor: getVrColor(tag.vr),
                        color: '#000',
                        fontWeight: 600,
                        fontSize: '10px',
                        height: 20,
                      }}
                    />
                  </TableCell>
                  <TableCell sx={{ fontSize: '11px', maxWidth: 150 }}>
                    <Typography variant="inherit" noWrap title={tag.name}>
                      {tag.name}
                    </Typography>
                  </TableCell>
                  <TableCell sx={{ fontSize: '11px', maxWidth: 150 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                      <Typography
                        variant="inherit"
                        noWrap
                        title={tag.value}
                        sx={{ flex: 1 }}
                      >
                        {tag.value || '(empty)'}
                      </Typography>
                      {tag.value && (
                        <IconButton
                          size="small"
                          onClick={() => handleCopyValue(tag.value)}
                          sx={{ opacity: 0.5, '&:hover': { opacity: 1 } }}
                        >
                          <CopyIcon sx={{ fontSize: 14 }} />
                        </IconButton>
                      )}
                    </Box>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </TableContainer>

      {/* Common Tags Quick View */}
      <Box sx={{ p: 1.5, borderTop: '1px solid #333', backgroundColor: '#1a1a1a' }}>
        <Typography variant="caption" color="text.secondary" display="block" sx={{ mb: 1 }}>
          Quick Info
        </Typography>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
          {[
            { label: 'Patient', tag: '(0010,0010)' },
            { label: 'Modality', tag: '(0008,0060)' },
            { label: 'Study Date', tag: '(0008,0020)' },
            { label: 'Series', tag: '(0008,103E)' },
          ].map((item) => {
            const found = tags.find((t) => t.tag.includes(item.tag.replace(/[()]/g, '')));
            return (
              <Chip
                key={item.label}
                label={`${item.label}: ${found?.value || 'N/A'}`}
                size="small"
                variant="outlined"
                sx={{ fontSize: '10px' }}
              />
            );
          })}
        </Box>
      </Box>
    </Paper>
  );
};

export default DicomTagsViewer;
