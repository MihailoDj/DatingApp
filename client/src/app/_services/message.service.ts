import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { Message } from '../_models/message';
import { UserParams } from '../_models/userParams';
import { getPaginatedResult } from './paginationHelper';

@Injectable({
  providedIn: 'root',
})
export class MessageService {
  baseUrl: string = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getMessages(pageNumber: number, pageSize: number, container) {
    let params = this.formParamsQuery(pageNumber, pageSize, container);

    return getPaginatedResult<Message[]>(
      this.baseUrl + 'messages',
      params,
      this.http
    );
  }

  formParamsQuery(pageNumber: number, pageSize: number, container: string) {
    let params = new HttpParams();

    params = params
      .append('pageNumber', pageNumber.toString())
      .append('pageSize', pageSize.toString())
      .append('Container', container);

    return params;
  }

  getMessageThread(username: string) {
    return this.http.get<Message[]>(
      this.baseUrl + 'messages/thread/' + username
    );
  }

  sendMessage(username: string, content: string) {
    return this.http.post<Message>(this.baseUrl + 'messages', {
      recipientUsername: username,
      content,
    });
  }
}