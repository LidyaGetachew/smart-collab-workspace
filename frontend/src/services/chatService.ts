import * as signalR from '@microsoft/signalr';

export interface ChatMessage {
  id: string;
  workspaceId: string;
  userId: string;
  userName: string;
  userEmail: string;
  userAvatar?: string;
  message: string;
  messageType: string;
  sentAt: string;
  timeAgo: string;
}

class ChatService {
  private connection: signalR.HubConnection | null = null;
  private messageCallbacks: ((message: ChatMessage) => void)[] = [];
  private historyCallbacks: ((messages: ChatMessage[]) => void)[] = [];
  private unreadCallbacks: ((count: number) => void)[] = [];
  private typingCallbacks: ((userId: string, userName: string, isTyping: boolean) => void)[] = [];

  async connect(workspaceId: string, token: string): Promise<boolean> {
    await this.disconnect();

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${process.env.REACT_APP_CHAT_URL || 'http://localhost:8000'}/hubs/chat`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    this.connection = connection;

    connection.on('ReceiveMessage', (message: ChatMessage) => {
      this.messageCallbacks.forEach(cb => cb(message));
    });

    connection.on('ChatHistory', (messages: ChatMessage[]) => {
      this.historyCallbacks.forEach(cb => cb(messages));
    });

    connection.on('UnreadCount', (count: number) => {
      this.unreadCallbacks.forEach(cb => cb(count));
    });

    connection.on('UserTyping', (userId: string, userName: string, isTyping: boolean) => {
      this.typingCallbacks.forEach(cb => cb(userId, userName, isTyping));
    });

    try {
      await connection.start();
      // Only join if this is still the active connection
      if (this.connection === connection) {
        await this.joinWorkspace(workspaceId);
        return true;
      }
      return false;
    } catch (err: any) {
      if (err.name === 'AbortError' || err.message?.includes('stopped during negotiation')) {
        console.log('SignalR connection stopped during negotiation. This is normal in React StrictMode.');
        return false;
      } else {
        throw err;
      }
    }
  }

  async joinWorkspace(workspaceId: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('JoinWorkspaceRoom', workspaceId);
    }
  }

  async leaveWorkspace(workspaceId: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('LeaveWorkspaceRoom', workspaceId);
    }
  }

  async sendMessage(workspaceId: string, message: string): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('SendMessage', workspaceId, message);
    }
  }

  async loadHistory(workspaceId: string, skip: number = 0, limit: number = 50): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('LoadHistory', workspaceId, skip, limit);
    }
  }

  async sendTyping(workspaceId: string, isTyping: boolean): Promise<void> {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('Typing', workspaceId, isTyping);
    }
  }

  onMessage(callback: (message: ChatMessage) => void): () => void {
    this.messageCallbacks.push(callback);
    return () => {
      this.messageCallbacks = this.messageCallbacks.filter(cb => cb !== callback);
    };
  }

  onHistory(callback: (messages: ChatMessage[]) => void): () => void {
    this.historyCallbacks.push(callback);
    return () => {
      this.historyCallbacks = this.historyCallbacks.filter(cb => cb !== callback);
    };
  }

  onUnreadCount(callback: (count: number) => void): () => void {
    this.unreadCallbacks.push(callback);
    return () => {
      this.unreadCallbacks = this.unreadCallbacks.filter(cb => cb !== callback);
    };
  }

  onTyping(callback: (userId: string, userName: string, isTyping: boolean) => void): () => void {
    this.typingCallbacks.push(callback);
    return () => {
      this.typingCallbacks = this.typingCallbacks.filter(cb => cb !== callback);
    };
  }

  async disconnect(): Promise<void> {
    const conn = this.connection;
    this.connection = null;
    
    // Clear all callbacks to prevent memory leaks on remount
    this.messageCallbacks = [];
    this.historyCallbacks = [];
    this.unreadCallbacks = [];
    this.typingCallbacks = [];

    if (conn) {
      await conn.stop();
    }
  }
}

export const chatService = new ChatService();