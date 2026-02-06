import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  IconButton,
  TextField,
  Button,
  Divider,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Chip,
  List,
  ListItem,
  ListItemText,
  Tab,
  Tabs,
  CircularProgress,
} from '@mui/material';
import { Close, Save, Send, Description, Mic, MicOff, Download } from '@mui/icons-material';

interface ReportPanelProps {
  studyId: number;
  onClose: () => void;
}

interface ReportTemplate { id: number; name: string; modality?: string; templateContent: string; }
interface Report { id: number; title: string; status: string; findings?: string; impression?: string; recommendations?: string; createdAt: string; }

const ReportPanel: React.FC<ReportPanelProps> = ({ studyId, onClose }) => {
  const [activeTab, setActiveTab] = useState(0);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [reports, setReports] = useState<Report[]>([]);
  const [templates, setTemplates] = useState<ReportTemplate[]>([]);
  const [selectedTemplate, setSelectedTemplate] = useState<number | ''>('');
  const [title, setTitle] = useState('Radiology Report');
  const [findings, setFindings] = useState('');
  const [impression, setImpression] = useState('');
  const [recommendations, setRecommendations] = useState('');
  const [status, setStatus] = useState<'Draft' | 'Preliminary' | 'Final'>('Draft');
  const [isRecording, setIsRecording] = useState(false);
  const [activeField, setActiveField] = useState<'findings' | 'impression' | 'recommendations'>('findings');

  useEffect(() => { loadReports(); loadTemplates(); }, [studyId]);

  const loadReports = async () => {
    setLoading(true);
    try {
      const response = await fetch(`/api/reports/studies/${studyId}/reports`);
      const data = await response.json();
      setReports(data);
    } catch (e) { console.error(e); }
    finally { setLoading(false); }
  };

  const loadTemplates = async () => {
    try {
      const response = await fetch('/api/reports/templates');
      setTemplates(await response.json());
    } catch (e) { console.error(e); }
  };

  const applyTemplate = (templateId: number) => {
    const t = templates.find(t => t.id === templateId);
    if (t) {
      try {
        const c = JSON.parse(t.templateContent);
        setFindings(c.findings || ''); setImpression(c.impression || ''); setRecommendations(c.recommendations || '');
      } catch {}
    }
  };

  const saveReport = async () => {
    setSaving(true);
    try {
      const r = await fetch(`/api/reports/studies/${studyId}/reports`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ title, findings, impression, recommendations, status, templateId: selectedTemplate || null }),
      });
      if (r.ok) { loadReports(); setActiveTab(1); }
    } catch (e) { console.error(e); }
    finally { setSaving(false); }
  };

  const exportReport = async (reportId: number, format: string) => {
    try {
      const r = await fetch(`/api/reports/${reportId}/export/${format}`);
      const blob = await r.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a'); a.href = url;
      a.download = `report_${reportId}.${format === 'dicom-sr' ? 'dcm' : 'html'}`;
      a.click(); URL.revokeObjectURL(url);
    } catch (e) { console.error(e); }
  };

  const toggleVoice = () => {
    setIsRecording(!isRecording);
    if (!isRecording) {
      setTimeout(() => {
        const text = ' [Voice text]';
        if (activeField === 'findings') setFindings(p => p + text);
        else if (activeField === 'impression') setImpression(p => p + text);
        else setRecommendations(p => p + text);
        setIsRecording(false);
      }, 2000);
    }
  };

  const statusColor = (s: string): 'success' | 'warning' | 'default' => s === 'Final' ? 'success' : s === 'Preliminary' ? 'warning' : 'default';

  return (
    <Paper sx={{ width: 450, display: 'flex', flexDirection: 'column', borderLeft: '1px solid #333', backgroundColor: '#151515' }}>
      <Box sx={{ display: 'flex', alignItems: 'center', p: 1.5, borderBottom: '1px solid #333' }}>
        <Description sx={{ mr: 1, color: '#4fc3f7' }} />
        <Typography variant="subtitle1" sx={{ fontWeight: 600, flex: 1 }}>Reporting</Typography>
        <IconButton size="small" onClick={onClose}><Close /></IconButton>
      </Box>

      <Tabs value={activeTab} onChange={(_, v) => setActiveTab(v)} sx={{ borderBottom: '1px solid #333' }}>
        <Tab label="New Report" /><Tab label={`Reports (${reports.length})`} />
      </Tabs>

      {activeTab === 0 && (
        <Box sx={{ flex: 1, overflow: 'auto', p: 2 }}>
          <FormControl fullWidth size="small" sx={{ mb: 2 }}>
            <InputLabel>Template</InputLabel>
            <Select value={selectedTemplate} onChange={(e) => { setSelectedTemplate(e.target.value as number); applyTemplate(e.target.value as number); }} label="Template">
              <MenuItem value="">None</MenuItem>
              {templates.map(t => <MenuItem key={t.id} value={t.id}>{t.name}</MenuItem>)}
            </Select>
          </FormControl>

          <TextField fullWidth size="small" label="Title" value={title} onChange={e => setTitle(e.target.value)} sx={{ mb: 2 }} />

          <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
            <Typography variant="subtitle2" sx={{ flex: 1 }}>Findings</Typography>
            <IconButton size="small" onClick={() => { setActiveField('findings'); toggleVoice(); }} color={isRecording && activeField === 'findings' ? 'error' : 'default'}>
              {isRecording && activeField === 'findings' ? <MicOff /> : <Mic />}
            </IconButton>
          </Box>
          <TextField fullWidth multiline rows={4} value={findings} onChange={e => setFindings(e.target.value)} placeholder="Enter findings..." sx={{ mb: 2 }} />

          <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
            <Typography variant="subtitle2" sx={{ flex: 1 }}>Impression</Typography>
            <IconButton size="small" onClick={() => { setActiveField('impression'); toggleVoice(); }} color={isRecording && activeField === 'impression' ? 'error' : 'default'}>
              {isRecording && activeField === 'impression' ? <MicOff /> : <Mic />}
            </IconButton>
          </Box>
          <TextField fullWidth multiline rows={3} value={impression} onChange={e => setImpression(e.target.value)} placeholder="Enter impression..." sx={{ mb: 2 }} />

          <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
            <Typography variant="subtitle2" sx={{ flex: 1 }}>Recommendations</Typography>
            <IconButton size="small" onClick={() => { setActiveField('recommendations'); toggleVoice(); }} color={isRecording && activeField === 'recommendations' ? 'error' : 'default'}>
              {isRecording && activeField === 'recommendations' ? <MicOff /> : <Mic />}
            </IconButton>
          </Box>
          <TextField fullWidth multiline rows={2} value={recommendations} onChange={e => setRecommendations(e.target.value)} placeholder="Enter recommendations..." sx={{ mb: 2 }} />

          <FormControl fullWidth size="small" sx={{ mb: 2 }}>
            <InputLabel>Status</InputLabel>
            <Select value={status} onChange={e => setStatus(e.target.value as 'Draft' | 'Preliminary' | 'Final')} label="Status">
              <MenuItem value="Draft">Draft</MenuItem><MenuItem value="Preliminary">Preliminary</MenuItem><MenuItem value="Final">Final</MenuItem>
            </Select>
          </FormControl>

          <Divider sx={{ my: 2 }} />
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button variant="outlined" startIcon={<Save />} onClick={saveReport} disabled={saving} fullWidth>{saving ? <CircularProgress size={20} /> : 'Save'}</Button>
            <Button variant="contained" startIcon={<Send />} onClick={() => { setStatus('Final'); saveReport(); }} disabled={saving} fullWidth>Finalize</Button>
          </Box>
        </Box>
      )}

      {activeTab === 1 && (
        <Box sx={{ flex: 1, overflow: 'auto' }}>
          {loading ? <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}><CircularProgress /></Box> :
           reports.length === 0 ? <Box sx={{ p: 3, textAlign: 'center' }}><Typography color="text.secondary">No reports</Typography></Box> :
           <List>{reports.map(r => (
             <ListItem key={r.id} sx={{ borderBottom: '1px solid #333' }}>
               <ListItemText primary={<Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>{r.title}<Chip label={r.status} size="small" color={statusColor(r.status)} /></Box>} secondary={new Date(r.createdAt).toLocaleString()} />
               <IconButton size="small" onClick={() => exportReport(r.id, 'pdf')}><Download /></IconButton>
             </ListItem>
           ))}</List>}
        </Box>
      )}
    </Paper>
  );
};

export default ReportPanel;
