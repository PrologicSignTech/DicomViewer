import React, { useState } from 'react';
import {
  Box,
  Typography,
  List,
  ListItem,
  ListItemButton,
  Collapse,
  Divider,
  Tooltip,
  CircularProgress,
} from '@mui/material';
import {
  ExpandLess,
  ExpandMore,
  Image as ImageIcon,
} from '@mui/icons-material';
import useAppStore from '../services/store';
import apiService from '../services/api';

const SeriesPanel: React.FC = () => {
  const {
    currentStudy,
    currentSeries,
    currentInstance,
    selectSeries,
    selectInstance,
  } = useAppStore();

  const [expandedSeries, setExpandedSeries] = useState<number | null>(null);
  const [loadingThumbnails, setLoadingThumbnails] = useState<Record<number, boolean>>({});

  const handleSeriesClick = async (seriesId: number) => {
    if (expandedSeries === seriesId) {
      setExpandedSeries(null);
    } else {
      setExpandedSeries(seriesId);
      await selectSeries(seriesId);
    }
  };

  const handleInstanceClick = async (instanceId: number) => {
    await selectInstance(instanceId);
  };

  if (!currentStudy) {
    return (
      <Box
        sx={{
          width: 200,
          backgroundColor: '#151515',
          borderRight: '1px solid #333',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          p: 2,
        }}
      >
        <ImageIcon sx={{ fontSize: 48, color: 'text.secondary', mb: 2 }} />
        <Typography variant="body2" color="text.secondary" textAlign="center">
          Select a study to view series
        </Typography>
      </Box>
    );
  }

  return (
    <Box
      sx={{
        width: 200,
        backgroundColor: '#151515',
        borderRight: '1px solid #333',
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden',
      }}
    >
      {/* Study Info Header */}
      <Box sx={{ p: 1.5, borderBottom: '1px solid #333' }}>
        <Typography variant="caption" color="text.secondary" display="block">
          Patient
        </Typography>
        <Typography variant="body2" sx={{ fontWeight: 500, mb: 0.5 }} noWrap>
          {currentStudy.patientName || 'Unknown'}
        </Typography>
        <Typography variant="caption" color="text.secondary" display="block">
          {currentStudy.studyDescription || 'No description'}
        </Typography>
      </Box>

      {/* Series List */}
      <Box sx={{ flex: 1, overflow: 'auto' }}>
        <List dense disablePadding>
          {currentStudy.series.map((series) => (
            <React.Fragment key={series.id}>
              <ListItem disablePadding>
                <ListItemButton
                  selected={currentSeries?.id === series.id}
                  onClick={() => handleSeriesClick(series.id)}
                  sx={{
                    py: 0.5,
                    '&.Mui-selected': {
                      backgroundColor: 'rgba(79, 195, 247, 0.15)',
                      borderLeft: '3px solid #4fc3f7',
                    },
                  }}
                >
                  <Box sx={{ flex: 1 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                      <Typography
                        variant="caption"
                        sx={{
                          backgroundColor: '#4fc3f7',
                          color: '#000',
                          px: 0.5,
                          borderRadius: 0.5,
                          fontWeight: 600,
                          fontSize: '10px',
                        }}
                      >
                        {series.modality || '?'}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        #{series.seriesNumber || '-'}
                      </Typography>
                    </Box>
                    <Tooltip title={series.seriesDescription || 'No description'}>
                      <Typography variant="body2" noWrap sx={{ fontSize: '11px', mt: 0.25 }}>
                        {series.seriesDescription || 'No description'}
                      </Typography>
                    </Tooltip>
                    <Typography variant="caption" color="text.secondary">
                      {series.numberOfInstances} images
                    </Typography>
                  </Box>
                  {expandedSeries === series.id ? <ExpandLess /> : <ExpandMore />}
                </ListItemButton>
              </ListItem>

              {/* Instance Thumbnails */}
              <Collapse in={expandedSeries === series.id} timeout="auto" unmountOnExit>
                <Box
                  sx={{
                    display: 'flex',
                    flexWrap: 'wrap',
                    gap: 0.5,
                    p: 1,
                    backgroundColor: '#0d0d0d',
                    maxHeight: 300,
                    overflowY: 'auto',
                  }}
                >
                  {currentSeries?.id === series.id &&
                    currentSeries.instances.map((instance, index) => (
                      <Tooltip
                        key={instance.id}
                        title={`Image ${instance.instanceNumber || index + 1}`}
                      >
                        <Box
                          onClick={() => handleInstanceClick(instance.id)}
                          sx={{
                            width: 56,
                            height: 56,
                            border: currentInstance?.id === instance.id
                              ? '2px solid #4fc3f7'
                              : '1px solid #444',
                            borderRadius: 0.5,
                            overflow: 'hidden',
                            cursor: 'pointer',
                            position: 'relative',
                            '&:hover': {
                              borderColor: '#4fc3f7',
                            },
                          }}
                        >
                          {loadingThumbnails[instance.id] ? (
                            <Box
                              sx={{
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                height: '100%',
                              }}
                            >
                              <CircularProgress size={16} />
                            </Box>
                          ) : (
                            <img
                              src={apiService.getThumbnailUrl(instance.id, 56, 56)}
                              alt={`Instance ${index + 1}`}
                              style={{
                                width: '100%',
                                height: '100%',
                                objectFit: 'cover',
                              }}
                              onError={(e) => {
                                (e.target as HTMLImageElement).style.display = 'none';
                              }}
                            />
                          )}
                          <Typography
                            variant="caption"
                            sx={{
                              position: 'absolute',
                              bottom: 0,
                              left: 0,
                              right: 0,
                              backgroundColor: 'rgba(0,0,0,0.7)',
                              color: '#fff',
                              fontSize: '9px',
                              textAlign: 'center',
                              py: 0.25,
                            }}
                          >
                            {instance.instanceNumber || index + 1}
                          </Typography>
                        </Box>
                      </Tooltip>
                    ))}
                </Box>
              </Collapse>
              <Divider />
            </React.Fragment>
          ))}
        </List>
      </Box>

      {/* Series Summary */}
      <Box sx={{ p: 1, borderTop: '1px solid #333' }}>
        <Typography variant="caption" color="text.secondary">
          {currentStudy.numberOfSeries} series • {currentStudy.numberOfInstances} images
        </Typography>
      </Box>
    </Box>
  );
};

export default SeriesPanel;
