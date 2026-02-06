import React, { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useDropzone } from 'react-dropzone';
import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TablePagination,
  TextField,
  InputAdornment,
  Button,
  IconButton,
  Typography,
  Chip,
  CircularProgress,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  LinearProgress,
  Grid,
  Card,
  CardContent,
  Tooltip,
} from '@mui/material';
import {
  Search as SearchIcon,
  CloudUpload as UploadIcon,
  Visibility as ViewIcon,
  Delete as DeleteIcon,
  Refresh as RefreshIcon,
  FilterList as FilterIcon,
  Person as PersonIcon,
  CalendarToday as CalendarIcon,
  Description as DescriptionIcon,
  Share as ShareIcon,
  ContentCopy as CopyIcon,
} from '@mui/icons-material';
import { format } from 'date-fns';
import useAppStore from '../services/store';
import apiService from '../services/api';
import { Study } from '../types';

const StudyList: React.FC = () => {
  const navigate = useNavigate();
  const {
    studies,
    studiesLoading,
    studiesError,
    totalStudies,
    currentPage,
    pageSize,
    fetchStudies,
    selectStudy,
    uploadFiles,
    uploading,
  } = useAppStore();

  const [searchTerm, setSearchTerm] = useState('');
  const [uploadDialogOpen, setUploadDialogOpen] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [uploadSuccess, setUploadSuccess] = useState(false);
  const [filesToUpload, setFilesToUpload] = useState<File[]>([]);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);
  const [shareLink, setShareLink] = useState('');

  useEffect(() => {
    fetchStudies(1);
  }, []);

  const handlePageChange = (_: unknown, newPage: number) => {
    fetchStudies(newPage + 1);
  };

  const handleSearch = () => {
    const search: Record<string, string> = {};
    if (searchTerm) {
      // Search in multiple fields
      search.patientName = searchTerm;
    }
    fetchStudies(1, search);
  };

  const handleViewStudy = async (study: Study) => {
    await selectStudy(study.id);
    // Use encrypted UID if available, otherwise use study ID
    if (study.encryptedStudyUid) {
      navigate(`/view/${study.encryptedStudyUid}`);
    } else {
      navigate(`/viewer/${study.id}`);
    }
  };

  const handleShareStudy = async (study: Study, event: React.MouseEvent) => {
    event.stopPropagation();
    try {
      // Use encrypted UID from study if already available
      let encryptedUid = study.encryptedStudyUid;
      
      // If not available, fetch it
      if (!encryptedUid) {
        const result = await apiService.getEncryptedStudyUid(study.id);
        encryptedUid = result.encryptedUid;
      }
      
      const baseUrl = window.location.origin;
      const link = `${baseUrl}/view/${encryptedUid}`;
      setShareLink(link);
      setShareDialogOpen(true);
    } catch (error) {
      console.error('Error generating share link:', error);
    }
  };

  const handleCopyLink = () => {
    navigator.clipboard.writeText(shareLink)
      .then(() => {
        // Optional: Show success notification
        console.log('Link copied to clipboard');
      })
      .catch((err) => {
        console.error('Failed to copy link:', err);
      });
  };

  const onDrop = useCallback((acceptedFiles: File[]) => {
    const dicomFiles = acceptedFiles.filter(
      (file) =>
        file.name.toLowerCase().endsWith('.dcm') ||
        file.name.toLowerCase().endsWith('.dicom') ||
        !file.name.includes('.') // DICOM files often have no extension
    );
    setFilesToUpload((prev) => [...prev, ...dicomFiles]);
  }, []);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      'application/dicom': ['.dcm', '.dicom'],
    },
    multiple: true,
  });

  const handleUpload = async () => {
    if (filesToUpload.length === 0) return;

    setUploadError(null);
    setUploadSuccess(false);

    try {
      await uploadFiles(filesToUpload);
      setUploadSuccess(true);
      setFilesToUpload([]);
      setTimeout(() => {
        setUploadDialogOpen(false);
        setUploadSuccess(false);
      }, 2000);
    } catch (error: any) {
      setUploadError(error.message || 'Upload failed');
    }
  };

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return '-';
    try {
      return format(new Date(dateStr), 'MMM dd, yyyy');
    } catch {
      return dateStr;
    }
  };

  const getModalityColor = (modality?: string) => {
    const colors: Record<string, string> = {
      CT: '#4fc3f7',
      MR: '#81c784',
      CR: '#ffb74d',
      DX: '#ff8a65',
      US: '#ba68c8',
      MG: '#f48fb1',
      PT: '#90a4ae',
      NM: '#80deea',
    };
    return colors[modality || ''] || '#9e9e9e';
  };

  return (
    <Box sx={{ height: 'calc(100vh - 48px)', display: 'flex', flexDirection: 'column', p: 2 }}>
      {/* Header */}
      <Box sx={{ mb: 2, display: 'flex', alignItems: 'center', gap: 2 }}>
        <Typography variant="h5" sx={{ fontWeight: 600 }}>
          Study List
        </Typography>
        <Box sx={{ flexGrow: 1 }} />
        
        <TextField
          size="small"
          placeholder="Search patients..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon sx={{ color: 'text.secondary' }} />
              </InputAdornment>
            ),
          }}
          sx={{ width: 300 }}
        />
        
        <Tooltip title="Refresh">
          <IconButton onClick={() => fetchStudies(currentPage)} disabled={studiesLoading}>
            <RefreshIcon />
          </IconButton>
        </Tooltip>
        
        <Button
          variant="contained"
          startIcon={<UploadIcon />}
          onClick={() => setUploadDialogOpen(true)}
          sx={{
            background: 'linear-gradient(45deg, #4fc3f7, #29b6f6)',
            '&:hover': {
              background: 'linear-gradient(45deg, #29b6f6, #03a9f4)',
            },
          }}
        >
          Upload DICOM
        </Button>
      </Box>

      {/* Error Alert */}
      {studiesError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {studiesError}
        </Alert>
      )}

      {/* Stats Cards */}
      <Grid container spacing={2} sx={{ mb: 2 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card sx={{ backgroundColor: 'rgba(79, 195, 247, 0.1)', border: '1px solid rgba(79, 195, 247, 0.3)' }}>
            <CardContent sx={{ py: 1.5 }}>
              <Typography variant="caption" color="text.secondary">
                Total Studies
              </Typography>
              <Typography variant="h4" sx={{ color: '#4fc3f7', fontWeight: 600 }}>
                {totalStudies}
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Study Table */}
      <Paper sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        <TableContainer sx={{ flexGrow: 1 }}>
          {studiesLoading && <LinearProgress />}
          <Table stickyHeader size="small">
            <TableHead>
              <TableRow>
                <TableCell sx={{ fontWeight: 600, backgroundColor: '#1a1a1a' }}>Patient</TableCell>
                <TableCell sx={{ fontWeight: 600, backgroundColor: '#1a1a1a' }}>Study</TableCell>
                <TableCell sx={{ fontWeight: 600, backgroundColor: '#1a1a1a' }}>Date</TableCell>
                <TableCell sx={{ fontWeight: 600, backgroundColor: '#1a1a1a' }}>Modality</TableCell>
                <TableCell sx={{ fontWeight: 600, backgroundColor: '#1a1a1a' }}>Series</TableCell>
                <TableCell sx={{ fontWeight: 600, backgroundColor: '#1a1a1a' }}>Images</TableCell>
                <TableCell sx={{ fontWeight: 600, backgroundColor: '#1a1a1a' }} align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {studies.map((study) => (
                <TableRow
                  key={study.id}
                  hover
                  sx={{
                    cursor: 'pointer',
                    '&:hover': { backgroundColor: 'rgba(79, 195, 247, 0.08)' },
                  }}
                  onClick={() => handleViewStudy(study)}
                >
                  <TableCell>
                    <Box sx={{ display: 'flex', flexDirection: 'column' }}>
                      <Typography variant="body2" sx={{ fontWeight: 500 }}>
                        {study.patientName || 'Unknown'}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        ID: {study.patientId || '-'}
                      </Typography>
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">
                      {study.studyDescription || 'No description'}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {study.accessionNumber || '-'}
                    </Typography>
                  </TableCell>
                  <TableCell>{formatDate(study.studyDate)}</TableCell>
                  <TableCell>
                    <Chip
                      label="CT"
                      size="small"
                      sx={{
                        backgroundColor: getModalityColor('CT'),
                        color: '#000',
                        fontWeight: 600,
                      }}
                    />
                  </TableCell>
                  <TableCell>{study.numberOfSeries}</TableCell>
                  <TableCell>{study.numberOfInstances}</TableCell>
                  <TableCell align="right">
                    <Tooltip title="View Study">
                      <IconButton
                        size="small"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleViewStudy(study);
                        }}
                      >
                        <ViewIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Share Study">
                      <IconButton
                        size="small"
                        onClick={(e) => handleShareStudy(study, e)}
                      >
                        <ShareIcon />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
              {studies.length === 0 && !studiesLoading && (
                <TableRow>
                  <TableCell colSpan={7} align="center" sx={{ py: 8 }}>
                    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2 }}>
                      <DescriptionIcon sx={{ fontSize: 64, color: 'text.secondary' }} />
                      <Typography variant="h6" color="text.secondary">
                        No studies found
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Upload DICOM files to get started
                      </Typography>
                      <Button
                        variant="outlined"
                        startIcon={<UploadIcon />}
                        onClick={() => setUploadDialogOpen(true)}
                      >
                        Upload Files
                      </Button>
                    </Box>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>
        
        <TablePagination
          component="div"
          count={totalStudies}
          page={currentPage - 1}
          onPageChange={handlePageChange}
          rowsPerPage={pageSize}
          rowsPerPageOptions={[pageSize]}
          sx={{ borderTop: '1px solid #333' }}
        />
      </Paper>

      {/* Upload Dialog */}
      <Dialog
        open={uploadDialogOpen}
        onClose={() => !uploading && setUploadDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Upload DICOM Files</DialogTitle>
        <DialogContent>
          {uploadSuccess ? (
            <Alert severity="success" sx={{ mb: 2 }}>
              Files uploaded successfully!
            </Alert>
          ) : uploadError ? (
            <Alert severity="error" sx={{ mb: 2 }}>
              {uploadError}
            </Alert>
          ) : null}

          <Box
            {...getRootProps()}
            sx={{
              border: '2px dashed',
              borderColor: isDragActive ? '#4fc3f7' : '#555',
              borderRadius: 2,
              p: 4,
              textAlign: 'center',
              cursor: 'pointer',
              transition: 'all 0.2s',
              backgroundColor: isDragActive ? 'rgba(79, 195, 247, 0.1)' : 'transparent',
              '&:hover': {
                borderColor: '#4fc3f7',
                backgroundColor: 'rgba(79, 195, 247, 0.05)',
              },
            }}
          >
            <input {...getInputProps()} />
            <UploadIcon sx={{ fontSize: 48, color: '#4fc3f7', mb: 2 }} />
            <Typography variant="h6">
              {isDragActive ? 'Drop files here' : 'Drag & drop DICOM files'}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              or click to select files
            </Typography>
          </Box>

          {filesToUpload.length > 0 && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="subtitle2" sx={{ mb: 1 }}>
                Files to upload: {filesToUpload.length}
              </Typography>
              <Box sx={{ maxHeight: 150, overflow: 'auto' }}>
                {filesToUpload.slice(0, 10).map((file, index) => (
                  <Typography key={index} variant="caption" display="block" color="text.secondary">
                    {file.name}
                  </Typography>
                ))}
                {filesToUpload.length > 10 && (
                  <Typography variant="caption" color="text.secondary">
                    ... and {filesToUpload.length - 10} more files
                  </Typography>
                )}
              </Box>
              <Button
                size="small"
                color="error"
                onClick={() => setFilesToUpload([])}
                sx={{ mt: 1 }}
              >
                Clear All
              </Button>
            </Box>
          )}

          {uploading && (
            <Box sx={{ mt: 2, display: 'flex', alignItems: 'center', gap: 2 }}>
              <CircularProgress size={24} />
              <Typography>Uploading...</Typography>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setUploadDialogOpen(false)} disabled={uploading}>
            Cancel
          </Button>
          <Button
            variant="contained"
            onClick={handleUpload}
            disabled={filesToUpload.length === 0 || uploading}
          >
            Upload {filesToUpload.length > 0 && `(${filesToUpload.length} files)`}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Share Dialog */}
      <Dialog
        open={shareDialogOpen}
        onClose={() => setShareDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Share Study Link</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Share this encrypted link to allow others to view this study directly:
          </Typography>
          <TextField
            fullWidth
            value={shareLink}
            InputProps={{
              readOnly: true,
              endAdornment: (
                <Tooltip title="Copy to clipboard">
                  <IconButton onClick={handleCopyLink} edge="end">
                    <CopyIcon />
                  </IconButton>
                </Tooltip>
              ),
            }}
            sx={{ mb: 2 }}
          />
          <Alert severity="info">
            This link contains an encrypted Study UID for secure access.
          </Alert>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShareDialogOpen(false)}>Close</Button>
          <Button variant="contained" onClick={handleCopyLink} startIcon={<CopyIcon />}>
            Copy Link
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default StudyList;
