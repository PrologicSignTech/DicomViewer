import axios, { AxiosInstance } from 'axios';
import {
  Study,
  StudyDetail,
  Series,
  SeriesDetail,
  Instance,
  InstanceDetail,
  PagedResult,
  StudySearch,
  UploadResult,
  DicomTag,
  Annotation,
  Measurement,
  CreateAnnotation,
  CreateMeasurement,
  HangingProtocol,
  ExportRequest,
} from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:65319/api';

class ApiService {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  // Studies
  async searchStudies(params: StudySearch): Promise<PagedResult<Study>> {
    const response = await this.client.get('/studies', { params });
    return response.data;
  }

  async getStudyById(id: number): Promise<StudyDetail> {
    const response = await this.client.get(`/studies/${id}`);
    return response.data;
  }

  async getStudyByUid(uid: string): Promise<StudyDetail> {
    const response = await this.client.get(`/studies/uid/${uid}`);
    return response.data;
  }

  async getStudyByEncryptedUid(encryptedUid: string): Promise<StudyDetail> {
    const response = await this.client.get(`/studies/encrypted/${encryptedUid}`);
    return response.data;
  }

  async getEncryptedStudyUid(studyId: number): Promise<{ encryptedUid: string; studyInstanceUid: string }> {
    const response = await this.client.get(`/studies/${studyId}/encrypt-uid`);
    return response.data;
  }

  async uploadFiles(files: File[]): Promise<UploadResult> {
    const formData = new FormData();
    files.forEach((file) => formData.append('files', file));

    const response = await this.client.post('/studies/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  }

  async deleteStudy(id: number): Promise<void> {
    await this.client.delete(`/studies/${id}`);
  }

  // Series
  async getSeriesById(id: number): Promise<SeriesDetail> {
    const response = await this.client.get(`/series/${id}`);
    return response.data;
  }

  async getSeriesByUid(uid: string): Promise<SeriesDetail> {
    const response = await this.client.get(`/series/uid/${uid}`);
    return response.data;
  }

  async getSeriesInstances(seriesId: number): Promise<Instance[]> {
    const response = await this.client.get(`/series/${seriesId}/instances`);
    return response.data;
  }

  getSeriesThumbnailUrl(seriesId: number, width = 128, height = 128): string {
    return `${API_BASE_URL}/series/${seriesId}/thumbnail?width=${width}&height=${height}`;
  }

  getMprUrl(seriesId: number, plane: string, sliceIndex: number, wc?: number, ww?: number): string {
    let url = `${API_BASE_URL}/series/${seriesId}/mpr?plane=${plane}&sliceIndex=${sliceIndex}`;
    if (wc !== undefined) url += `&windowCenter=${wc}`;
    if (ww !== undefined) url += `&windowWidth=${ww}`;
    return url;
  }

  // Instances
  async getInstanceById(id: number): Promise<InstanceDetail> {
    const response = await this.client.get(`/instances/${id}`);
    return response.data;
  }

  async getInstanceByUid(uid: string): Promise<InstanceDetail> {
    const response = await this.client.get(`/instances/uid/${uid}`);
    return response.data;
  }

  getImageUrl(instanceId: number, frame = 0, wc?: number, ww?: number, invert = false): string {
    let url = `${API_BASE_URL}/instances/${instanceId}/image?frame=${frame}`;
    if (wc !== undefined) url += `&windowCenter=${wc}`;
    if (ww !== undefined) url += `&windowWidth=${ww}`;
    if (invert) url += `&invert=true`;
    return url;
  }

  getThumbnailUrl(instanceId: number, width = 128, height = 128): string {
    return `${API_BASE_URL}/instances/${instanceId}/thumbnail?width=${width}&height=${height}`;
  }

  async getDicomTags(instanceId: number): Promise<{ sopInstanceUid: string; tags: DicomTag[] }> {
    const response = await this.client.get(`/instances/${instanceId}/tags`);
    return response.data;
  }

  async getPixelValue(instanceId: number, x: number, y: number, frame = 0): Promise<{
    x: number;
    y: number;
    value: number;
    unit: string;
  }> {
    const response = await this.client.get(`/instances/${instanceId}/pixel-value`, {
      params: { x, y, frame },
    });
    return response.data;
  }

  getDicomFileUrl(instanceId: number): string {
    return `${API_BASE_URL}/instances/${instanceId}/dicom`;
  }

  // Measurements
  async getMeasurements(instanceId: number): Promise<Measurement[]> {
    const response = await this.client.get(`/instances/${instanceId}/measurements`);
    return response.data;
  }

  async createMeasurement(instanceId: number, data: CreateMeasurement): Promise<Measurement> {
    const response = await this.client.post(`/instances/${instanceId}/measurements`, data);
    return response.data;
  }

  async updateMeasurement(measurementId: number, data: CreateMeasurement): Promise<Measurement> {
    const response = await this.client.put(`/instances/measurements/${measurementId}`, data);
    return response.data;
  }

  async deleteMeasurement(measurementId: number): Promise<void> {
    await this.client.delete(`/instances/measurements/${measurementId}`);
  }

  // Annotations
  async getAnnotations(instanceId: number): Promise<Annotation[]> {
    const response = await this.client.get(`/instances/${instanceId}/annotations`);
    return response.data;
  }

  async createAnnotation(instanceId: number, data: CreateAnnotation): Promise<Annotation> {
    const response = await this.client.post(`/instances/${instanceId}/annotations`, data);
    return response.data;
  }

  async updateAnnotation(annotationId: number, data: CreateAnnotation): Promise<Annotation> {
    const response = await this.client.put(`/instances/annotations/${annotationId}`, data);
    return response.data;
  }

  async deleteAnnotation(annotationId: number): Promise<void> {
    await this.client.delete(`/instances/annotations/${annotationId}`);
  }

  // Export
  async exportImage(instanceId: number, request: ExportRequest): Promise<Blob> {
    const response = await this.client.post(`/instances/${instanceId}/export`, request, {
      responseType: 'blob',
    });
    return response.data;
  }
}

export const apiService = new ApiService();
export default apiService;
