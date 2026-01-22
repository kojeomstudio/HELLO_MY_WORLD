using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharedProtocol
{
    /// <summary>
    /// Protocol validation utilities for protobuf messages
    /// Provides comprehensive validation for protocol contracts
    /// </summary>
    public static class ProtocolValidation
    {
        private static readonly Dictionary<Type, ProtocolValidator> _validators = new();
        private static readonly List<ValidationRule> _globalRules = new();
        private static bool _initialized = false;
        
        /// <summary>
        /// Initializes protocol validation system
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            
            // Register default validators
            RegisterDefaultValidators();
            
            // Register global validation rules
            RegisterGlobalRules();
            
            _initialized = true;
        }
        
        /// <summary>
        /// Validates a protocol message
        /// </summary>
        public static ValidationResult ValidateMessage(object message)
        {
            if (message == null)
            {
                return ValidationResult.Failure("Message cannot be null");
            }
            
            Initialize();
            
            Type messageType = message.GetType();
            
            // Get or create validator for message type
            if (!_validators.TryGetValue(messageType, out var validator))
            {
                validator = CreateValidator(messageType);
                _validators[messageType] = validator;
            }
            
            // Validate message
            return validator.Validate(message);
        }
        
        /// <summary>
        /// Validates protocol implementation
        /// </summary>
        public static ValidationResult ValidateProtocolImplementation()
        {
            Initialize();
            
            var errors = new List<string>();
            var warnings = new List<string>();
            
            // Check for required message types
            CheckRequiredMessageTypes(errors, warnings);
            
            // Check for required enums
            CheckRequiredEnums(errors, warnings);
            
            // Check for namespace consistency
            CheckNamespaceConsistency(errors, warnings);
            
            // Check for version compatibility
            CheckVersionCompatibility(errors, warnings);
            
            if (errors.Any())
            {
                return ValidationResult.Failure(string.Join("; ", errors));
            }
            
            if (warnings.Any())
            {
                return ValidationResult.Warning(string.Join("; ", warnings));
            }
            
            return ValidationResult.Success();
        }
        
        /// <summary>
        /// Validates protocol bindings
        /// </summary>
        public static ValidationResult ValidateBindings()
        {
            Initialize();
            
            var errors = new List<string>();
            var warnings = new List<string>();
            
            // Check message handler bindings
            CheckMessageHandlerBindings(errors, warnings);
            
            // Check message type mappings
            CheckMessageTypeMappings(errors, warnings);
            
            if (errors.Any())
            {
                return ValidationResult.Failure(string.Join("; ", errors));
            }
            
            if (warnings.Any())
            {
                return ValidationResult.Warning(string.Join("; ", warnings));
            }
            
            return ValidationResult.Success();
        }
        
        /// <summary>
        /// Validates enhanced protocol contracts
        /// </summary>
        public static ValidationResult ValidateEnhancedContracts()
        {
            Initialize();
            
            var errors = new List<string>();
            var warnings = new List<string>();
            
            // Check enhanced protocol messages
            CheckEnhancedProtocolMessages(errors, warnings);
            
            // Check enhanced protocol enums
            CheckEnhancedProtocolEnums(errors, warnings);
            
            // Check enhanced protocol versioning
            CheckEnhancedProtocolVersioning(errors, warnings);
            
            if (errors.Any())
            {
                return ValidationResult.Failure(string.Join("; ", errors));
            }
            
            if (warnings.Any())
            {
                return ValidationResult.Warning(string.Join("; ", warnings));
            }
            
            return ValidationResult.Success();
        }
        
        private static void RegisterDefaultValidators()
        {
            // Register validators for common message types
            // This would be extended with specific validators for each message type
        }
        
        private static void RegisterGlobalRules()
        {
            // Register global validation rules
            _globalRules.Add(new NonNullRule());
            _globalRules.Add(new StringLengthRule());
            _globalRules.Add(new NumericRangeRule());
            _globalRules.Add(new CollectionNotEmptyRule());
        }
        
        private static ProtocolValidator CreateValidator(Type messageType)
        {
            var validator = new ProtocolValidator(messageType);
            
            // Add field validators based on message type
            PropertyInfo[] properties = messageType.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                AddFieldValidator(validator, property);
            }
            
            return validator;
        }
        
        private static void AddFieldValidator(ProtocolValidator validator, PropertyInfo property)
        {
            // Add appropriate validator based on property type
            Type propertyType = property.PropertyType;
            
            if (propertyType == typeof(string))
            {
                validator.AddFieldValidator(new StringFieldValidator(property));
            }
            else if (propertyType == typeof(int) || propertyType == typeof(long))
            {
                validator.AddFieldValidator(new NumericFieldValidator(property));
            }
            else if (propertyType.IsGenericType && 
                     propertyType.GetGenericTypeDefinition() == typeof(RepeatedField<>))
            {
                validator.AddFieldValidator(new CollectionFieldValidator(property));
            }
        }
        
        private static void CheckRequiredMessageTypes(List<string> errors, List<string> warnings)
        {
            // Check for required message types in EnhancedMinecraftProtocol
            var requiredTypes = new[]
            {
                "PlayerInfoRequest",
                "PlayerInfoResponse",
                "InventoryRequest",
                "InventoryResponse",
                "BlockBreakRequest",
                "BlockBreakResponse",
                "ChunkDataRequest",
                "ChunkDataResponse"
            };
            
            foreach (string typeName in requiredTypes)
            {
                Type type = Type.GetType($"EnhancedMinecraftProtocol.{typeName}");
                if (type == null)
                {
                    errors.Add($"Required message type not found: {typeName}");
                }
            }
        }
        
        private static void CheckRequiredEnums(List<string> errors, List<string> warnings)
        {
            // Check for required enums in EnhancedMinecraftProtocol
            var requiredEnums = new[]
            {
                "BlockType",
                "EntityType",
                "DamageType",
                "EffectType",
                "ChatType",
                "WeatherType"
            };
            
            foreach (string enumName in requiredEnums)
            {
                Type type = Type.GetType($"EnhancedMinecraftProtocol.{enumName}");
                if (type == null || !type.IsEnum)
                {
                    errors.Add($"Required enum not found: {enumName}");
                }
            }
        }
        
        private static void CheckNamespaceConsistency(List<string> errors, List<string> warnings)
        {
            // Check namespace consistency across protocol files
            var namespaces = new[]
            {
                "EnhancedMinecraftProtocol",
                "Game.World",
                "Game.Auth",
                "Game.Move",
                "Game.Chat",
                "Game.Diag"
            };
            
            foreach (string ns in namespaces)
            {
                try
                {
                    Assembly.GetExecutingAssembly().GetTypes()
                        .FirstOrDefault(t => t.Namespace == ns);
                }
                catch (Exception ex)
                {
                    errors.Add($"Namespace check failed for {ns}: {ex.Message}");
                }
            }
        }
        
        private static void CheckVersionCompatibility(List<string> errors, List<string> warnings)
        {
            // Check version compatibility between client and server
            // This would be extended with actual version checking logic
            warnings.Add("Version compatibility check not fully implemented");
        }
        
        private static void CheckMessageHandlerBindings(List<string> errors, List<string> warnings)
        {
            // Check if all message types have corresponding handlers
            // This would be extended with actual handler binding checking
            warnings.Add("Message handler binding check not fully implemented");
        }
        
        private static void CheckMessageTypeMappings(List<string> errors, List<string> warnings)
        {
            // Check if all message types are properly mapped
            // This would be extended with actual type mapping checking
            warnings.Add("Message type mapping check not fully implemented");
        }
        
        private static void CheckEnhancedProtocolMessages(List<string> errors, List<string> warnings)
        {
            // Check enhanced protocol message structure
            // This would be extended with actual message structure checking
            warnings.Add("Enhanced protocol message check not fully implemented");
        }
        
        private static void CheckEnhancedProtocolEnums(List<string> errors, List<string> warnings)
        {
            // Check enhanced protocol enum values
            // This would be extended with actual enum value checking
            warnings.Add("Enhanced protocol enum check not fully implemented");
        }
        
        private static void CheckEnhancedProtocolVersioning(List<string> errors, List<string> warnings)
        {
            // Check enhanced protocol versioning
            // This would be extended with actual versioning checking
            warnings.Add("Enhanced protocol versioning check not fully implemented");
        }
    }
    
    /// <summary>
    /// Protocol validator for a specific message type
    /// </summary>
    public class ProtocolValidator
    {
        private readonly Type _messageType;
        private readonly List<IFieldValidator> _fieldValidators = new();
        
        public ProtocolValidator(Type messageType)
        {
            _messageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        }
        
        public void AddFieldValidator(IFieldValidator validator)
        {
            _fieldValidators.Add(validator);
        }
        
        public ValidationResult Validate(object message)
        {
            if (message == null)
            {
                return ValidationResult.Failure("Message cannot be null");
            }
            
            if (message.GetType() != _messageType)
            {
                return ValidationResult.Failure($"Message type mismatch. Expected: {_messageType.Name}, Got: {message.GetType().Name}");
            }
            
            var errors = new List<string>();
            
            // Validate each field
            foreach (var validator in _fieldValidators)
            {
                ValidationResult result = validator.Validate(message);
                if (!result.IsValid)
                {
                    errors.Add(result.ErrorMessage);
                }
            }
            
            if (errors.Any())
            {
                return ValidationResult.Failure(string.Join("; ", errors));
            }
            
            return ValidationResult.Success();
        }
    }
    
    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public bool IsWarning { get; private set; }
        public string ErrorMessage { get; private set; }
        
        private ValidationResult(bool isValid, bool isWarning, string errorMessage)
        {
            IsValid = isValid;
            IsWarning = isWarning;
            ErrorMessage = errorMessage;
        }
        
        public static ValidationResult Success()
        {
            return new ValidationResult(true, false, null);
        }
        
        public static ValidationResult Failure(string errorMessage)
        {
            return new ValidationResult(false, false, errorMessage);
        }
        
        public static ValidationResult Warning(string warningMessage)
        {
            return new ValidationResult(true, true, warningMessage);
        }
    }
    
    /// <summary>
    /// Field validator interface
    /// </summary>
    public interface IFieldValidator
    {
        ValidationResult Validate(object message);
    }
    
    /// <summary>
    /// String field validator
    /// </summary>
    public class StringFieldValidator : IFieldValidator
    {
        private readonly PropertyInfo _property;
        
        public StringFieldValidator(PropertyInfo property)
        {
            _property = property ?? throw new ArgumentNullException(nameof(property));
        }
        
        public ValidationResult Validate(object message)
        {
            string value = _property.GetValue(message) as string;
            
            if (value == null)
            {
                // Null might be valid for optional fields
                return ValidationResult.Success();
            }
            
            // Check string length
            if (value.Length > 1024)
            {
                return ValidationResult.Failure($"{_property.Name} exceeds maximum length of 1024 characters");
            }
            
            return ValidationResult.Success();
        }
    }
    
    /// <summary>
    /// Numeric field validator
    /// </summary>
    public class NumericFieldValidator : IFieldValidator
    {
        private readonly PropertyInfo _property;
        
        public NumericFieldValidator(PropertyInfo property)
        {
            _property = property ?? throw new ArgumentNullException(nameof(property));
        }
        
        public ValidationResult Validate(object message)
        {
            object value = _property.GetValue(message);
            
            if (value == null)
            {
                // Null might be valid for optional fields
                return ValidationResult.Success();
            }
            
            // Check numeric range
            if (value is int intValue)
            {
                if (intValue < int.MinValue || intValue > int.MaxValue)
                {
                    return ValidationResult.Failure($"{_property.Name} is out of valid range");
                }
            }
            else if (value is long longValue)
            {
                if (longValue < long.MinValue || longValue > long.MaxValue)
                {
                    return ValidationResult.Failure($"{_property.Name} is out of valid range");
                }
            }
            
            return ValidationResult.Success();
        }
    }
    
    /// <summary>
    /// Collection field validator
    /// </summary>
    public class CollectionFieldValidator : IFieldValidator
    {
        private readonly PropertyInfo _property;
        
        public CollectionFieldValidator(PropertyInfo property)
        {
            _property = property ?? throw new ArgumentNullException(nameof(property));
        }
        
        public ValidationResult Validate(object message)
        {
            object value = _property.GetValue(message);
            
            if (value == null)
            {
                // Null might be valid for optional fields
                return ValidationResult.Success();
            }
            
            // Check collection size
            var collection = value as System.Collections.IEnumerable;
            if (collection != null)
            {
                int count = 0;
                foreach (var item in collection)
                {
                    count++;
                    if (count > 10000)
                    {
                        return ValidationResult.Failure($"{_property.Name} exceeds maximum size of 10000 items");
                    }
                }
            }
            
            return ValidationResult.Success();
        }
    }
    
    /// <summary>
    /// Validation rule interface
    /// </summary>
    public interface ValidationRule
    {
        string Name { get; }
        ValidationResult Validate(object message);
    }
    
    /// <summary>
    /// Non-null validation rule
    /// </summary>
    public class NonNullRule : ValidationRule
    {
        public string Name => "NonNullRule";
        
        public ValidationResult Validate(object message)
        {
            return message != null ? ValidationResult.Success() : ValidationResult.Failure("Message cannot be null");
        }
    }
    
    /// <summary>
    /// String length validation rule
    /// </summary>
    public class StringLengthRule : ValidationRule
    {
        public string Name => "StringLengthRule";
        
        public ValidationResult Validate(object message)
        {
            // This would be extended with actual string length validation
            return ValidationResult.Success();
        }
    }
    
    /// <summary>
    /// Numeric range validation rule
    /// </summary>
    public class NumericRangeRule : ValidationRule
    {
        public string Name => "NumericRangeRule";
        
        public ValidationResult Validate(object message)
        {
            // This would be extended with actual numeric range validation
            return ValidationResult.Success();
        }
    }
    
    /// <summary>
    /// Collection not empty validation rule
    /// </summary>
    public class CollectionNotEmptyRule : ValidationRule
    {
        public string Name => "CollectionNotEmptyRule";
        
        public ValidationResult Validate(object message)
        {
            // This would be extended with actual collection validation
            return ValidationResult.Success();
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharedProtocol
{
    /// <summary>
    /// Protocol validation utilities for protobuf messages
    /// Provides comprehensive validation for protocol contracts
    /// </summary>
    public static class ProtocolValidation
    {
        private static readonly Dictionary<Type, ProtocolValidator> _validators = new();
        private static readonly List<ValidationRule> _globalRules = new();
        private static bool _initialized = false;
        
        /// <summary>
        /// Initializes protocol validation system
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            
            // Register default validators
            RegisterDefaultValidators();
            
            // Register global validation rules
            RegisterGlobalRules();
            
            _initialized = true;
        }
        
        /// <summary>
        /// Validates a protocol message
        /// </summary>
        public static ValidationResult ValidateMessage(object message)
        {
            if (message == null)
            {
                return ValidationResult.Failure("Message cannot be null");
            }
            
            Initialize();
            
            Type messageType = message.GetType();
            
            // Get or create validator for message type
            if (!_validators.TryGetValue(messageType, out var validator))
            {
                validator = CreateValidator(messageType);
                _validators[messageType] = validator;
            }
            
            // Validate message
            return validator.Validate(message);
        }
        
        /// <summary>
        /// Validates protocol implementation
        /// </summary>
        public static ValidationResult ValidateProtocolImplementation()
        {
            Initialize();
            
            var errors = new List<string>();
            var warnings = new List<string>();
            
            // Check for required message types
            CheckRequiredMessageTypes(errors, warnings);
            
            // Check for required enums
            CheckRequiredEnums(errors, warnings);
            
            // Check for namespace consistency
            CheckNamespaceConsistency(errors, warnings);
            
            // Check for version compatibility
            CheckVersionCompatibility(errors, warnings);
            
            if (errors.Any())
            {
                return ValidationResult.Failure(string.Join("; ", errors));
            }
            
            if (warnings.Any())
            {
                return ValidationResult.Warning(string.Join("; ", warnings));
            }
            
            return ValidationResult.Success();
        }
        
        /// <summary>
        /// Validates protocol bindings
        /// </summary>
        public static ValidationResult ValidateBindings()
        {
            Initialize();
            
            var errors = new List<string>();
            var warnings = new List<string>();
            
            // Check message handler bindings
            CheckMessageHandlerBindings(errors, warnings);
            
            // Check message type mappings
            CheckMessageTypeMappings(errors, warnings);
            
            if (errors.Any())
            {
                return ValidationResult.Failure(string.Join("; ", errors));
            }
            
            if (warnings.Any())
            {
                return ValidationResult.Warning(string.Join("; ", warnings));
            }
            
            return ValidationResult.Success();
        }
        
        /// <summary>
        /// Validates enhanced protocol contracts
        /// </summary>
        public static ValidationResult ValidateEnhancedContracts()
        {
            Initialize();
            
            var errors = new List<string>();
            var warnings = new List<string>();
            
            // Check enhanced protocol messages
            CheckEnhancedProtocolMessages(errors, warnings);
            
            // Check enhanced protocol enums
            CheckEnhancedProtocolEnums(errors, warnings);
            
            // Check enhanced protocol versioning
            CheckEnhancedProtocolVersioning(errors, warnings);
            
            if (errors.Any())
            {
                return ValidationResult.Failure(string.Join("; ", errors));
            }
            
            if (warnings.Any())
            {
                return ValidationResult.Warning(string.Join("; ", warnings));
            }
            
            return ValidationResult.Success();
        }
        
        private static void RegisterDefaultValidators()
        {
            // Register validators for common message types
            // This would be extended with specific validators for each message type
        }
        
        private static void RegisterGlobalRules()
        {
            // Register global validation rules
            _globalRules.Add(new NonNullRule());
            _globalRules.Add(new StringLengthRule());
            _globalRules.Add(new NumericRangeRule());
            _globalRules.Add(new CollectionNotEmptyRule());
        }
        
        private static ProtocolValidator CreateValidator(Type messageType)
        {
            var validator = new ProtocolValidator(messageType);
            
            // Add field validators based on message type
            PropertyInfo[] properties = messageType.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                AddFieldValidator(validator, property);
            }
            
            return validator;
        }
        
        private static void AddFieldValidator(ProtocolValidator validator, PropertyInfo property)
        {
            // Add appropriate validator based on property type
            Type propertyType = property.PropertyType;
            
            if (propertyType == typeof(string))
            {
                validator.AddFieldValidator(new StringFieldValidator(property));
            }
            else if (propertyType == typeof(int) || propertyType == typeof(long))
            {
                validator.AddFieldValidator(new NumericFieldValidator(property));
            }
            else if (propertyType.IsGenericType && 
                     propertyType.GetGenericTypeDefinition() == typeof(RepeatedField<>))
            {
                validator.AddFieldValidator(new CollectionFieldValidator(property));
            }
        }
        
        private static void CheckRequiredMessageTypes(List<string> errors, List<string> warnings)
        {
            // Check for required message types in EnhancedMinecraftProtocol
            var requiredTypes = new[]
            {
                "PlayerInfoRequest",
                "PlayerInfoResponse",
                "InventoryRequest",
                "InventoryResponse",
                "BlockBreakRequest",
                "BlockBreakResponse",
                "ChunkDataRequest",
                "ChunkDataResponse"
            };
            
            foreach (string typeName in requiredTypes)
            {
                Type type = Type.GetType($"EnhancedMinecraftProtocol.{typeName}");
                if (type == null)
                {
                    errors.Add($"Required message type not found: {typeName}");
                }
            }
        }
        
        private static void CheckRequiredEnums(List<string> errors, List<string> warnings)
        {
            // Check for required enums in EnhancedMinecraftProtocol
            var requiredEnums = new[]
            {
                "BlockType",
                "EntityType",
                "DamageType",
                "EffectType",
                "ChatType",
                "WeatherType"
            };
            
            foreach (string enumName in requiredEnums)
            {
                Type type = Type.GetType($"EnhancedMinecraftProtocol.{enumName}");
                if (type == null || !type.IsEnum)
                {
                    errors.Add($"Required enum not found: {enumName}");
                }
            }
        }
        
        private static void CheckNamespaceConsistency(List<string> errors, List<string> warnings)
        {
            // Check namespace consistency across protocol files
            var namespaces = new[]
            {
                "EnhancedMinecraftProtocol",
                "Game.World",
                "Game.Auth",
                "Game.Move",
                "Game.Chat",
                "Game.Diag"
            };
            
            foreach (string ns in namespaces)
            {
                try
                {
                    Assembly.GetExecutingAssembly().GetTypes()
                        .FirstOrDefault(t => t.Namespace == ns);
                }
                catch (Exception ex)
                {
                    errors.Add($"Namespace check failed for {ns}: {ex.Message}");
                }
            }
        }
        
        private static void CheckVersionCompatibility(List<string> errors, List<string> warnings)
        {
            // Check version compatibility between client and server
            // This would be extended with actual version checking logic
            warnings.Add("Version compatibility check not fully implemented");
        }
        
        private static void CheckMessageHandlerBindings(List<string> errors, List<string> warnings)
        {
            // Check if all message types have corresponding handlers
            // This would be extended with actual handler binding checking
            warnings.Add("Message handler binding check not fully implemented");
        }
        
        private static void CheckMessageTypeMappings(List<string> errors, List<string> warnings)
        {
            // Check if all message types are properly mapped
            // This would be extended with actual type mapping checking
            warnings.Add("Message type mapping check not fully implemented");
        }
        
        private static void CheckEnhancedProtocolMessages(List<string> errors, List<string> warnings)
        {
            // Check enhanced protocol message structure
            // This would be extended with actual message structure checking
            warnings.Add("Enhanced protocol message check not fully implemented");
        }
        
        private static void CheckEnhancedProtocolEnums(List<string> errors, List<string> warnings)
        {
            // Check enhanced protocol enum values
            // This would be extended with actual enum value checking
            warnings.Add("Enhanced protocol enum check not fully implemented");
        }
        
        private static void CheckEnhancedProtocolVersioning(List<string> errors, List<string> warnings)
        {
            // Check enhanced protocol versioning
            // This would be extended with actual versioning checking
            warnings.Add("Enhanced protocol versioning check not fully implemented");
        }
    }
    
    /// <summary>
    /// Protocol validator for a specific message type
    /// </summary>
    public class ProtocolValidator
    {
        private readonly Type _messageType;
        private readonly List<IFieldValidator> _fieldValidators = new();
        
        public ProtocolValidator(Type messageType)
        {
            _messageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        }
        
        public void AddFieldValidator(IFieldValidator validator)
        {
            _fieldValidators.Add(validator);
        }
        
        public ValidationResult Validate(object message)
        {
            if (message == null)
            {
                return ValidationResult.Failure("Message cannot be null");
            }
            
            if (message.GetType() != _messageType)
            {
                return ValidationResult.Failure($"Message type mismatch. Expected: {_messageType.Name}, Got: {message.GetType().Name}");
            }
            
            var errors = new List<string>();
            
            // Validate each field
            foreach (var validator in _fieldValidators)
            {
                ValidationResult result = validator.Validate(message);
                if (!result.IsValid)
                {
                    errors.Add(result.ErrorMessage);
                }
            }
            
            if (errors.Any())
            {
                return ValidationResult.Failure(string.Join("; ", errors));
            }
            
            return ValidationResult.Success();
        }
    }
    
    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public bool IsWarning { get; private set; }
        public string ErrorMessage { get; private set; }
        
        private ValidationResult(bool isValid, bool isWarning, string errorMessage)
        {
            IsValid = isValid;
            IsWarning = isWarning;
            ErrorMessage = errorMessage;
        }
        
        public static ValidationResult Success()
        {
            return new ValidationResult(true, false, null);
        }
        
        public static ValidationResult Failure(string errorMessage)
        {
            return new ValidationResult(false, false, errorMessage);
        }
        
        public static ValidationResult Warning(string warningMessage)
        {
            return new ValidationResult(true, true, warningMessage);
        }
    }
    
    /// <summary>
    /// Field validator interface
    /// </summary>
    public interface IFieldValidator
    {
        ValidationResult Validate(object message);
    }
    
    /// <summary>
    /// String field validator
    /// </summary>
    public class StringFieldValidator : IFieldValidator
    {
        private readonly PropertyInfo _property;
        
        public StringFieldValidator(PropertyInfo property)
        {
            _property = property ?? throw new ArgumentNullException(nameof(property));
        }
        
        public ValidationResult Validate(object message)
        {
            string value = _property.GetValue(message) as string;
            
            if (value == null)
            {
                // Null might be valid for optional fields
                return ValidationResult.Success();
            }
            
            // Check string length
            if (value.Length > 1024)
            {
                return ValidationResult.Failure($"{_property.Name} exceeds maximum length of 1024 characters");
            }
            
            return ValidationResult.Success();
        }
    }
    
    /// <summary>
    /// Numeric field validator
    /// </summary>
    public class NumericFieldValidator : IFieldValidator
    {
        private readonly PropertyInfo _property;
        
        public NumericFieldValidator(PropertyInfo property)
        {
            _property = property ?? throw new ArgumentNullException(nameof(property));
        }
        
        public ValidationResult Validate(object message)
        {
            object value = _property.GetValue(message);
            
            if (value == null)
            {
                // Null might be valid for optional fields
                return ValidationResult.Success();
            }
            
            // Check numeric range
            if (value is int intValue)
            {
                if (intValue < int.MinValue || intValue > int.MaxValue)
                {
                    return ValidationResult.Failure($"{_property.Name} is out of valid range");
                }
            }
            else if (value is long longValue)
            {
                if (longValue < long.MinValue || longValue > long.MaxValue)
                {
                    return ValidationResult.Failure($"{_property.Name} is out of valid range");
                }
            }
            
            return ValidationResult.Success();
        }
    }
    
    /// <summary>
    /// Collection field validator
    /// </summary>
    public class CollectionFieldValidator : IFieldValidator
    {
        private readonly PropertyInfo _property;
        
        public CollectionFieldValidator(PropertyInfo property)
        {
            _property = property ?? throw new ArgumentNullException(nameof(property));
        }
        
        public ValidationResult Validate(object message)
        {
            object value = _property.GetValue(message);
            
            if (value == null)
            {
                // Null might be valid for optional fields
                return ValidationResult.Success();
            }
            
            // Check collection size
            var collection = value as System.Collections.IEnumerable;
            if (collection != null)
            {
                int count = 0;
                foreach (var item in collection)
                {
                    count++;
                    if (count > 10000)
                    {
                        return ValidationResult.Failure($"{_property.Name} exceeds maximum size of 10000 items");
                    }
                }
            }
            
            return ValidationResult.Success();
        }
    }
    
    /// <summary>
    /// Validation rule interface
    /// </summary>
    public interface ValidationRule
    {
        string Name { get; }
        ValidationResult Validate(object message);
    }
    
    /// <summary>
    /// Non-null validation rule
    /// </summary>
    public class NonNullRule : ValidationRule
    {
        public string Name => "NonNullRule";
        
        public ValidationResult Validate(object message)
        {
            return message != null ? ValidationResult.Success() : ValidationResult.Failure("Message cannot be null");
        }
    }
    
    /// <summary>
    /// String length validation rule
    /// </summary>
    public class StringLengthRule : ValidationRule
    {
        public string Name => "StringLengthRule";
        
        public ValidationResult Validate(object message)
        {
            // This would be extended with actual string length validation
            return ValidationResult.Success();
        }
    }
    
    /// <summary>
    /// Numeric range validation rule
    /// </summary>
    public class NumericRangeRule : ValidationRule
    {
        public string Name => "NumericRangeRule";
        
        public ValidationResult Validate(object message)
        {
            // This would be extended with actual numeric range validation
            return ValidationResult.Success();
        }
    }
    
    /// <summary>
    /// Collection not empty validation rule
    /// </summary>
    public class CollectionNotEmptyRule : ValidationRule
    {
        public string Name => "CollectionNotEmptyRule";
        
        public ValidationResult Validate(object message)
        {
            // This would be extended with actual collection validation
            return ValidationResult.Success();
        }
    }
}

