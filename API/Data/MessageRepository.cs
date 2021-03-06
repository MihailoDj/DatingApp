using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public MessageRepository(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public void AddMessage(Message message)
        {
            _context.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _context.Messages.Remove(message);
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages
                .Include(u => u.Sender)
                .Include(u => u.Recipient)
                .SingleOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = _context.Messages.OrderByDescending(m => m.DateSent).AsQueryable();

            query = messageParams.Container switch
            {
                "Inbox" => query.Where(
                    u => u.Recipient.UserName == messageParams.Username && !u.RecipientDeleted),
                "Outbox" => query.Where(
                    u => u.Sender.UserName == messageParams.Username && !u.SenderDeleted),
                _ => query.Where(
                    u => u.Recipient.UserName == messageParams.Username 
                    && u.DateRead == null && !u.RecipientDeleted)
            };

            var messages = query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider);

            return await PagedList<MessageDto>
                .CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(
            string currentUserUsername, string recipientUsername)
        {
            var messages = await _context.Messages
                .Include(u => u.Sender).ThenInclude(p => p.Photos)
                .Include(u => u.Recipient).ThenInclude(p => p.Photos)
                .Where(m => m.Recipient.UserName == currentUserUsername && !m.RecipientDeleted &&
                        m.Sender.UserName == recipientUsername ||
                        m.Recipient.UserName == recipientUsername &&
                        m.Sender.UserName == currentUserUsername && !m.SenderDeleted)
                .OrderBy(m => m.DateSent)
                .ToListAsync();

            var unreadMessages = messages
                .Where(m => m.DateRead == null && m.Recipient.UserName == currentUserUsername)
                .ToList();

            if (unreadMessages.Any())
            {
                foreach (var uMessage in unreadMessages)
                {
                    uMessage.DateRead = DateTime.Now;
                }

                await _context.SaveChangesAsync();
            }

            return _mapper.Map<IEnumerable<MessageDto>>(messages);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}